using System;
using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Streaming
{
  /// <summary>
  /// <c>ISweepAware</c>: the one thing the engine tells a backend about why a read is happening, and
  /// the only optional interface anything in the tree tests for.
  /// <para>
  /// <b>What it replaced, and why it is smaller.</b> A windowed view used to hand its own extent down
  /// with every cell it read, so which band was open was INFERRED from whichever subspace object
  /// happened to make the read — one hint per cell, one gate acquisition per resident read, and a
  /// per-cell deduplication to keep the cost down. Since phase 6 a region is a locator over the root
  /// space and there are no subspace objects to infer from, so the engine says it directly: once per
  /// placement, where the region is cut.
  /// </para>
  /// <para>
  /// <b>The two halves of the contract, and both are load-bearing.</b> It is announced EXACTLY ONCE
  /// per placement — a counter that ticked per read would be a different quantity, and a store that
  /// heard nothing would fall back to plain LRU and quietly change the cost model of an interleaved
  /// read. And what is announced is the extent the placement DECLARED, never the one a discovered
  /// bottom edge settles to: asking a region still being discovered how tall it is would read the
  /// file to find out, which is the cost the announcement exists to avoid paying.
  /// </para>
  /// <para>
  /// Tested against a double rather than against the store, because the store can only be asked what
  /// the announcement COST and these are claims about what the announcement IS. The store's own side
  /// — the union rule, the overrun counter — is in <see cref="SheetStoreTests"/>, and the two doors'
  /// agreement is in <see cref="StreamingIdentityTests"/>.
  /// </para>
  /// </summary>
  public class SweepAnnouncementTests
  {
    /// <summary>
    /// A sheet that records every band announced to it and refuses to be measured while it is
    /// listening — the second half being the point: a space that forced its own extent in order to
    /// answer the announcement would be paying exactly the cost the announcement saves.
    /// </summary>
    private sealed class ListeningSheet : ISheetCells, ISweepAware
    {
      private readonly ISheetCells _inner;

      public ListeningSheet(ISheetCells inner) => _inner = inner;

      /// <summary>Every band announced, in order, as <c>"origin+extent"</c>.</summary>
      public List<string> Announced { get; } = new List<string>();

      public void Sweeping(Offset origin, Area area)
        => Announced.Add($"{origin.Width},{origin.Height}+{area.Width}x{area.Height}");

      public Area Area => _inner.Area;

      public bool IsBlank(int column, int row) => _inner.IsBlank(column, row);

      public bool IsText(int column, int row) => _inner.IsText(column, row);

      public string? AsText(int column, int row) => _inner.AsText(column, row);

      public bool TextAt(int column, int row, out string value, out CellProblem? problem)
        => _inner.TextAt(column, row, out value, out problem);

      public bool DecimalAt(int column, int row, out decimal value, out CellProblem? problem)
        => _inner.DecimalAt(column, row, out value, out problem);

      public bool IntegerAt(int column, int row, out int value, out CellProblem? problem)
        => _inner.IntegerAt(column, row, out value, out problem);

      public bool DoubleAt(int column, int row, out double value, out CellProblem? problem)
        => _inner.DoubleAt(column, row, out value, out problem);

      public bool DateTimeAt(int column, int row, out DateTime value, out CellProblem? problem)
        => _inner.DateTimeAt(column, row, out value, out problem);

      public bool BooleanAt(int column, int row, out bool value, out CellProblem? problem)
        => _inner.BooleanAt(column, row, out value, out problem);

      public CellKind KindAt(int column, int row) => _inner.KindAt(column, row);

      public string Describe(int column, int row) => _inner.Describe(column, row);

      public bool IsErrorAt(int column, int row) => _inner.IsErrorAt(column, row);

      public string? ErrorTextAt(int column, int row) => _inner.ErrorTextAt(column, row);
    }

    /// <summary>A four-by-four coordinate grid that listens.</summary>
    private static ListeningSheet Listening() => new ListeningSheet(CoordinateGrid(4, 4));

    // --- Once per placement, and what it says ------------------------------------------------------

    [Fact]
    public void ARootDeclarationAnnouncesItsOwnRegionExactlyOnce()
    {
      // The simplest shape there is, so the count is unambiguous: one placement, one announcement,
      // of the region that placement resolved to. Nothing is announced per cell and nothing per read.
      var sheet = Listening();

      Right(1).Down(2).Of(Point()).Map(sheet);

      Assert.Equal(new[] { "1,2+1x1" }, sheet.Announced);
    }

    [Fact]
    public void EveryChildAnnouncesItsOwnBandAndTheParentAnnouncesTheWhole()
    {
      // A flow's children are placements, so each announces the band it was cut — in flow order,
      // after the flow's own. This is the shape the store's union rule is written for: a nested
      // child's band must not shrink the locus its parent opened, which SheetStoreTests pins from
      // the other side.
      var sheet = Listening();

      VerticalFlow(v =>
      {
        var rowSlot = v.Next(Row(4, cells => cells.Count));
        var rowSlot2 = v.Next(Row(4, cells => cells.Count));

        return v.Build(read => $"{read.Of(rowSlot)}{read.Of(rowSlot2)}");
      }).Map(sheet);

      Assert.Equal(new[] { "0,0+4x4", "0,0+4x1", "0,1+4x1" }, sheet.Announced);
    }

    [Fact]
    public void ATransparentWrapperAnnouncesItsOwnBandAndSoRepeatsItsChilds()
    {
      // The reason the store deduplicates rather than counting announcements. A transparent wrapper
      // is still a PLACEMENT — the engine resolves it, cuts a region and announces it — and a
      // boundary resolves its own placement and then places what it wraps, so ONE declared band
      // arrives more than once. Pinned as the exact sequence rather than as a count, because what
      // the store dedups is consecutive identical bands and that is only meaningful if the
      // repetition is known to be consecutive.
      var sheet = Listening();

      Right(1).Down(2).Of(Point().Optional()).Map(sheet);

      Assert.Equal(new[] { "1,2+3x2", "1,2+3x2", "1,2+1x1" }, sheet.Announced);
    }

    [Fact]
    public void AndAWrapperCarryingNoGeometryAnnouncesTheWholeExtentItWasHanded()
    {
      // The other arrangement, and the one that says the announcement is about the PLACEMENT rather
      // than about the shape underneath it: put the geometry on the child and the wrapper has none
      // of its own, so what it announces is everything it was handed. Two different bands, from a
      // declaration that reads exactly one cell.
      var sheet = Listening();

      Right(1).Down(2).Of(Point()).Padded(0).Map(sheet);

      Assert.Equal(new[] { "0,0+4x4", "1,2+1x1" }, sheet.Announced);
    }

    // --- Never forcing -----------------------------------------------------------------------------

    [Fact]
    public void ADiscoveredExtentIsAnnouncedAsDeclaredWithoutBeingSettled()
    {
      // The half that is easy to lose and impossible to notice: what is announced for a region whose
      // bottom edge is still being discovered is the CEILING that edge sits under, not where it turns
      // out to be. Announcing the settled height would mean reading the file to find out — the
      // forcing this whole arrangement exists to avoid.
      //
      // The grid is four rows and only the first two carry a value, so the table's discovered extent
      // settles at 2. The announcement says 4, which is what the placement declared before anything
      // was read.
      var sheet = new ListeningSheet(Mixed(new object?[,]
      {
        { "Name", "Amount" },
        { "Acme", 10 },
        { null, null },
        { null, null },
      }));

      var rows = Table((TableRow<ISheetCells> row) => row["Amount"].Integer()).Map(sheet);

      Assert.Equal(new[] { 10 }, rows);
      Assert.Equal("0,0+2x4", sheet.Announced[0]);
    }

    [Fact]
    public void AndTheBoundIsNotForcedByTheAnnouncementItself()
    {
      // The same claim measured rather than inferred, through the counting space. The table's extent
      // is discovered, so if Announce asked the plane how big it was, every row of the table would
      // have been scanned by the time the first announcement was recorded.
      //
      // One row has been touched, and it is not the discovery: it is the table's own OFFSET rule
      // probing where the header starts, which runs before the region is cut and so before there is
      // anything to announce. Four rows is what forcing would read; one is what placing costs.
      var counting = new CountingSpace(Mixed(new object?[,]
      {
        { "Name", "Amount" },
        { "Acme", 10 },
        { "Beta", 20 },
        { null, null },
      }));

      var touchedAtFirstAnnouncement = -1;
      var sheet = new ForcingWatch(counting, () => touchedAtFirstAnnouncement = touchedAtFirstAnnouncement < 0 ? counting.RowsTouched : touchedAtFirstAnnouncement);

      Table((TableRow<ISheetCells> row) => row["Amount"].Integer()).Map(sheet);

      Assert.Equal(1, touchedAtFirstAnnouncement);
      Assert.Equal(4, counting.RowsTouched);
    }

    /// <summary>
    /// A listening sheet that runs a callback at the moment it is told about a band, so a test can
    /// observe what had been read by then.
    /// </summary>
    private sealed class ForcingWatch : ISheetCells, ISweepAware
    {
      private readonly CountingSpace _inner;
      private readonly Action _onSweep;

      public ForcingWatch(CountingSpace inner, Action onSweep)
      {
        _inner = inner;
        _onSweep = onSweep;
      }

      public void Sweeping(Offset origin, Area area) => _onSweep();

      public Area Area => _inner.Area;

      public bool IsBlank(int column, int row) => _inner.IsBlank(column, row);

      public bool IsText(int column, int row) => _inner.IsText(column, row);

      public string? AsText(int column, int row) => _inner.AsText(column, row);

      public bool TextAt(int column, int row, out string value, out CellProblem? problem)
        => _inner.TextAt(column, row, out value, out problem);

      public bool DecimalAt(int column, int row, out decimal value, out CellProblem? problem)
        => _inner.DecimalAt(column, row, out value, out problem);

      public bool IntegerAt(int column, int row, out int value, out CellProblem? problem)
        => _inner.IntegerAt(column, row, out value, out problem);

      public bool DoubleAt(int column, int row, out double value, out CellProblem? problem)
        => _inner.DoubleAt(column, row, out value, out problem);

      public bool DateTimeAt(int column, int row, out DateTime value, out CellProblem? problem)
        => _inner.DateTimeAt(column, row, out value, out problem);

      public bool BooleanAt(int column, int row, out bool value, out CellProblem? problem)
        => _inner.BooleanAt(column, row, out value, out problem);

      public CellKind KindAt(int column, int row) => _inner.KindAt(column, row);

      public string Describe(int column, int row) => _inner.Describe(column, row);

      public bool IsErrorAt(int column, int row) => _inner.IsErrorAt(column, row);

      public string? ErrorTextAt(int column, int row) => _inner.ErrorTextAt(column, row);
    }

    // --- A space that does not listen is not asked --------------------------------------------------

    [Fact]
    public void ASpaceThatDoesNotImplementItIsNeverAsked()
    {
      // The optionality, stated: nothing in Core requires the capability and nothing in the engine
      // depends on it. A declaration over a plain grid reads exactly the same, which is the whole of
      // what "one optional type test per placement" is allowed to cost.
      var plain = CoordinateGrid(4, 4);

      Assert.False(plain is ISweepAware);
      Assert.Equal(22, Right(1).Down(2).Of(Point()).Map(plain).Integer());
    }
  }
}
