using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// The differential suite, and the primary evidence for lazy extents: a bound discovered while a
  /// projection consumes it must mean exactly what the same bound measured up front meant. Every
  /// case here is one declaration over one space, run twice — once with the engine free to defer and
  /// once with <see cref="ShapeEngine.ForceEager"/> holding it to the reading it did before Part 2 —
  /// and the two readings are compared on everything a caller can observe: the projected value, the
  /// extent consumed, the diagnostics in order, and, where the declaration fails, the failure's
  /// message, path, location and fault flag.
  /// <para>
  /// The suite is only worth anything if the declarations it sweeps actually take the deferred
  /// branch, which is why the census at the bottom exists. That branch is reached by exactly one
  /// thing: an area strategy that is an <see cref="IIncrementalAreaStrategy"/>. Two families are —
  /// a per-row height at the full available width (<c>RowsWhileAnyValue()</c>/<c>RowsWhileAny(…)</c>,
  /// spelled through <c>Range(strategy, …)</c> or <c>.Sized(…)</c>), and, since the width/height
  /// interleave landed, a discovered block, which is what <c>Range(…)</c> and every table rung are
  /// placed by with nothing said. A declaration built on any other extent is eager on both runs and
  /// proves nothing, so the census pins which spellings defer and which — deliberately — do not.
  /// </para>
  /// </summary>
  public class LazyDenotationTests
  {
    // --- The spaces every case is read over --------------------------------------------------------

    /// <summary>
    /// Three rows of values over two blank ones: a row-wise rule stops at row 3, which leaves both a
    /// discovered bound and undescribed space below it for the diagnostics to have something to say.
    /// </summary>
    private static ISpace Sheet() => Grid(new[,]
    {
      { 1, 2, 3 },
      { 4, 5, 6 },
      { 7, 8, 9 },
      { 0, 0, 0 },
      { 0, 0, 0 },
    });

    /// <summary>
    /// A hundred rows of values and three blank ones — long enough that "the projection read three
    /// rows" and "the scan read the lot" are different readings rather than the same one twice.
    /// </summary>
    private static ISpace TallSheet()
    {
      var values = new int[103, 2];

      for (var row = 0; row < 100; row++)
      {
        values[row, 0] = row + 1;
        values[row, 1] = (row + 1) * 2;
      }

      return Grid(values);
    }

    /// <summary>
    /// A caption row over three body rows and two blank ones — the same shape as <see cref="Sheet"/>
    /// with a header on top, so the table rungs have captions to bind and undescribed space below.
    /// </summary>
    private static ISpace Headered() => Mixed(new object?[,]
    {
      { "Client", "Amount" },
      { "Acme", 10 },
      { "Beta", 20 },
      { "Gamma", 30 },
      { null, null },
      { null, null },
    });

    /// <summary>
    /// A hundred body rows whose second column is empty until row 50, under a caption row that names
    /// only the first — so a discovered width is not settled by the header and the walk deciding it
    /// has to read fifty rows to find out. The interleave's expensive case, and the one where the
    /// deferred reading and the measured one read most nearly the same amount of the sheet.
    /// </summary>
    private static ISpace LateWideningSheet()
    {
      var values = new object?[104, 2];

      values[0, 0] = "Client";

      for (var row = 1; row <= 100; row++)
        values[row, 0] = $"client {row}";

      for (var row = 50; row <= 100; row++)
        values[row, 1] = row;

      return Mixed(values);
    }

    /// <summary>What the typed rung binds a row of <see cref="Headered"/> to.</summary>
    public record Entry(string Client, int Amount);

    // --- The two ways a scan can go wrong, both decided by content --------------------------------
    //
    // Content-based on purpose: a predicate that counted its own calls would break on a different row
    // in each of the two runs — the runs consume different numbers of cells, which is the whole point
    // — and the suite would report a difference the engine did not cause.

    /// <summary>A cell rule that breaks, absorbably, on the cell holding <paramref name="marker"/>.</summary>
    private static Func<CellValue, bool> BreaksOn(int marker)
      => cell => cell.TryGetInt() == marker ? throw new InvalidOperationException("no") : true;

    /// <summary>A cell rule whose failure is the environment's, not the data's.</summary>
    private static Func<CellValue, bool> FaultsOn(int marker)
      => cell => cell.TryGetInt() == marker ? throw new IOException("the disk stopped answering") : true;

    /// <summary>
    /// The sheet's 7 is the first cell of row 2, so a rule that breaks on it survives the first two
    /// rows: eagerly the scan reaches it before the projection starts, lazily only if something asks
    /// for row 2 or the engine forces the bound. That gap is where every "late break" case lives.
    /// </summary>
    private const int LateMarker = 7;

    /// <summary>A rule that breaks before it has looked at anything — the scan fails at its first row.</summary>
    private static IShape<int> Breaks(Exception exception)
      => Range(RowsWhileAny(_ => throw exception), b => b.Height);

    // --- The cases -------------------------------------------------------------------------------

    private static Scenario Case(string name) => name switch
    {
      // The plain shapes of the thing: a bound read in full, a bound read not at all, and a bound
      // that is not discovered at all so the sweep contains its own control.
      "discovered extent" => Scenario.Of(Range(RowsWhileAnyValue(), b => b.Height), Sheet()),
      "unread extent" => Scenario.Of(Range(RowsWhileAnyValue(), _ => 0), Sheet()),
      "full sheet" => Scenario.Of(Range(WholeExtent(), b => b.Height), Sheet()),

      // The same bound reached through a layout, which is where the deferred extent stops being the
      // root's and becomes a child's — and where a sibling's placement depends on what it consumed.
      "flow" => Scenario.Of(VerticalFlow(v => v.Next(Range(RowsWhileAnyValue(), b => b.Height))), Sheet()),
      "flow of two children" => Scenario.Of(
        VerticalFlow(v =>
        {
          var head = v.Next(Range(RowsWhileAnyValue(), b => b.Height));
          var tail = v.Next(Range(WholeExtent(), b => b.Height));

          return head * 100 + tail;
        }),
        Sheet()),

      // The other spelling of the same extent: .Sized replaces a shape's own area outright.
      "sized" => Scenario.Of(Range(b => b.Height).Sized(RowsWhileAnyValue()), Sheet()),
      "sized, unread" => Scenario.Of(Range(_ => 0).Sized(RowsWhileAnyValue()), Sheet()),

      // The one declaration already in the suite that goes through this branch, lifted verbatim from
      // ShapeReExportTests: the grid has a blank column, so the discovered extent is the full width
      // over both rows and the projection is a string rather than a number.
      "the re-export suite's .Sized declaration" => Scenario.Of(
        Range(b => $"{b.Width}x{b.Height}").Sized(RowsWhileAnyValue()),
        Grid(new[,] { { 1, 0, 3 }, { 2, 0, 4 } })),

      // Reads that force through the view rather than through the space: the block's own members are
      // dimension queries, so these are the cases where the projection settles the bound itself.
      "rows enumerated" => Scenario.Of(Range(RowsWhileAnyValue(), b => b.Rows.Count), Sheet()),
      "block width" => Scenario.Of(Range(RowsWhileAnyValue(), b => b.Width), Sheet()),

      // The boundary of the discovered extent, from both sides. Reading its last row is ordinary;
      // reading the row below it is an overrun, and must be the same overrun either way.
      "last row of the bound read" => Scenario.Of(Range(RowsWhileAnyValue(), b => b.Space[0, 2].GetInt()), Sheet()),
      "read past the bound" => Scenario.Of(Range(RowsWhileAnyValue(), b => b.Space[0, 4].TryGetInt()), Sheet()),

      // A hundred-row bound of which the projection reads three: the case the whole feature is for,
      // and the one where the two runs read the most different amounts of the sheet.
      "tall bound, three rows read" => Scenario.Of(
        Range(RowsWhileAnyValue(), b => b.Space[0, 0].GetInt() + b.Space[0, 1].GetInt() + b.Space[0, 2].GetInt()),
        TallSheet()),

      // A scan that breaks immediately. Eagerly this is a placement failure before the projection
      // runs; lazily the placement succeeded and the break arrives from inside it. Same failure.
      "predicate throws" => Scenario.Of(Breaks(new InvalidOperationException("no")), Sheet()),
      "predicate faults" => Scenario.Of(Breaks(new IOException("the disk stopped answering")), Sheet()),

      // A fault is never tolerance, at any of the three boundaries — deferring it must not turn a
      // broken disk into an absent section.
      "faulting under Optional" => Scenario.Of(Breaks(new IOException("gone")).Optional(), Sheet()),
      "faulting under Else" => Scenario.Of(Range(RowsWhileAny(FaultsOn(LateMarker)), b => b.Height).Else(-1), Sheet()),
      "faulting under Choice" => Scenario.Of(
        Choice(Range(RowsWhileAny(FaultsOn(LateMarker)), b => b.Height), Range(WholeExtent(), b => b.Height)),
        Sheet()),

      // The absorbable twin of each: a warning either way, with the same subject and the same reason.
      "breaking under Optional" => Scenario.Of(Breaks(new InvalidOperationException("no")).Optional(), Sheet()),
      "breaking under Else" => Scenario.Of(Range(RowsWhileAny(BreaksOn(LateMarker)), b => b.Height).Else(-1), Sheet()),

      // Rule 2: a repeat's item is placed non-strictly and therefore never defers. What is under test
      // here is that saying so changed nothing about what the repeat produces.
      "repeat of discovered items" => Scenario.Of(
        Repeat(Range(RowsWhileAnyValue(), b => b.Height), separatedBy: BlankRows()).Select(items => items.Count),
        Sheet()),
      "repeat requiring one" => Scenario.Of(
        Repeat(Range(RowsWhileAnyValue(), b => b.Height), separatedBy: BlankRows(), atLeast: 1).Select(items => items.Count),
        Sheet()),

      // A scan claiming one column more than there is. The engine declines to bind rather than
      // reporting the overrun itself, because saying so needs the height the eager reading measured.
      "overwide scan" => Scenario.Of(Range(new OverwideStrategy(), b => b.Height), Sheet()),

      // The scan breaks on row 2, which the projection never reads: eagerly the break happens at
      // placement, lazily only when the engine forces the bound after projecting. The moment differs
      // and nothing else may.
      "late break, unread extent" => Scenario.Of(Range(RowsWhileAny(BreaksOn(LateMarker)), _ => 0), Sheet()),
      "late break under Optional" => Scenario.Of(Range(RowsWhileAny(BreaksOn(LateMarker)), _ => 0).Optional(), Sheet()),
      "late break in a Choice" => Scenario.Of(
        Choice(Range(RowsWhileAny(BreaksOn(LateMarker)), _ => 0), Range(WholeExtent(), b => b.Height)),
        Sheet()),

      // The three table rungs, which since step 6 read their bodies through TableView.StreamRows.
      // .Sized fixes the width at the available one, which is the simplest extent that defers and so
      // the one the rungs are swept on; the default placement — a discovered block, deferred since
      // the interleave landed — is swept separately below. What is under test is that a streamed body
      // means what an indexed one meant: the same rows, in order, with the same space left over.
      "table rows, lambda" => Scenario.Of(
        TableRows(row => $"{row["Client"].GetString()}={row["Amount"].GetInt()}").Sized(RowsWhileAnyValue()),
        Headered()),
      "table rows, typed" => Scenario.Of(TableRows<Entry>().Sized(RowsWhileAnyValue()), Headered()),
      // .Sized before .Select on purpose: Select's wrapper is a shape with a placement of its own, so
      // sizing the wrapper would leave the table inside it placed by its own eager default.
      "table rows, dictionaries" => Scenario.Of(
        TableRows().Sized(RowsWhileAnyValue()).Select(rows => rows.Select(row => $"{row["Client"]}/{row["Amount"]}").ToList()),
        Headered()),

      // The table view reached directly, where the projection asks for the dimension query the three
      // rungs are written to avoid — so the forcing read has to denote what the streaming one does.
      "table, rows materialised" => Scenario.Of(
        Table(table => $"{table.RowCount}x{table.ColumnCount}").Sized(RowsWhileAnyValue()),
        Headered()),

      // A body read in part: the case where the two runs read the most different amounts of a table,
      // and the one where a cached Rows and an uncached StreamRows could most easily disagree.
      "table, first row only" => Scenario.Of(
        Table(table => table.StreamRows().First()["Client"].GetString()).Sized(RowsWhileAnyValue()),
        Headered()),

      // The declarations nobody decorates, which the width/height interleave brought onto the
      // deferred branch: a block and a table placed by their own defaults, where the width is
      // discovered alongside the height rather than taken as the available one. The sweep's job is
      // the same as ever — the two-pass reading and the one-walk reading must denote the same thing
      // — but these are the spellings most declarations in the corpus are actually written in.
      "block, default placement" => Scenario.Of(Range(b => $"{b.Width}x{b.Height}"), Sheet()),
      "block, default placement, unread" => Scenario.Of(Range(_ => 0), Sheet()),
      "table rows typed, default placement" => Scenario.Of(TableRows<Entry>(), Headered()),
      "table rows lambda, default placement" => Scenario.Of(
        TableRows(row => $"{row["Client"].GetString()}={row["Amount"].GetInt()}"),
        Headered()),

      // The same default placement over a sheet whose width is NOT settled by its first row: column
      // 1 carries nothing until row 50, so the walk that decides the width forces most of the bound
      // before the projection starts — §11.4's "the width decision forces the whole bound, correctly
      // and honestly". What that costs is LazyForcingTests' business; what is under test here is that
      // paying it changes nothing a caller can observe.
      "table whose width settles late" => Scenario.Of(
        TableRows(row => row.Index).Select(rows => $"{rows.Count}:{rows[0]}..{rows[rows.Count - 1]}"),
        LateWideningSheet()),

      // A bound inside a wrapper whose own placement reads the sheet to find its landmark.
      "discovered extent under Until" => Scenario.Of(
        Range(RowsWhileAnyValue(), b => b.Height).Until(RowWhere((space, row) => space[0, row].IsBlank), orEnd: true),
        Sheet()),

      _ => throw new ArgumentOutOfRangeException(nameof(name), name, "No such case."),
    };

    [Theory]
    [InlineData("discovered extent")]
    [InlineData("unread extent")]
    [InlineData("full sheet")]
    [InlineData("flow")]
    [InlineData("flow of two children")]
    [InlineData("sized")]
    [InlineData("sized, unread")]
    [InlineData("the re-export suite's .Sized declaration")]
    [InlineData("rows enumerated")]
    [InlineData("block width")]
    [InlineData("last row of the bound read")]
    [InlineData("read past the bound")]
    [InlineData("tall bound, three rows read")]
    [InlineData("predicate throws")]
    [InlineData("predicate faults")]
    [InlineData("faulting under Optional")]
    [InlineData("faulting under Else")]
    [InlineData("faulting under Choice")]
    [InlineData("breaking under Optional")]
    [InlineData("breaking under Else")]
    [InlineData("repeat of discovered items")]
    [InlineData("repeat requiring one")]
    [InlineData("overwide scan")]
    [InlineData("late break, unread extent")]
    [InlineData("late break under Optional")]
    [InlineData("late break in a Choice")]
    [InlineData("table rows, lambda")]
    [InlineData("table rows, typed")]
    [InlineData("table rows, dictionaries")]
    [InlineData("table, rows materialised")]
    [InlineData("table, first row only")]
    [InlineData("block, default placement")]
    [InlineData("block, default placement, unread")]
    [InlineData("table rows typed, default placement")]
    [InlineData("table rows lambda, default placement")]
    [InlineData("table whose width settles late")]
    [InlineData("discovered extent under Until")]
    public void ADeclarationMeansTheSameWhicheverWayItsExtentsAreResolved(string name)
    {
      var subject = Case(name);

      var lazily = subject.Lazily();
      var eagerly = subject.Eagerly();

      // Compared facet by facet rather than as one blob, so a failure names which of them moved.
      Assert.Equal(eagerly.Failure, lazily.Failure);
      Assert.Equal(eagerly.Value, lazily.Value);
      Assert.Equal(eagerly.Consumed, lazily.Consumed);
      Assert.Equal(eagerly.Diagnostics, lazily.Diagnostics);
    }

    // --- The census: which of those declarations actually took the deferred branch ------------------
    //
    // Without this, a change that stopped the engine binding lazily would leave the whole sweep above
    // green and testing eager against eager. The probe is the honest one available from outside the
    // engine: the eager path measures the extent BEFORE the projection runs, so a projection that
    // asks the counting space how much of the sheet has been read is asking a question the two paths
    // answer differently — nothing yet, versus the whole scan.

    private static int RowsReadBeforeTheProjectionRuns(Func<Func<CellBlock, int>, IShape<int>> declare, ISpace sheet, bool eager)
    {
      var counter = new CountingSpace(sheet);
      var observed = -1;
      var shape = declare(_ =>
      {
        observed = counter.RowsTouched;

        return 0;
      });

      if (eager)
      {
        using (ShapeEngine.ForceEager())
          shape.Apply(counter);
      }
      else
      {
        shape.Apply(counter);
      }

      return observed;
    }

    [Theory]
    [InlineData("Range(strategy)")]
    [InlineData("Sized")]
    [InlineData("inside a flow")]
    [InlineData("under Optional")]
    public void TheDeclarationsThisSuiteSweepsDoTakeTheDeferredBranch(string spelling)
    {
      Func<Func<CellBlock, int>, IShape<int>> declare = spelling switch
      {
        "Range(strategy)" => project => Range(RowsWhileAnyValue(), project),
        "Sized" => project => Range(project).Sized(RowsWhileAnyValue()),
        "inside a flow" => project => VerticalFlow(v => v.Next(Range(RowsWhileAnyValue(), project))),
        "under Optional" => project => Range(RowsWhileAnyValue(), project).Optional(),

        _ => throw new ArgumentOutOfRangeException(nameof(spelling), spelling, "No such spelling."),
      };

      // Nothing at all read when the projection starts, against the four rows the eager reading takes
      // to discover where the values stop. If these two ever agree, the branch is no longer taken.
      Assert.Equal(0, RowsReadBeforeTheProjectionRuns(declare, Sheet(), eager: false));
      Assert.Equal(4, RowsReadBeforeTheProjectionRuns(declare, Sheet(), eager: true));
    }

    [Theory]
    [InlineData("TableRows(lambda)")]
    [InlineData("TableRows<T>()")]
    [InlineData("TableRows()")]
    [InlineData("Table(lambda)")]
    public void TheTableDeclarationsThisSuiteSweepsDoTakeTheDeferredBranch(string rung)
    {
      // The table half of the census. The probe is structural rather than observational because two
      // of the three rungs project the whole body themselves and give a test nowhere to stand; the
      // observational reading of the same claim is LazyForcingTests, which watches a TableRows lambda
      // project its first row having read two rows of the sheet.
      // The rungs project different types, so each is reduced to the pair of placements the claim is
      // actually about before the switch has to agree on one.
      static (IShape Declared, IShape Sized) Probe<T>(IShape<T> shape) => (shape, shape.Sized(RowsWhileAnyValue()));

      var (declared, sized) = rung switch
      {
        "TableRows(lambda)" => Probe(TableRows(row => row.Index)),
        "TableRows<T>()" => Probe(TableRows<Entry>()),
        "TableRows()" => Probe(TableRows()),
        "Table(lambda)" => Probe(Table(table => table.RowCount)),

        _ => throw new ArgumentOutOfRangeException(nameof(rung), rung, "No such rung."),
      };

      // Both, and .Sized is no longer the difference. A table's own extent is a discovered block,
      // which the width/height interleave made an incremental area strategy — so every rung defers
      // as written, and .Sized now changes only how the width is arrived at (taken as the available
      // one, rather than walked for) and not whether the height can be discovered.
      Assert.IsAssignableFrom<IIncrementalAreaStrategy>(declared.Placement.Area);
      Assert.IsAssignableFrom<IIncrementalAreaStrategy>(sized.Placement.Area);
    }

    [Theory]
    [InlineData("Range(width, height)")]
    [InlineData("WholeExtent")]
    [InlineData("Extent(width, height)")]
    public void ADeclarationWhoseExtentIsNotAPerRowRuleIsEagerBothWays(string spelling)
    {
      // The other half of the census, and the honest statement of what Part 2 does not cover. None of
      // these extents is an IIncrementalAreaStrategy — each is a fixed size, which has nothing to
      // discover and so nothing to defer — so the sweep above proves nothing about them. Range()'s
      // discovered block left this theory when the width/height interleave landed; it is now
      // ADefaultPlacementDiscoversItsWidthFromTheRowsItsHeightWouldHaveReadAnyway.
      Func<Func<CellBlock, int>, IShape<int>> declare = spelling switch
      {
        "Range(width, height)" => project => Range(3, 3, project),
        "WholeExtent" => project => Range(WholeExtent(), project),
        "Extent(width, height)" => project => Range(Extent(3, 3), project),

        _ => throw new ArgumentOutOfRangeException(nameof(spelling), spelling, "No such spelling."),
      };

      Assert.IsNotAssignableFrom<IIncrementalAreaStrategy>(declare(_ => 0).Placement.Area);

      Assert.Equal(
        RowsReadBeforeTheProjectionRuns(declare, Sheet(), eager: true),
        RowsReadBeforeTheProjectionRuns(declare, Sheet(), eager: false));
    }

    [Theory]
    [InlineData("VerticalFlow")]
    [InlineData("HorizontalFlow")]
    [InlineData("Overlay")]
    public void ASizedLayoutCompositeIsEagerBothWaysThoughItsExtentIsAPerRowRule(string layout)
    {
      // The third statement of what Part 2 does not cover, and the one that could not join the theory
      // above it: these extents ARE IIncrementalAreaStrategy — the same RowsWhileAnyValue() that
      // defers on a leaf — and they still resolve in full before anything projects. So the claim has
      // to be made twice over, structurally and observationally, rather than by the one negative
      // assertion that serves a fixed size.
      //
      // The forcing sites are two lines of ShapeEngine.TryPlace, both reached while placing the
      // composite's FIRST child, both before any child projects:
      //   Exceeds(offset.Size, availableSpace)  — reads availableSpace.Area, which on a BoundedSpace
      //                                           is defined as reading the scan to exhaustion;
      //   availableSpace.GetSubspace(offset)    — the ISpace form, which asks for an Area too.
      // Making those bound-aware — a child's placement asking "is there a row here" rather than "how
      // tall are you" — is Part 3's business, deliberately not attempted in Part 2. This test is the
      // decision pin: lifting the limitation must come here and flip the second number below from 4
      // to 0 on purpose, rather than silently widening what defers.
      Func<Func<CellBlock, int>, IShape<int>> declare = layout switch
      {
        // A fixed 3x1 child so the child's own placement has nothing to discover: what is measured
        // here is the parent's bound being settled, not the child's.
        "VerticalFlow" => project => VerticalFlow(v => v.Next(Range(3, 1, project))).Sized(RowsWhileAnyValue()),
        "HorizontalFlow" => project => HorizontalFlow(h => h.Next(Range(3, 1, project))).Sized(RowsWhileAnyValue()),
        "Overlay" => project => Overlay(o => o.Next(Range(3, 1, project))).Sized(RowsWhileAnyValue()),

        _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, "No such layout."),
      };

      Assert.IsAssignableFrom<IIncrementalAreaStrategy>(declare(_ => 0).Placement.Area);

      // Both readings take the whole bound — the four rows it costs to find where the values stop —
      // before the projection runs. Eager both ways in effect, incremental strategy or not.
      Assert.Equal(4, RowsReadBeforeTheProjectionRuns(declare, Sheet(), eager: false));
      Assert.Equal(4, RowsReadBeforeTheProjectionRuns(declare, Sheet(), eager: true));
    }

    /// <summary>
    /// <see cref="Sheet"/> with a hole in its first row, so an "any" column rule cannot settle the
    /// width there and the walk has to take a second row to find it.
    /// </summary>
    private static ISpace HoledSheet() => Grid(new[,]
    {
      { 1, 0, 3 },
      { 4, 5, 6 },
      { 7, 8, 9 },
      { 0, 0, 0 },
      { 0, 0, 0 },
    });

    [Theory]
    [InlineData("dense first row", 1)]
    [InlineData("hole in the first row", 2)]
    public void ADefaultPlacementDiscoversItsWidthFromTheRowsItsHeightWouldHaveReadAnyway(string sheet, int rowsRead)
    {
      // The third census entry, and it belongs to itself rather than to the theory above it: a
      // default placement discovers BOTH dimensions, so unlike a .Sized declaration it cannot start
      // the projection having read nothing. What it can promise is that the rows it read to settle
      // the width are a prefix of the rows the height accepts — nothing early, nothing twice — which
      // is exactly what these numbers say. One row where the first row is dense, two where a hole in
      // it defers the answer, against the four the eager reading takes either way.
      var space = sheet switch
      {
        "dense first row" => Sheet(),
        "hole in the first row" => HoledSheet(),

        _ => throw new ArgumentOutOfRangeException(nameof(sheet), sheet, "No such sheet."),
      };

      Assert.IsAssignableFrom<IIncrementalAreaStrategy>(Range(_ => 0).Placement.Area);

      Assert.Equal(rowsRead, RowsReadBeforeTheProjectionRuns(project => Range(project), space, eager: false));
      Assert.Equal(4, RowsReadBeforeTheProjectionRuns(project => Range(project), space, eager: true));
    }

    [Fact]
    public void TheRowThatSettlesADiscoveredWidthIsTheFirstRowOfTheExtent()
    {
      // Which is the whole argument for the interleave being free in the dense case: the row the
      // width came from is not a row read early, it is the extent's own first row, and the projection
      // was going to want it. Reading it back costs nothing further; reading the one after it costs
      // exactly one more.
      var counter = new CountingSpace(Sheet());
      var afterFirst = -1;
      var afterSecond = -1;

      Range(block =>
      {
        Assert.Equal(1, block[0, 0].GetInt());
        afterFirst = counter.RowsTouched;

        Assert.Equal(4, block[0, 1].GetInt());
        afterSecond = counter.RowsTouched;

        return 0;
      }).Apply(counter);

      Assert.Equal(1, afterFirst);
      Assert.Equal(2, afterSecond);
    }

    [Fact]
    public void ForcingEagerRestoresRatherThanClearing()
    {
      // The switch composes with itself, which matters because a differential case may itself contain
      // a nested Map. Asserted through the probe rather than through the flag, which is private.
      using (ShapeEngine.ForceEager())
      {
        using (ShapeEngine.ForceEager())
        {
          Assert.Equal(4, RowsReadBeforeTheProjectionRuns(p => Range(RowsWhileAnyValue(), p), Sheet(), eager: false));
        }

        Assert.Equal(4, RowsReadBeforeTheProjectionRuns(p => Range(RowsWhileAnyValue(), p), Sheet(), eager: false));
      }

      Assert.Equal(0, RowsReadBeforeTheProjectionRuns(p => Range(RowsWhileAnyValue(), p), Sheet(), eager: false));
    }

    // --- What a run of one declaration is compared on ----------------------------------------------

    /// <summary>One declaration over one space, ready to be read either way.</summary>
    private sealed class Scenario
    {
      private Scenario(Func<Outcome> observe) => Observe = observe;

      private Func<Outcome> Observe { get; }

      public static Scenario Of<T>(IShape<T> shape, ISpace space) => new Scenario(() => Read(shape, space));

      public Outcome Lazily() => Observe();

      public Outcome Eagerly()
      {
        using (ShapeEngine.ForceEager())
          return Observe();
      }

      private static Outcome Read<T>(IShape<T> shape, ISpace space)
      {
        try
        {
          // Two calls because the two entry points answer different halves of the question: the value
          // and what the parse noticed come from one, the extent consumed from the other. A shape is a
          // value safe to apply twice, so this is one reading asked about twice, not two readings.
          var mapped = shape.MapWithDiagnostics(space);
          var applied = shape.Apply(space);

          return new Outcome(
            RenderValue(mapped.Value),
            $"{applied.Consumed.Width}x{applied.Consumed.Height}",
            mapped.Diagnostics.Select(Describe).ToList(),
            null);
        }
        catch (ShapeException failure)
        {
          return new Outcome("<threw>", "<threw>", Array.Empty<string>(), Describe(failure));
        }
      }
    }

    private sealed class Outcome
    {
      public Outcome(string value, string consumed, IReadOnlyList<string> diagnostics, string? failure)
      {
        Value = value;
        Consumed = consumed;
        Diagnostics = diagnostics;
        Failure = failure;
      }

      /// <summary>The projected value, rendered deeply so a list is compared by its elements.</summary>
      public string Value { get; }

      public string Consumed { get; }

      /// <summary>What the parse noticed, in order — order is part of the claim.</summary>
      public IReadOnlyList<string> Diagnostics { get; }

      /// <summary>The failure's whole identity, or null where the declaration succeeded.</summary>
      public string? Failure { get; }
    }

    private static string RenderValue(object? value) => value switch
    {
      null => "<null>",
      string text => text,
      IEnumerable items => "[" + string.Join(", ", items.Cast<object?>().Select(RenderValue)) + "]",
      _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
    };

    private static string Describe(ShapeDiagnostic diagnostic)
      => $"{diagnostic.Severity} {diagnostic.Subject} at {diagnostic.Location.A1} in {diagnostic.Path}: {diagnostic.Message}";

    private static string Describe(ShapeException failure)
      => $"{failure.Message} | path={failure.Path} | at={failure.Location} "
       + $"| fault={failure.IsFault} | inner={failure.InnerException?.GetType().Name ?? "<none>"}";

    /// <summary>
    /// A scan that claims one column more than the space has. Nothing in the library spells this —
    /// the width of every incremental strategy is the available width — but the engine has a branch
    /// for it, and a branch the differential sweep never enters is a branch the sweep does not cover.
    /// </summary>
    private sealed class OverwideStrategy : IIncrementalAreaStrategy
    {
      public IAreaScan BeginArea(ISpace availableSpace) => new Scan(availableSpace.Area.Width + 1);

      public Area GetArea(ISpace availableSpace) => Scans.FoldArea(BeginArea(availableSpace), availableSpace);

      private sealed class Scan : IAreaScan
      {
        public Scan(int width) => Width = width;

        public int Width { get; }

        public bool IncludesRow(ISpace space, int row) => !space[0, row].IsBlank;
      }
    }
  }
}
