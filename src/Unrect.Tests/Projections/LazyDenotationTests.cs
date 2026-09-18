using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
  /// What survives of the pull interpreter's differential suite: the readings that hold whichever
  /// way an extent is discovered, kept because they pin values, consumed extents and diagnostics a
  /// caller can observe. The differential half — the same declaration run with extents measured up
  /// front and compared — retired with the pull interpreter.
  /// <para>
  /// The suite is only worth anything if the declarations it sweeps actually take the deferred
  /// branch, which is why the census at the bottom exists. That branch is reached by exactly one
  /// thing: an area strategy that is an a scan that answers row by row. Two families are —
  /// a per-row height at the full available width
  /// (<c>RowsWhileAnyValue()</c>/<c>RowsWhileAny(…)</c>, spelled through <c>Range(strategy, …)</c>
  /// or <c>.Sized(…)</c>), and, since the width/height interleave landed, a discovered block, which
  /// is what <c>Range(…)</c> and every table rung are placed by with nothing said. A declaration
  /// built on any other extent is eager on both runs and proves nothing, so the census pins which
  /// spellings defer and which — deliberately — do not.
  /// </para>
  /// </summary>
  public class LazyDenotationTests
  {
    // --- The spaces every case is read over --------------------------------------------------------

    /// <summary>
    /// Three rows of values over two blank ones: a row-wise rule stops at row 3, which leaves both
    /// a discovered bound and undescribed space below it for the diagnostics to have something to
    /// say.
    /// </summary>
    private static ISheetCells Sheet() => Grid(new[,]
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

    /// <summary>
    /// A caption row over three body rows and two blank ones — the same shape as <see
    /// cref="Sheet"/> with a header on top, so the table rungs have captions to bind and
    /// undescribed space below.
    /// </summary>
    private static ISheetCells Headered() => Mixed(new object?[,]
    {
      { "Client", "Amount" },
      { "Acme", 10 },
      { "Beta", 20 },
      { "Gamma", 30 },
      { null, null },
      { null, null },
    });

    /// <summary>
    /// A hundred body rows whose second column is empty until row 50, under a caption row that
    /// names only the first — so a discovered width is not settled by the header and the walk
    /// deciding it has to read fifty rows to find out. The interleave's expensive case, and the one
    /// where the deferred reading and the measured one read most nearly the same amount of the
    /// sheet.
    /// </summary>
    private static ISheetCells LateWideningSheet()
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
    private static Func<Point<ISheetCells>, bool> BreaksOn(int marker)
      => cell => cell.Kind() == CellKind.Number && cell.Integer() == marker
        ? throw new InvalidOperationException("no")
        : true;

    /// <summary>A cell rule whose failure is the environment's, not the data's.</summary>
    private static Func<Point<ISheetCells>, bool> FaultsOn(int marker)
      => cell => cell.Kind() == CellKind.Number && cell.Integer() == marker
        ? throw new IOException("the disk stopped answering")
        : true;

    /// <summary>
    /// The sheet's 7 is the first cell of row 2, so a rule that breaks on it survives the first two
    /// rows: eagerly the scan reaches it before the projection starts, lazily only if something
    /// asks for row 2 or the engine forces the bound. That gap is where every "late break" case
    /// lives.
    /// </summary>
    private const int LateMarker = 7;

    /// <summary>A rule that breaks before it has looked at anything — the scan fails at its first row.</summary>
    private static IProjectionDefinition<ISheetCells, int> Breaks(Exception exception)
      => Range(RowsWhileAny(_ => throw exception), b => b.Height);

    // --- The cases -------------------------------------------------------------------------------

    private static Scenario Case(string name) => name switch
    {
      // The plain projections of the thing: a bound read in full, a bound read not at all, and a
      // bound that is not discovered at all so the sweep contains its own control.
      "discovered extent" => Scenario.Of(Range(RowsWhileAnyValue(), b => b.Height), Sheet()),
      "unread extent" => Scenario.Of(Range(RowsWhileAnyValue(), _ => 0), Sheet()),
      "full sheet" => Scenario.Of(Range(WholeExtent(), b => b.Height), Sheet()),

      // The same bound reached through a layout, which is where the deferred extent stops being the
      // root's and becomes a child's — and where a sibling's placement depends on what it consumed.
      "flow" => Scenario.Of(VerticalFlow(v =>
      {
        var rangeSlot = v.Next(Range(RowsWhileAnyValue(), b => b.Height));

        return v.Build(read2 => read2.Of(rangeSlot));
      }), Sheet()),
      "flow of two children" => Scenario.Of(
        VerticalFlow(v =>
        {
          var head = v.Next(Range(RowsWhileAnyValue(), b => b.Height));
          var tail = v.Next(Range(WholeExtent(), b => b.Height));

          return v.Build(read => read.Of(head) * 100 + read.Of(tail));
        }),
        Sheet()),

      // The other spelling of the same extent: .Sized replaces a projection's own area outright.
      "sized" => Scenario.Of(Sized(RowsWhileAnyValue()).Of(Range(b => b.Height)), Sheet()),
      "sized, unread" => Scenario.Of(Sized(RowsWhileAnyValue()).Of(Range(_ => 0)), Sheet()),

      // The one declaration already in the suite that goes through this branch, lifted verbatim
      // from ProjectionReExportTests: the grid has a blank column, so the discovered extent is the
      // full width over both rows and the projection is a string rather than a number.
      "the re-export suite's .Sized declaration" => Scenario.Of(
        Sized(RowsWhileAnyValue()).Of(Range(b => $"{b.Width}x{b.Height}")),
        Grid(new[,] { { 1, 0, 3 }, { 2, 0, 4 } })),

      // Reads that force through the view rather than through the space: the block's own members
      // are dimension queries, so these are the cases where the projection settles the bound
      // itself.
      "rows enumerated" => Scenario.Of(Range(RowsWhileAnyValue(), b => b.Rows.Count), Sheet()),
      "block width" => Scenario.Of(Range(RowsWhileAnyValue(), b => b.Width), Sheet()),

      // The boundary of the discovered extent, from both sides. Reading its last row is ordinary;
      // reading the row below it is an overrun, and must be the same overrun either way.
      "last row of the bound read" => Scenario.Of(Range(RowsWhileAnyValue(), b => b.Space[0, 2].Integer()), Sheet()),
      "read past the bound" => Scenario.Of(Range(RowsWhileAnyValue(), b => b.Space[0, 4].IntegerOrBlank()), Sheet()),

      // A hundred-row bound of which the projection reads three: the case the whole feature is for,
      // and the one where the two runs read the most different amounts of the sheet.
      "tall bound, three rows read" => Scenario.Of(
        Range(RowsWhileAnyValue(), b => b.Space[0, 0].Integer() + b.Space[0, 1].Integer() + b.Space[0, 2].Integer()),
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

      // The absorbable twin of each: a warning either way, with the same subject and the same
      // reason.
      "breaking under Optional" => Scenario.Of(Breaks(new InvalidOperationException("no")).Optional(), Sheet()),
      "breaking under Else" => Scenario.Of(Range(RowsWhileAny(BreaksOn(LateMarker)), b => b.Height).Else(-1), Sheet()),

      // Rule 2: a repeat's item is placed non-strictly and therefore never defers. What is under
      // test here is that saying so changed nothing about what the repeat produces.
      "repeat of discovered items" => Scenario.Of(
        VerticalRepeat(Range(RowsWhileAnyValue(), b => b.Height), separatedBy: BlankRows()).Select(items => items.Count),
        Sheet()),
      "repeat requiring one" => Scenario.Of(
        VerticalRepeat(Range(RowsWhileAnyValue(), b => b.Height), separatedBy: BlankRows(), atLeast: 1).Select(items => items.Count),
        Sheet()),

      // A scan claiming one column more than there is. The engine declines to bind rather than
      // reporting the overrun itself, because saying so needs the height the eager reading
      // measured.
      "overwide scan" => Scenario.Of(Range(new OverwideStrategy(), b => b.Height), Sheet()),

      // The scan breaks on row 2, which the projection never reads: eagerly the break happens at
      // placement, lazily only when the engine forces the bound after projecting. The moment
      // differs and nothing else may.
      "late break, unread extent" => Scenario.Of(Range(RowsWhileAny(BreaksOn(LateMarker)), _ => 0), Sheet()),
      "late break under Optional" => Scenario.Of(Range(RowsWhileAny(BreaksOn(LateMarker)), _ => 0).Optional(), Sheet()),
      "late break in a Choice" => Scenario.Of(
        Choice(Range(RowsWhileAny(BreaksOn(LateMarker)), _ => 0), Range(WholeExtent(), b => b.Height)),
        Sheet()),

      // The three table rungs, which since step 6 read their bodies through TableView.StreamRows.
      // .Sized fixes the width at the available one, which is the simplest extent that defers and
      // so the one the rungs are swept on; the default placement — a discovered block, deferred
      // since the interleave landed — is swept separately below. What is under test is that a
      // streamed body means what an indexed one meant: the same rows, in order, with the same space
      // left over.
      "table rows, lambda" => Scenario.Of(
        Sized(RowsWhileAnyValue()).Of(Table(row => $"{row["Client"].Text()}={row["Amount"].Integer()}")),
        Headered()),
      "table rows, typed" => Scenario.Of(Sized(RowsWhileAnyValue()).Of(Table<Entry>()), Headered()),
      // .Sized before .Select on purpose: Select's wrapper is a projection with a placement of its
      // own, so sizing the wrapper would leave the table inside it placed by its own eager default.
      "table rows, dictionaries" => Scenario.Of(
        Sized(RowsWhileAnyValue()).Of(Table()).Select(rows => rows.Select(row => $"{row["Client"]}/{row["Amount"]}").ToList()),
        Headered()),

      // The row-projection slot, which reads its body as a tiler of one-row bands — the walk a
      // forcing regression would show up on first. Three shapes of record: a whole band read by a
      // flow, a band read in part, and a headered table whose header is consumed rather than
      // projected.
      "table with a row projection" => Scenario.Of(
        Sized(RowsWhileAnyValue()).Of(Table(0, eachRow: HorizontalFlow(h =>
        {
          var intCell = h.Next(IntCell());
          var intCell2 = h.Next(IntCell());

          return h.Build(read => $"{read.Of(intCell)}/{read.Of(intCell2)}");
        }))),
        Sheet()),
      "row projection reading part of its band" => Scenario.Of(
        Sized(RowsWhileAnyValue()).Of(Table(0, eachRow: IntCell())),
        Sheet()),
      "row projection under a consumed header" => Scenario.Of(
        Sized(RowsWhileAnyValue()).Of(Table(1, eachRow: IntCell())),
        Sheet()),

      // A hundred records of which each reads one cell: the slot form of the case the whole feature
      // is for, and the one where the two runs read the most different amounts of the sheet.
      "tall table of records" => Scenario.Of(
        Sized(RowsWhileAnyValue()).Of(Table(0, eachRow: IntCell())).Select(records => records.Count),
        TallSheet()),

      // A record that cannot be described, and the same record tolerated. Eagerly the bound is
      // settled before the first record is reached; lazily the failure arrives mid-walk. Same
      // failure, same path, same cell — including the record index in it.
      "record that fails" => Scenario.Of(
        Sized(RowsWhileAnyValue()).Of(Table(0, eachRow: Text())),
        Sheet()),
      "record that fails, tolerated" => Scenario.Of(
        Sized(RowsWhileAnyValue()).Of(Table(0, eachRow: Text())).Optional(),
        Sheet()),

      // The undecorated slot form, which is the one people write: a discovered block, deferred since
      // the width/height interleave landed.
      "table with a row projection, default placement" => Scenario.Of(
        Table(0, eachRow: HorizontalFlow(h =>
        {
          var intCell = h.Next(IntCell());
          var intCell2 = h.Next(IntCell());

          return h.Build(read => $"{read.Of(intCell)}/{read.Of(intCell2)}");
        })),
        Sheet()),

      // The table view reached directly, where the projection asks for the dimension query the
      // three rungs are written to avoid — so the forcing read has to denote what the streaming one
      // does.
      "table, rows materialised" => Scenario.Of(
        Sized(RowsWhileAnyValue()).Of(Table(table => $"{table.RowCount}x{table.ColumnCount}")),
        Headered()),

      // A body read in part: the case where the two runs read the most different amounts of a
      // table, and the one where a cached Rows and an uncached StreamRows could most easily
      // disagree.
      "table, first row only" => Scenario.Of(
        Sized(RowsWhileAnyValue()).Of(Table(table => table.StreamRows().First()["Client"].Text())),
        Headered()),

      // The declarations nobody decorates, which the width/height interleave brought onto the
      // deferred branch: a block and a table placed by their own defaults, where the width is
      // discovered alongside the height rather than taken as the available one. The sweep's job is
      // the same as ever — the two-pass reading and the one-walk reading must denote the same thing
      // — but these are the spellings most declarations in the corpus are actually written in.
      "block, default placement" => Scenario.Of(Range(b => $"{b.Width}x{b.Height}"), Sheet()),
      "block, default placement, unread" => Scenario.Of(Range(_ => 0), Sheet()),
      "table rows typed, default placement" => Scenario.Of(Table<Entry>(), Headered()),
      "table rows lambda, default placement" => Scenario.Of(
        Table(row => $"{row["Client"].Text()}={row["Amount"].Integer()}"),
        Headered()),

      // The same default placement over a sheet whose width is NOT settled by its first row: column
      // 1 carries nothing until row 50, so the walk that decides the width forces most of the bound
      // before the projection starts — §11.4's "the width decision forces the whole bound,
      // correctly and honestly". What that costs is LazyForcingTests' business; what is under test
      // here is that paying it changes nothing a caller can observe.
      "table whose width settles late" => Scenario.Of(
        Table(row => row.Index).Select(rows => $"{rows.Count}:{rows[0]}..{rows[rows.Count - 1]}"),
        LateWideningSheet()),

      // A bound inside a wrapper whose own placement reads the sheet to find its landmark.
      "discovered extent under Until" => Scenario.Of(
        Until(RowWhere((space, row) => space[0, row].IsBlank), orEnd: true).Of(Range(RowsWhileAnyValue(), b => b.Height)),
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
    [InlineData("table with a row projection")]
    [InlineData("row projection reading part of its band")]
    [InlineData("row projection under a consumed header")]
    [InlineData("tall table of records")]
    [InlineData("record that fails")]
    [InlineData("record that fails, tolerated")]
    [InlineData("table with a row projection, default placement")]
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

    private static int RowsReadBeforeTheProjectionRuns(Func<Func<CellBlock<ISheetCells>, int>, IProjectionDefinition<ISheetCells, int>> declare, ISheetCells sheet, bool eager)
    {
      var counter = new CountingSpace(sheet);
      var observed = -1;
      var projection = declare(_ =>
      {
        observed = counter.RowsTouched;

        return 0;
      });

      if (eager)
      {
          projection.Apply(counter);
      }
      else
      {
        projection.Apply(counter);
      }

      return observed;
    }

    [Theory]
    [InlineData("Table(row lambda)")]
    [InlineData("Table<T>()")]
    [InlineData("Table()")]
    [InlineData("Table(view lambda)")]
    [InlineData("Table(0, eachRow)")]
    [InlineData("Table(1, eachRow)")]
    public void TheTableDeclarationsThisSuiteSweepsDoTakeTheDeferredBranch(string rung)
    {
      // The table half of the census. The probe is structural rather than observational because two
      // of the three rungs project the whole body themselves and give a test nowhere to stand; the
      // observational reading of the same claim is LazyForcingTests, which watches a Table
      // lambda project its first row having read two rows of the sheet.
      // The rungs project different types, so each is reduced to the pair of placements the claim
      // is actually about before the switch has to agree on one.
      static (IProjectionDefinition Declared, IProjectionDefinition Sized) Probe<T>(IProjectionDefinition<ISheetCells, T> projection) => (projection, Sized(RowsWhileAnyValue()).Of(projection));

      var (declared, sized) = rung switch
      {
        "Table(row lambda)" => Probe(Table(row => row.Index)),
        "Table<T>()" => Probe(Table<Entry>()),
        "Table()" => Probe(Table()),
        "Table(view lambda)" => Probe(Table(table => table.RowCount)),
        "Table(0, eachRow)" => Probe(Table(0, IntCell())),
        "Table(1, eachRow)" => Probe(Table(1, IntCell())),

        _ => throw new ArgumentOutOfRangeException(nameof(rung), rung, "No such rung."),
      };

      // Both, and .Sized is no longer the difference. A table's own extent is a discovered block,
      // whose scan answers a row at a time — so every rung streams as written, and .Sized now
      // changes only how the width is arrived at (taken as the available one, rather than walked
      // for) and not whether the height can be discovered.
      Assert.True(declared.Placement.Area!.Begin(Orientation.Vertical).Incremental);
      Assert.True(sized.Placement.Area!.Begin(Orientation.Vertical).Incremental);
    }

    /// <summary>
    /// <see cref="Sheet"/> with a hole in its first row, so an "any" column rule cannot settle the
    /// width there and the walk has to take a second row to find it.
    /// </summary>
    private static ISheetCells HoledSheet() => Grid(new[,]
    {
      { 1, 0, 3 },
      { 4, 5, 6 },
      { 7, 8, 9 },
      { 0, 0, 0 },
      { 0, 0, 0 },
    });

    // --- What a run of one declaration is compared on ----------------------------------------------

    /// <summary>One declaration over one space, ready to be read either way.</summary>
    private sealed class Scenario
    {
      private Scenario(Func<Outcome> observe) => Observe = observe;

      private Func<Outcome> Observe { get; }

      public static Scenario Of<T>(IProjectionDefinition<ISheetCells, T> projection, ISheetCells space) => new Scenario(() => Read(projection, space));

      public Outcome Lazily() => Observe();

      public Outcome Eagerly()
      {
          return Observe();
      }

      private static Outcome Read<T>(IProjectionDefinition<ISheetCells, T> projection, ISheetCells space)
      {
        try
        {
          // Two calls because the two entry points answer different halves of the question: the
          // value and what the parse noticed come from one, the extent consumed from the other. A
          // projection is a value safe to apply twice, so this is one reading asked about twice,
          // not two readings.
          var mapped = projection.MapWithDiagnostics(space);
          var applied = projection.Apply(space);

          return new Outcome(
            RenderValue(mapped.Value),
            $"{applied.Consumed.Width}x{applied.Consumed.Height}",
            mapped.Diagnostics.Select(Describe).ToList(),
            null);
        }
        catch (ProjectionException failure)
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

    private static string Describe(ProjectionDiagnostic diagnostic)
      => $"{diagnostic.Severity} {diagnostic.Subject} at {diagnostic.Location.A1} in {diagnostic.Path}: {diagnostic.Message}";

    private static string Describe(ProjectionException failure)
      => $"{failure.Message} | path={failure.Path} | at={failure.Location} "
       + $"| fault={failure.IsFault} | inner={failure.InnerException?.GetType().Name ?? "<none>"}";

    /// <summary>
    /// A scan that claims one column more than the space has. Nothing in the library spells this —
    /// the width of every incremental strategy is the available width — but the engine has a branch
    /// for it, and a branch the differential sweep never enters is a branch the sweep does not
    /// cover.
    /// </summary>
    private sealed class OverwideStrategy : IAreaStrategy
    {
      public ISizeScan Begin(Orientation along) => new Scan();

      private sealed class Scan : ISizeScan
      {
        public bool Incremental => true;

        public bool Take(Plane<ISpace> region, int taken) => !region[0, taken].IsBlank;

        public int? Across(Plane<ISpace> region, int taken, bool final) => region.Width + 1;

        public int Along(Plane<ISpace> region, int taken) => taken;

        public bool Complete(int taken) => true;

        public Size Declared => default;
      }
    }
  }
}
