using System;

using Unrect.Core;
using Unrect.Strategies;

using Xunit;

using static Unrect.Tests.ShapeTestSpaces;

namespace Unrect.Tests
{
  /// <summary>
  /// A strategy that is genuinely a per-row rule can be read one row at a time, so a bound can be
  /// discovered as a projection consumes it instead of measured before the projection starts. These
  /// tests pin the two halves of that claim.
  /// <para>
  /// The first half is the <em>fold identity</em>: a strategy's eager answer is the fold of its own
  /// scan to exhaustion, on every shape of grid. The interfaces make this true by construction — the
  /// eager method is a default implementation written as the fold — so what is under test is that no
  /// implementation quietly supplies its own eager loop that says something else, and that the scans
  /// themselves mean what they claim to.
  /// </para>
  /// <para>
  /// The identity is claimed at three widths: a row rule's count, a size's width-and-height, and —
  /// since the width/height interleave landed — a rows-then-columns area, where one forward walk
  /// answers both dimensions and has to agree with the two passes it replaced.
  /// </para>
  /// <para>
  /// The second half is the <em>census</em>: which factories hand back an incremental strategy and
  /// which deliberately do not. A strategy that reads no cells has nothing to defer, and an explicit
  /// count's <see cref="OutOfBoundsException"/> on overrun is a promise about the available height —
  /// which a scan is never told. Those are pinned as hard negatives so that a later, helpful
  /// implementation of incrementality on one of them has to come and argue with a failing test.
  /// </para>
  /// </summary>
  public class IncrementalStrategyTests
  {
    private static bool HasValue(CellValue value) => value.HasValue;

    // --- The four grids every fold is folded over ------------------------------------------------

    private static ISpace Space(string name) => name switch
    {
      // Every cell carries a value, so every row-wise rule runs to the bottom.
      "dense" => Grid(new[,]
      {
        { 1, 2, 3 },
        { 4, 5, 6 },
        { 7, 8, 9 },
        { 10, 11, 12 },
      }),

      // Values here and there, with one row carrying none: where an "any" rule stops, an "all" rule
      // never started, and a rule reading column 0 stops a row earlier still.
      "sparse" => Grid(new[,]
      {
        { 1, 0, 0 },
        { 0, 0, 2 },
        { 0, 0, 0 },
        { 3, 0, 0 },
      }),

      // Three columns and no rows at all: the fold's body never runs, and every answer is zero
      // however the rule would have decided.
      "empty" => Grid(new int[0, 3]),

      "blank" => Grid(new[,]
      {
        { 0, 0, 0 },
        { 0, 0, 0 },
      }),

      // --- and three more, for the interleave, where WHEN a column answer settles is the point -----

      // One value per row, each a column further right: the case the interleave exists to be honest
      // about. An "any" rule cannot know its width until the third row, so the walk that decides the
      // width consumes the whole band — and the height that follows is then already known.
      "staircase" => Grid(new[,]
      {
        { 1, 0, 0 },
        { 0, 2, 0 },
        { 0, 0, 3 },
      }),

      // A single row of content over a blank one, with real rows below it that the row rule never
      // reaches: the width must come from the one accepted row and not from the sheet.
      "ragged" => Grid(new[,]
      {
        { 1, 2, 0 },
        { 0, 0, 0 },
        { 5, 6, 7 },
      }),

      // A full first row and a hole in the last column of the second: the "any" answer settles on row
      // 0 and the "all" answer needs row 1, so one grid separates the two rules by when they settle.
      "gap" => Grid(new[,]
      {
        { 1, 2, 3 },
        { 4, 5, 0 },
        { 0, 0, 0 },
      }),

      _ => throw new ArgumentOutOfRangeException(nameof(name), name, "No such grid."),
    };

    /// <summary>
    /// The fold, written out here rather than called from <see cref="Scans.Fold"/>, so the test
    /// says independently what every implementation's one-line delegation claims.
    /// </summary>
    private static int Fold(IRowScan scan, ISpace space)
    {
      var count = 0;

      while (count < space.Area.Height && scan.IncludesRow(space, count))
        count++;

      return count;
    }

    // --- The row layer ---------------------------------------------------------------------------

