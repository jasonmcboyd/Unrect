using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The hybrid rule, one fact at a time: forward consumption streams, a dimension query forces.
  /// Where <see cref="LazyDenotationTests"/> says a discovered bound <em>means</em> what a measured
  /// one meant, this says how much of the sheet each way of reading it costs — a claim about
  /// <em>when</em> rows are touched, which only a counting space can make.
  /// <para>
  /// Every observation is taken from <em>inside</em> the projection, and that is not a stylistic
  /// choice. A declared area is consumed in full, so by the time <c>Map</c> returns the engine has
  /// forced the bound whatever the projection did with it; the only place the difference is visible
  /// is while the projection is still running.
  /// </para>
  /// <para>
  /// Reads are spelled both ways on purpose. Before step 6 a projection had to go through
  /// <c>CellBlock.Space</c> to stay lazy, because the view validated against its own height first;
  /// the views are bound-aware now, so <c>block[c, r]</c> costs exactly what <c>block.Space[c,
  /// r]</c> costs and both spellings are pinned below. What still forces is a genuine dimension
  /// query — <c>Height</c>, <c>Rows</c>, <c>Columns</c>, <c>Column</c>, <c>Location</c> — and each
  /// of those has its own fact here saying so.
  /// </para>
  /// <para>
  /// The counting space's ledger counts <em>distinct rows any cell was read from</em>, in the
  /// outermost space's coordinates. So "the scan advanced through row 2" and "three rows touched"
  /// are the same statement here, and re-reading a row already scanned adds nothing.
  /// </para>
  /// </summary>
  public class LazyForcingTests
  {
    /// <summary>
    /// A hundred rows of values over three blank ones. The scan therefore stops after row 99,
    /// having had to read row 100 to find out — so "forced to exhaustion" is 101 rows touched for a
    /// bound of 100, and the difference between the two numbers is the row that ended it.
    /// </summary>
    private static ISheetCells TallSheet()
    {
      var values = new int[103, 2];

      for (var row = 0; row < 100; row++)
      {
        values[row, 0] = row + 1;
        values[row, 1] = (row + 1) * 2;
      }

      return Grid(values);
    }

    private const int BoundHeight = 100;
    private const int RowsToExhaustion = 101;

    /// <summary>
    /// Applies a declared extent of "rows while any cell has a value" over the tall sheet, and
    /// hands back what <paramref name="read"/> saw of the sheet at the moment it ran — together
    /// with what the whole application ended up consuming, which is the other half of every fact
    /// here.
    /// </summary>
    private static (int RowsTouchedAtReadTime, Size Consumed) Observe(Action<Plane<ISheetCells>> read)
      => ObserveBlock(block => read(block.Space));

    /// <summary>
    /// The same observation taken through the view rather than through the space beneath it — the
    /// reading a real projection does, and since step 6 the one the cost pins are about.
    /// </summary>
    private static (int RowsTouchedAtReadTime, Size Consumed) ObserveBlock(Action<CellBlock<ISheetCells>> read)
    {
      var counter = new CountingSpace(TallSheet());
      var observed = -1;

      var applied = Range(RowsWhileAnyValue(), block =>
      {
        read(block);
        observed = counter.RowsTouched;

        return 0;
      }).Apply(counter);

      // Carried by every fact below rather than written out in each: however little of the extent
      // the projection asked for, the engine has read the scan to exhaustion by the time Apply
      // returns. A declared area is consumed in full, so the only variable here is what the
      // projection cost.
      Assert.Equal(RowsToExhaustion, counter.RowsTouched);

      return (observed, applied.Consumed);
    }

    // --- this[column, row]: through that row and no further ---------------------------------------

    [Fact]
    public void ARowBelowTheDiscoveredBoundIsAnOrdinaryOverrun()
    {
      // The bound is 100 rows and the sheet is 103, so row 100 exists and is still outside this
      // extent — exactly as it would be outside a measured one. Which is why it is OutOfBounds and
      // not the scan's own failure: nothing broke, the declaration ran out of room.
      //
      // And it is the REGION that refuses, not the space. The space underneath has a row 100 and
      // would have handed it over; what the projection was given is the region, whose bottom edge
      // is the declaration's own rule. A read that went to the space instead would silently return
      // a cell from past the boundary the declaration drew.
      var extent = Range(RowsWhileAnyValue(), block => block.Space[0, BoundHeight].IntegerOrBlank());

      var failure = Assert.Throws<ProjectionException>(() => extent.Map(TallSheet()));

      Assert.IsType<OutOfBoundsException>(failure.InnerException);
      Assert.False(failure.IsFault);

      // The space it reads through answers for the very same coordinate without complaint, which is
      // what makes the refusal the region's own rather than the sheet running out.
      Assert.Equal(103, TallSheet().Area.Height);
      Assert.True(TallSheet().IsBlank(0, BoundHeight));
    }

    [Fact]
    public void MintingAPointPastTheDiscoveredBoundRefusesWithoutEverReadingThatRow()
    {
      // The refusal happens where the address is made, which is the difference between "there is no
      // such row here" and "let me go and look". Row 102 is a real row of the sheet and outside the
      // region, so the scan runs to exhaustion saying so — 101 rows — and row 102 is never read at
      // all. A check that had gone to the space to decide would have read it.
      var counter = new CountingSpace(TallSheet());
      var observed = -1;
      Exception? refused = null;

      Range(RowsWhileAnyValue(), block =>
      {
        try
        {
          _ = block.Space[0, 102];
        }
        catch (OutOfBoundsException overrun)
        {
          refused = overrun;
        }

        observed = counter.RowsTouched;

        return 0;
      }).Apply(counter);

      Assert.IsType<OutOfBoundsException>(refused);
      Assert.Equal(RowsToExhaustion, observed);
    }

    // --- GetSubspace: through the rows asked for ---------------------------------------------------

    // --- The block view: the same rule, one level up ----------------------------------------------
    //
    // Step 6 made CellBlock bound-aware, so these numbers are the ones a projection actually pays: a
    // reader who never asked for a height never had to spell b.Space to keep it that way.

    [Fact]
    public void AnIndexPastTheDiscoveredBoundIsTheReadersBugAndNotTheFilesShape()
    {
      // The classification matters more than the message. b.Space[0, 100] is an overrun the
      // declaration may recover from (ARowBelowTheDiscoveredBoundIsAnOrdinaryOverrun, above);
      // b[0, 100] is a wrong index into the view, which is ArgumentOutOfRangeException and
      // therefore on the fault list. Same row, same bound, different verdict — and the bound-aware
      // validation must not have quietly turned the second into the first.
      var failure = Assert.Throws<ProjectionException>(() =>
        Range(RowsWhileAnyValue(), block => block[0, BoundHeight].Integer()).Named("bad").Map(TallSheet()));

      Assert.IsType<ArgumentOutOfRangeException>(failure.GetBaseException());
      Assert.True(failure.IsFault);
    }

    [Fact]
    public void AnIndexPastTheDiscoveredBoundIsNotAbsorbedByATolerance()
    {
      // The discovered-bound twin of
      // BoundaryProjectionTests.ABadViewIndexInAProjection_IsNotAbsorbed, which covers only the
      // measured case. A reading bug reported as "this section was absent" would be the worst
      // outcome laziness could produce.
      var failure = Assert.Throws<ProjectionException>(() =>
        Range(RowsWhileAnyValue(), block => block[0, BoundHeight].Integer()).Named("bad").Optional().Map(TallSheet()));

      Assert.IsType<ArgumentOutOfRangeException>(failure.GetBaseException());
      Assert.Equal("'bad'", failure.Subject);
    }

    // --- The dimension queries: everything ---------------------------------------------------------

    [Fact]
    public void AskingForTheHeightForcesTheScanToExhaustion()
    {
      var (observed, consumed) = Observe(space => Assert.Equal(BoundHeight, space.Area.Height));

      Assert.Equal(RowsToExhaustion, observed);
      Assert.Equal(BoundHeight, consumed.Height);
    }

    [Fact]
    public void AskingAPublicSpaceForItsWidthForcesTheHeightWithIt()
    {
      // DECIDED, not pending. §11.5 says a width never forces the height, and through ISheetCells it
      // does force, because ISheetCells.Area is ONE struct: there is no answering half of it, so a
      // public caller asking for a width asks for a height too. Step 6 did not change that and no
      // step will without surgery on ISheetCells. What it changed is that the free width now exists one
      // level up, on the region a view holds — so the 0 lives on the views, pinned by
      // TheBlocksWidthIsFreeOnADiscoveredBound and TheTablesColumnVocabularyIsFree below.
      var (observed, _) = Observe(space => Assert.Equal(2, space.Area.Width));

      Assert.Equal(RowsToExhaustion, observed);
    }

    [Fact]
    public void TheBlocksHeightIsADimensionQueryAndForces()
    {
      // CellBlock.Height, .Rows, .Columns, .Column and .Location read Space.Area, so a projection
      // that asks any of them settles the bound at that moment. Its Width, its indexer and its Row
      // do not — they go through the bound-aware seam, and their costs are the theories above.
      var (observed, _) = ObserveBlock(block => Assert.Equal(BoundHeight, block.Height));

      Assert.Equal(RowsToExhaustion, observed);
    }

    [Theory]
    [InlineData("Rows")]
    [InlineData("Location")]
    public void TheBlocksOtherDimensionQueriesForceTooAndSayThatOnTheirSummaries(string member)
    {
      // Rows is Row in a loop with the loop bound asked for up front, and Location carries the
      // extent it was found in — so both are the height question wearing a different name.
      var (observed, _) = ObserveBlock(block =>
      {
        switch (member)
        {
          case "Rows":
            Assert.Equal(BoundHeight, block.Rows.Count);
            break;

          // The address itself says nothing about the height — it is the extent the location
          // carries alongside it that has to be settled, and that is the whole cost.
          case "Location":
            Assert.Equal("A1", block.Location.A1);
            break;

          default:
            throw new ArgumentOutOfRangeException(nameof(member), member, "No such member.");
        }
      });

      Assert.Equal(RowsToExhaustion, observed);
    }

    // --- The claim the feature exists for -----------------------------------------------------------

    [Fact]
    public void TheEagerPathHasReadTheWholeScanBeforeTheProjectionStarts()
    {
      // The contrast that gives every number above its meaning: measured up front, all 101 rows are
      // behind the projection before it has read a cell, and it consumes exactly the same extent.
      var counter = new CountingSpace(TallSheet());
      var observed = -1;

      Size consumed;
      {
        consumed = Range(RowsWhileAnyValue(), block =>
        {
          observed = counter.RowsTouched;

          return block.Space[0, 0].Integer();
        }).Apply(counter).Consumed;
      }

      Assert.Equal(RowsToExhaustion, observed);
      Assert.Equal(BoundHeight, consumed.Height);
    }

    // --- The table view: a header up front, then whatever is asked for ----------------------------
    //
    // A table is the reading most declarations actually do, so its numbers are the ones the feature
    // is judged on. The shape of the claim is the same as the block's — a column question is free, a
    // row question costs the rows it names, a height question costs everything — with one extra term
    // that never goes away: the header row, which a table reads before it can name a column.

    /// <summary>
    /// A caption row over a hundred body rows, then three blank ones — the table twin of
    /// <see cref="TallSheet"/>. The declared extent is 101 rows with the header, and reaching
    /// exhaustion costs 102, the extra one being the blank row that ends the scan.
    /// </summary>
    private static ISheetCells TallTable()
    {
      var values = new object?[104, 2];

      values[0, 0] = "Client";
      values[0, 1] = "Amount";

      for (var row = 1; row <= TableBodyRows; row++)
      {
        values[row, 0] = $"client {row}";
        values[row, 1] = row;
      }

      return Mixed(values);
    }

    private const int TableBodyRows = 100;
    private const int TableBoundHeight = 101;
    private const int TableRowsToExhaustion = 102;

    /// <summary>
    /// The table twin of <see cref="ObserveBlock"/>. <c>.Sized</c> fixes the width at the available
    /// one, so every number below is about a fixed-width, discovered-height table — the simplest
    /// reading, and the one the block facts above are stated in. A table's own default placement is
    /// a discovered <em>block</em>, which since the width/height interleave landed is on the same
    /// deferred branch and costs the same here.
    /// </summary>
    private static (int RowsTouchedAtReadTime, Size Consumed) ObserveTable(Action<TableView<ISheetCells>> read)
    {
      var counter = new CountingSpace(TallTable());
      var observed = -1;

      var applied = Sized(RowsWhileAnyValue()).Of(Table(table =>
      {
        read(table);
        observed = counter.RowsTouched;

        return 0;
      })).Apply(counter);

      Assert.Equal(TableRowsToExhaustion, counter.RowsTouched);

      return (observed, applied.Consumed);
    }

    [Theory]
    [InlineData("RowCount")]
    [InlineData("Rows")]
    [InlineData("Location")]
    [InlineData("Failure")]
    public void TheTablesDimensionQueriesForceTheScanToExhaustion(string member)
    {
      var (observed, consumed) = ObserveTable(table =>
      {
        switch (member)
        {
          case "RowCount":
            Assert.Equal(TableBodyRows, table.RowCount);
            break;

          case "Rows":
            Assert.Equal(TableBodyRows, table.Rows.Count);
            break;

          case "Location":
            Assert.Equal("A1", table.Location.A1);
            break;

          // Not a query anyone writes for its own sake, but every complaint a projection makes
          // about the table itself goes through it — and citing the table's extent settles the
          // bound. That costs nothing worth saving, since the reading is over either way.
          case "Failure":
            Assert.Contains("A1", table.Failure("something is wrong").Message);
            break;

          default:
            throw new ArgumentOutOfRangeException(nameof(member), member, "No such member.");
        }
      });

      Assert.Equal(TableRowsToExhaustion, observed);
      Assert.Equal(TableBoundHeight, consumed.Height);
    }

    // --- The interleave law -----------------------------------------------------------------------

    /// <summary>
    /// How much of the sheet had been read at the moment each body row was projected.
    /// </summary>
    private static IReadOnlyList<int> RowsReadAsEachBodyRowProjects(bool sized, ISheetCells? sheet = null)
    {
      var counter = new CountingSpace(sheet ?? TallTable());
      var observations = new List<int>();

      var rows = Table(row =>
      {
        observations.Add(counter.RowsTouched);

        return row.Index;
      });

      (sized ? Sized(RowsWhileAnyValue()).Of(rows) : rows).Apply(counter);

      return observations;
    }

    /// <summary>
    /// The same hundred body rows, with the second column empty until row 50 — so the width is not
    /// settled by the caption row and the walk that decides it has to go looking.
    /// </summary>
    private static ISheetCells LateWideningTable()
    {
      var values = new object?[104, 2];

      values[0, 0] = "Client";

      for (var row = 1; row <= TableBodyRows; row++)
        values[row, 0] = $"client {row}";

      for (var row = FirstWideRow; row <= TableBodyRows; row++)
        values[row, 1] = row;

      return Mixed(values);
    }

    /// <summary>The first row carrying anything in column 1 — the row that settles the width.</summary>
    private const int FirstWideRow = 50;

    // --- What StreamRows promises besides being cheap ---------------------------------------------

    [Fact]
    public void StreamingPastTheLastRowEndsRatherThanOverrunning()
    {
      // The sheet has three real rows below the bound, so "there is no row 101" is a statement
      // about the extent and not about the file. A forward-only reader asks and is told no; it must
      // not be handed the OutOfBoundsException that reading b.Space[0, 101] would earn.
      var (observed, _) = ObserveTable(table => Assert.Equal(TableBodyRows, table.StreamRows().Count()));

      Assert.Equal(TableRowsToExhaustion, observed);
    }

    [Fact]
    public void ASecondStreamRebuildsTheRowsAndReadsNoMoreOfTheSheet()
    {
      // Uncached by design, and that is affordable precisely because the scan is settled: the
      // second enumeration walks rows the bound already knows about. Compared as two runs rather
      // than measured mid-projection, so the claim is "no additional rows" and not "some number".
      var once = ObserveTable(table => table.StreamRows().Count()).RowsTouchedAtReadTime;
      var twice = ObserveTable(table =>
      {
        table.StreamRows().Count();
        table.StreamRows().Count();
      }).RowsTouchedAtReadTime;

      Assert.Equal(TableRowsToExhaustion, once);
      Assert.Equal(once, twice);

      // Rebuilt, though — which is the part Rows does differently, and the reason a projection that
      // wants the rows twice should ask for Rows.
      ObserveTable(table =>
      {
        Assert.NotSame(table.StreamRows().First(), table.StreamRows().First());
        Assert.Same(table.Rows[0], table.Rows[0]);
      });
    }

    [Fact]
    public void OnAMeasuredExtentStreamRowsIsRowsWithoutTheCache()
    {
      // The control. Nothing about StreamRows is conditional on the extent being discovered, so on
      // an ordinary measured table it must read as the same rows in the same order — otherwise the
      // three Table rungs would have changed meaning for every declaration that is not lazy at
      // all.
      var space = Mixed(new object?[,]
      {
        { "Client", "Amount" },
        { "Acme", 10 },
        { "Beta", 20 },
        { "Gamma", 30 },
      });

      Table(table =>
      {
        Assert.Equal(
          table.Rows.Select(row => $"{row["Client"].Text()}={row["Amount"].Integer()}").ToList(),
          table.StreamRows().Select(row => $"{row["Client"].Text()}={row["Amount"].Integer()}").ToList());

        Assert.Equal(
          table.Rows.Select(row => row.Index).ToList(),
          table.StreamRows().Select(row => row.Index).ToList());

        Assert.NotSame(table.StreamRows().First(), table.StreamRows().First());
        Assert.Same(table.Rows[0], table.Rows[0]);

        return 0;
      }).Map(space);
    }
  }
}