    private static IRowStrategy RowStrategy(string name) => name switch
    {
      "TakeRowsWhile" => RowStrategies.TakeRowsWhile((space, row) => space[0, row].HasValue),
      "TakeRowsWhile(column)" => RowStrategies.TakeRowsWhile(0, (cell, _) => cell.HasValue),
      "TakeRowsWhileAll" => RowStrategies.TakeRowsWhileAll(HasValue),
      "TakeRowsWhileAny" => RowStrategies.TakeRowsWhileAny(HasValue),
      "TakeRowsWhileAnyValue" => RowStrategies.TakeRowsWhileAnyValue(),
      "TakeRowsTo" => RowStrategies.TakeRowsTo((space, row) => space[0, row].IsBlank),
      "TakeRowsToValue" => RowStrategies.TakeRowsToValue(0, CellValue.Of(7)),
      "AllRows" => RowStrategies.AllRows(),

      _ => throw new ArgumentOutOfRangeException(nameof(name), name, "No such strategy."),
    };

    private static void AssertRowFoldIdentity(IRowStrategy strategy, ISpace space, int expected)
    {
      var incremental = Assert.IsAssignableFrom<IIncrementalRowStrategy>(strategy);

      Assert.Equal(expected, strategy.SelectRows(space));
      Assert.Equal(expected, Fold(incremental.BeginRows(), space));
    }

    [Theory]
    // The whole-row rules: "any" survives a row with a single value, "all" needs every cell.
    [InlineData("TakeRowsWhileAny", "dense", 4)]
    [InlineData("TakeRowsWhileAny", "sparse", 2)]
    [InlineData("TakeRowsWhileAny", "empty", 0)]
    [InlineData("TakeRowsWhileAny", "blank", 0)]
    [InlineData("TakeRowsWhileAnyValue", "dense", 4)]
    [InlineData("TakeRowsWhileAnyValue", "sparse", 2)]
    [InlineData("TakeRowsWhileAnyValue", "empty", 0)]
    [InlineData("TakeRowsWhileAnyValue", "blank", 0)]
    [InlineData("TakeRowsWhileAll", "dense", 4)]
    [InlineData("TakeRowsWhileAll", "sparse", 0)]
    [InlineData("TakeRowsWhileAll", "empty", 0)]
    [InlineData("TakeRowsWhileAll", "blank", 0)]
    // The two spellings that read one label column, and stop on the sparse grid's second row.
    [InlineData("TakeRowsWhile", "dense", 4)]
    [InlineData("TakeRowsWhile", "sparse", 1)]
    [InlineData("TakeRowsWhile", "empty", 0)]
    [InlineData("TakeRowsWhile", "blank", 0)]
    [InlineData("TakeRowsWhile(column)", "dense", 4)]
    [InlineData("TakeRowsWhile(column)", "sparse", 1)]
    [InlineData("TakeRowsWhile(column)", "empty", 0)]
    [InlineData("TakeRowsWhile(column)", "blank", 0)]
    // The stateful pair: the same scan object, keeping the matching row instead of stopping before
    // it. TakeRowsTo stops one row later than the TakeRowsWhile above, which is the state showing.
    [InlineData("TakeRowsTo", "dense", 4)]
    [InlineData("TakeRowsTo", "sparse", 2)]
    [InlineData("TakeRowsTo", "empty", 0)]
    [InlineData("TakeRowsTo", "blank", 1)]
    [InlineData("TakeRowsToValue", "dense", 3)]
    [InlineData("TakeRowsToValue", "sparse", 4)]
    [InlineData("TakeRowsToValue", "empty", 0)]
    [InlineData("TakeRowsToValue", "blank", 2)]
    // The constant rule, which stops only because the rows do.
    [InlineData("AllRows", "dense", 4)]
    [InlineData("AllRows", "sparse", 4)]
    [InlineData("AllRows", "empty", 0)]
    [InlineData("AllRows", "blank", 2)]
    public void ARowStrategysEagerAnswerIsTheFoldOfItsOwnScan(string strategy, string grid, int expected)
    {
      AssertRowFoldIdentity(RowStrategy(strategy), Space(grid), expected);
    }

    // --- The size layer, and the area it lifts to -------------------------------------------------
    //
    // A size is a width settled up front and a height that is a per-row rule, so the scan carries the
    // width and the fold answers the height. The area lift is the same scan under another name: an
    // Area is a Size read as an extent, so the two must never differ by anything but the type.

    private static ISizeStrategy SizeStrategy(string name) => name switch
    {
      "RowsWhileAnyValue" => SizeStrategies.RowsWhileAnyValue(),
      "RowsWhileAny" => SizeStrategies.RowsWhileAny(value => value.TryGetInt() < 7),

      _ => throw new ArgumentOutOfRangeException(nameof(name), name, "No such strategy."),
    };

    [Theory]
    [InlineData("RowsWhileAnyValue", "dense", 3, 4)]
    [InlineData("RowsWhileAnyValue", "sparse", 3, 2)]
    [InlineData("RowsWhileAnyValue", "empty", 3, 0)]
    [InlineData("RowsWhileAnyValue", "blank", 3, 0)]
    // The supplied predicate, which stops the dense grid two rows short of where blankness would —
    // so a fold that ignored the predicate and counted rows with values would fail here.
    [InlineData("RowsWhileAny", "dense", 3, 2)]
    [InlineData("RowsWhileAny", "sparse", 3, 2)]
    [InlineData("RowsWhileAny", "empty", 3, 0)]
    [InlineData("RowsWhileAny", "blank", 3, 0)]
    public void ASizeStrategysEagerAnswerIsTheFoldOfItsOwnScan(string strategy, string grid, int width, int height)
    {
      var space = Space(grid);
      var incremental = Assert.IsAssignableFrom<IIncrementalSizeStrategy>(SizeStrategy(strategy));

      var size = incremental.GetSize(space);
      var scan = incremental.BeginSize(space);

      Assert.Equal(width, size.Width);
      Assert.Equal(height, size.Height);

      Assert.Equal(width, scan.Width);
      Assert.Equal(height, Fold(scan, space));
    }

    [Theory]
    [InlineData("RowsWhileAnyValue", "dense", 3, 4)]
    [InlineData("RowsWhileAnyValue", "sparse", 3, 2)]
    [InlineData("RowsWhileAnyValue", "empty", 3, 0)]
    [InlineData("RowsWhileAnyValue", "blank", 3, 0)]
    [InlineData("RowsWhileAny", "dense", 3, 2)]
    [InlineData("RowsWhileAny", "sparse", 3, 2)]
    [InlineData("RowsWhileAny", "empty", 3, 0)]
    [InlineData("RowsWhileAny", "blank", 3, 0)]
    public void TheAreaLiftCarriesTheScanAcrossUnchanged(string strategy, string grid, int width, int height)
    {
      var space = Space(grid);
      var lifted = Assert.IsAssignableFrom<IIncrementalAreaStrategy>(SizeStrategy(strategy).ToAreaStrategy());

      var area = lifted.GetArea(space);
      var scan = lifted.BeginArea(space);

      Assert.Equal(width, area.Width);
      Assert.Equal(height, area.Height);

      Assert.Equal(width, scan.Width);
      Assert.Equal(height, Fold(scan, space));
    }

    // --- Rows first, then columns inside them: one walk where there used to be two passes ----------
    //
    // The interleave's fold identity, and the widest of the three because its scan answers two
    // questions rather than one. A rows-then-columns extent measures rows over the full width and
    // then columns within the band they found, so the width depends on the row bound and yet has to
    // be handed out before the projection starts. One forward walk serves both: the rows the width
    // decision consumes are exactly the rows the height would have consumed first. What that buys is
    // only worth having if it says the same thing the two passes said, on every shape of grid —
    // including the ones where the column answer needs more than the first row to settle.

    private static IAreaStrategy RowsThenColumns(string columns) => AreaStrategies.RowsThenColumns(
      RowStrategies.TakeRowsWhileAnyValue(),
      columns switch
      {
        "TakeColumnsWhileAnyValue" => ColumnStrategies.TakeColumnsWhileAnyValue(),
        "TakeColumnsWhileAll" => ColumnStrategies.TakeColumnsWhileAll(HasValue),

        _ => throw new ArgumentOutOfRangeException(nameof(columns), columns, "No such strategy."),
      });

    [Theory]
    // The "any" rule, whose answer only grows: settled at the full width, on row 0 wherever the
    // first row is dense. Sparse gives 1 because column 1 carries nothing in the two-row band, and
    // the run has to be contiguous from 0 — the value at column 2 is behind a gap and never counted.
    [InlineData("TakeColumnsWhileAnyValue", "dense", 3, 4)]
    [InlineData("TakeColumnsWhileAnyValue", "sparse", 1, 2)]
    [InlineData("TakeColumnsWhileAnyValue", "empty", 0, 0)]
    [InlineData("TakeColumnsWhileAnyValue", "blank", 0, 0)]
    [InlineData("TakeColumnsWhileAnyValue", "staircase", 3, 3)]
    [InlineData("TakeColumnsWhileAnyValue", "ragged", 2, 1)]
    [InlineData("TakeColumnsWhileAnyValue", "gap", 3, 2)]
    // The "all" rule, whose answer only shrinks: settled at zero, and on empty and blank it never
    // shrinks at all. Those two are the cases worth stating out loud — no row was accepted, so no
    // row ruled anything out, and the answer is the FULL width over no rows. A three-column extent
    // of zero height is not a contradiction; it is what "every cell of the column qualifies" says
    // when there are no cells, and both readings have to agree on it.
    [InlineData("TakeColumnsWhileAll", "dense", 3, 4)]
    [InlineData("TakeColumnsWhileAll", "sparse", 0, 2)]
    [InlineData("TakeColumnsWhileAll", "empty", 3, 0)]
    [InlineData("TakeColumnsWhileAll", "blank", 3, 0)]
    [InlineData("TakeColumnsWhileAll", "staircase", 0, 3)]
    [InlineData("TakeColumnsWhileAll", "ragged", 2, 1)]
    [InlineData("TakeColumnsWhileAll", "gap", 2, 2)]
    public void ARowsThenColumnsExtentsTwoPassAnswerIsTheFoldOfItsOneWalk(string columns, string grid, int width, int height)
    {
      var space = Space(grid);
      var incremental = Assert.IsAssignableFrom<IIncrementalAreaStrategy>(RowsThenColumns(columns));

      var area = incremental.GetArea(space);
      var scan = incremental.BeginArea(space);

      Assert.Equal(width, area.Width);
      Assert.Equal(height, area.Height);

      Assert.Equal(width, scan.Width);
      Assert.Equal(height, Fold(scan, space));
    }

    [Fact]
    public void AStatefulRowRuleIsNotAskedTwiceAboutARowTheWidthWalkAlreadyConsumed()
    {
      // What the replay inside the scan is for, made to bite. The staircase makes the column answer
      // need all three rows, so the width walk takes a verdict on all three; TakeRowsTo is the one
      // rule that remembers giving one — it keeps the matching row and then says no to everything
      // after it. Re-asking rather than replaying would therefore answer "no" from row 0 and report
      // an empty extent, which is the failure this arrangement is built to catch.
      var space = Space("staircase");
      var incremental = Assert.IsAssignableFrom<IIncrementalAreaStrategy>(AreaStrategies.RowsThenColumns(
        RowStrategies.TakeRowsTo((s, row) => s[2, row].HasValue),
        ColumnStrategies.TakeColumnsWhileAnyValue()));

      var area = incremental.GetArea(space);
      var scan = incremental.BeginArea(space);

      Assert.Equal(3, area.Width);
      Assert.Equal(3, area.Height);

      Assert.Equal(3, scan.Width);
      Assert.Equal(3, Fold(scan, space));
    }

    // --- The width is settled at the beginning, and reading it is not a step ----------------------

    [Fact]
    public void ReadingAScansWidthConsumesNothing()
    {
      // A width is not a cursor. The contract lets deciding it consume leading rows — a width
      // measured inside the discovered band would have to — but it must never consume rows the
      // height alone would not, and reading the property a second time must not consume at all.
      // Here the width is the whole of what is available, so the honest count of cells read is zero.
      var space = new CountingSpace(Space("sparse"));
      var strategy = Assert.IsAssignableFrom<IIncrementalSizeStrategy>(SizeStrategies.RowsWhileAnyValue());

      var scan = strategy.BeginSize(space);

      Assert.Equal(3, scan.Width);
      Assert.Equal(3, scan.Width);
      Assert.Equal(0, space.CellReads);
    }

    [Fact]
    public void AskingForTheWidthFirstDoesNotChangeTheHeight()
    {
      var space = Space("sparse");
      var strategy = Assert.IsAssignableFrom<IIncrementalSizeStrategy>(SizeStrategies.RowsWhileAnyValue());

      var afterReadingWidth = strategy.BeginSize(space);
      _ = afterReadingWidth.Width;

      Assert.Equal(Fold(strategy.BeginSize(space), space), Fold(afterReadingWidth, space));
    }

    [Fact]
    public void TheWidthIsTheSpaceTheScanWasBegunOn()
    {
      // Not the space it is folded over and not the strategy's own idea of a width: BeginSize takes
      // the available space and the answer comes from it, so a narrower band gives a narrower extent.
      var band = Space("dense").GetSubspace(default, new Area(2, 4));
      var strategy = Assert.IsAssignableFrom<IIncrementalSizeStrategy>(SizeStrategies.RowsWhileAnyValue());

      Assert.Equal(2, strategy.BeginSize(band).Width);
    }

    // --- A scan is one-shot, and BeginRows is how it starts over ----------------------------------

    [Fact]
    public void AStrategyThatCarriesStateAnswersTheSameEveryTimeItIsAsked()
    {
      // The regression the state bit invites. TakeRowsTo remembers that the matching row is behind
      // it, and that memory belongs to one reading of one space — if it survived into the next, the
      // second reading of the same declaration would report an empty extent.
      var strategy = RowStrategies.TakeRowsTo((space, row) => space[0, row].IsBlank);
      var space = Space("sparse");

      Assert.Equal(2, strategy.SelectRows(space));
      Assert.Equal(2, strategy.SelectRows(space));
      Assert.Equal(2, strategy.SelectRows(space));
    }

    [Fact]
    public void EachCallToBeginRowsIsAFreshReading()
    {
      var incremental = Assert.IsAssignableFrom<IIncrementalRowStrategy>(
        RowStrategies.TakeRowsTo((space, row) => space[0, row].IsBlank));

      var space = Space("sparse");

      Assert.Equal(2, Fold(incremental.BeginRows(), space));
      Assert.Equal(2, Fold(incremental.BeginRows(), space));
    }

    [Fact]
    public void AScanIsOneShot_SoASecondFoldThroughTheSameOneIsNotTheFirst()
    {
      // Why BeginRows exists at all, stated as a test rather than left as prose. Once the scan has
      // reported the extent over it does not un-remember, so a caller that wants to read again asks
      // for a new scan. Nothing in the library reuses one; this pins what would happen if it did.
      var incremental = Assert.IsAssignableFrom<IIncrementalRowStrategy>(
        RowStrategies.TakeRowsTo((space, row) => space[0, row].IsBlank));

      var space = Space("sparse");
      var scan = incremental.BeginRows();

      Assert.Equal(2, Fold(scan, space));
      Assert.Equal(0, Fold(scan, space));
    }

    [Fact]
    public void AStatelessScanHasNothingToLeak()
    {
      // The other side of the same coin: a rule that carries nothing from row to row may hand back
      // one instance for every scan of it, and folding through that instance twice is folding twice.
      var incremental = Assert.IsAssignableFrom<IIncrementalRowStrategy>(RowStrategies.TakeRowsWhileAnyValue());

      var space = Space("sparse");
      var scan = incremental.BeginRows();

      Assert.Equal(2, Fold(scan, space));
      Assert.Equal(2, Fold(scan, space));
    }

    // --- The census: which factories are incremental, and which must never become so ---------------

    [Theory]
    [InlineData("TakeRowsWhile")]
    [InlineData("TakeRowsWhile(column)")]
    [InlineData("TakeRowsWhileAll")]
    [InlineData("TakeRowsWhileAny")]
    [InlineData("TakeRowsWhileAnyValue")]
    [InlineData("TakeRowsTo")]
    [InlineData("TakeRowsToValue")]
    [InlineData("AllRows")]
    public void EveryRowStrategyThatIsAPerRowRuleIsIncremental(string strategy)
    {
      Assert.IsAssignableFrom<IIncrementalRowStrategy>(RowStrategy(strategy));
    }

    [Theory]
    [InlineData("RowsWhileAnyValue")]
    [InlineData("RowsWhileAny")]
    public void EverySizeStrategyThatLiftsAPerRowRuleIsIncremental(string strategy)
    {
      Assert.IsAssignableFrom<IIncrementalSizeStrategy>(SizeStrategy(strategy));
      Assert.IsAssignableFrom<IIncrementalAreaStrategy>(SizeStrategy(strategy).ToAreaStrategy());
    }

    [Fact]
    public void AnExplicitRowCountIsNotIncremental()
    {
      // And must not become one, however per-row it looks. TakeRows(n) reads no cells, so deferring
      // it buys nothing; and it throws when n does not fit, which is a promise about the AVAILABLE
      // height. A scan is never told the available height, so the promise is not expressible one row
      // at a time — it would silently become "as many of the n rows as there turned out to be".
      Assert.IsNotAssignableFrom<IIncrementalRowStrategy>(RowStrategies.TakeRows(3));
    }

    [Theory]
    [InlineData("MaxSize")]
    [InlineData("MinSize")]
    [InlineData("ExplicitSize")]
    [InlineData("ColumnsWhileAnyValue")]
    [InlineData("SelectSize")]
    public void ASizeThatIsNotAPerRowRuleIsNotIncremental(string strategy)
    {
      // Each for its own reason. The first three read no cells at all, so there is nothing to defer.
      // ColumnsWhileAny reads down the full height of a column to decide a width, which is the one
      // shape of question a lazily bounded space cannot answer without resolving itself. SelectSize
      // is the escape hatch: an opaque function of the whole space, which is exactly what a scan is
      // not. The area lift must not invent incrementality for any of them either.
      ISizeStrategy size = strategy switch
      {
        "MaxSize" => SizeStrategies.MaxSize(),
        "MinSize" => SizeStrategies.MinSize(),
        "ExplicitSize" => SizeStrategies.ExplicitSize(2, 2),
        "ColumnsWhileAnyValue" => SizeStrategies.ColumnsWhileAnyValue(),
        "SelectSize" => SizeStrategies.SelectSize(space => space.Area.Size),

        _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "No such strategy."),
      };

      Assert.IsNotAssignableFrom<IIncrementalSizeStrategy>(size);
      Assert.IsNotAssignableFrom<IIncrementalAreaStrategy>(size.ToAreaStrategy());
    }

    [Fact]
    public void AnOffsetIsNeverIncremental_EvenWhenTheSizeBehindItIs()
    {
      // An offset has to be resolved before the extent it displaces exists, so there is nothing to
      // defer it past. The lift is where that is decided: the same size strategy carries its scan
      // across ToAreaStrategy and drops it at ToOffsetStrategy.
      var size = SizeStrategies.RowsWhileAnyValue();

      Assert.IsAssignableFrom<IIncrementalSizeStrategy>(size);

      Assert.IsNotAssignableFrom<IIncrementalSizeStrategy>(size.ToOffsetStrategy());
      Assert.IsNotAssignableFrom<IIncrementalRowStrategy>(size.ToOffsetStrategy());
      Assert.IsNotAssignableFrom<IIncrementalSizeStrategy>(OffsetStrategies.SkipBlankRows());
    }

    [Theory]
    [InlineData("TakeColumnsWhileAnyValue")]
    [InlineData("TakeColumnsWhileAll")]
    public void AWidthMeasuredInsideTheDiscoveredBandIsIncremental(string columns)
    {
      // Rows first, then columns within them: the width depends on the row bound, so one forward
      // walk serves both scans before the area is handed out. This was pinned as a negative until the
      // interleave landed, and is the census entry that says it did — Table's default placement is
      // this extent, so it is the one that decides whether an undecorated declaration can stream.
      Assert.IsAssignableFrom<IIncrementalAreaStrategy>(RowsThenColumns(columns));
    }

    [Fact]
    public void AWidthMeasuredDownTheWholeAvailableHeightIsNotIncremental()
    {
      // Columns first, then rows within them — and deliberately never incremental, which is a
      // decision and not an unfinished step. A column rule read down the full available height
      // decides the width from rows the row bound may never reach, and IAreaScan.Width may never
      // consume rows the height scan would not. The interleave has no reading of this to offer: the
      // walk it is built on is the row rule's, and here the row rule runs second.
      var area = AreaStrategies.ColumnsThenRows(
        ColumnStrategies.TakeColumnsWhileAnyValue(),
        RowStrategies.TakeRowsWhileAnyValue());

      Assert.IsNotAssignableFrom<IIncrementalAreaStrategy>(area);
    }

    [Fact]
    public void TheInterleaveNeedsBothHalvesToBePerRowRules()
    {
      // Which is why the choice is made once, in the factory. A column rule spelled as a predicate
      // over (space, column) may read its column down the whole height — the same reading
      // ColumnsThenRows is refused for — so rows-then-columns falls back to two passes rather than
      // claiming a discoverable bound it cannot deliver. The row half is refused for its own reason,
      // given above on AnExplicitRowCountIsNotIncremental: an explicit count promises something about
      // the available height, and a walk that stopped early would quietly weaken the promise.
      Assert.IsNotAssignableFrom<IIncrementalAreaStrategy>(AreaStrategies.RowsThenColumns(
        RowStrategies.TakeRowsWhileAnyValue(),
        ColumnStrategies.TakeColumnsWhile((space, column) => space[column, 0].HasValue)));

      Assert.IsNotAssignableFrom<IIncrementalAreaStrategy>(AreaStrategies.RowsThenColumns(
        RowStrategies.TakeRows(3),
        ColumnStrategies.TakeColumnsWhileAnyValue()));
    }
  }
}
