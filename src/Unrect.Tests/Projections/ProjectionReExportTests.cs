using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The single-import claim: <c>using static Unrect.Projections.ProjectionBuilders&lt;Unrect.Core.ICellSpace&gt;;</c> is all a
  /// declaration needs. Every re-export here forwards to a strategy factory, and each test proves
  /// the forwarding by behaviour rather than by reference — a re-export wired to the wrong strategy
  /// would compile.
  /// <para>
  /// The forwarding tests name the strategy factories they compare against, so they import
  /// <c>Unrect.Strategies</c>. The spelling tests below them do not use a single member of it —
  /// that is where the single-import claim is actually made, and where it would break.
  /// </para>
  /// <para>
  /// The lifts used to be re-exported here and are not any more: <c>OffsetStrategies.To</c>/
  /// <c>Past</c> stay public in <c>Unrect.Strategies</c>, and at projection level a landmark is
  /// placed by <c>On</c>/<c>Below</c>/<c>RightOf</c>, whose forwarding is pinned in
  /// <see cref="AnchorModifierTests"/> — where the anchors' own semantics are.
  /// </para>
  /// </summary>
  public class ProjectionReExportTests
  {
    private sealed record Line(string Client, DateTime When, decimal Amount);

    // 3 columns by 2 rows: 1 0 3 / 2 0 4 — a blank middle column, so column-wise and row-wise
    // discovery give different answers and a mis-wired re-export cannot hide.
    private static ICellSpace Patchy() => Grid(new[,] { { 1, 0, 3 }, { 2, 0, 4 } });

    private static ICellSpace Block() => Grid(new[,] { { 1, 2, 3 }, { 4, 5, 6 } });

    /// <summary>The extent a strategy resolves to on the patchy grid, as "WxH".</summary>
    private static string Measure(ISizeStrategy<ICellSpace> area)
    {
      var size = area.GetSize(Patchy());

      return $"{size.Width}x{size.Height}";
    }

    // --- Extents ------------------------------------------------------------------------------------

    [Fact]
    public void TheExtentReExportsForwardToTheirStrategies()
    {
      Assert.Equal(Measure(SizeStrategies.MaxSize<ICellSpace>()), Measure(WholeExtent()));
      Assert.Equal(Measure(SizeStrategies.MinSize<ICellSpace>()), Measure(NoExtent()));
      Assert.Equal(Measure(SizeStrategies.ExplicitSize<ICellSpace>(2, 1)), Measure(Extent(2, 1)));
    }

    [Fact]
    public void TheRowExtentReExportsForwardToTheirStrategies()
    {
      Assert.Equal(
        Measure(SizeStrategies.RowsWhileAnyIsNotBlank<ICellSpace>()),
        Measure(RowsWhileAnyIsNotBlank()));
    }

    [Fact]
    public void TheColumnExtentReExportsForwardToTheirStrategies()
    {
      Assert.Equal(
        Measure(SizeStrategies.ColumnsWhileAnyIsNotBlank<ICellSpace>()),
        Measure(ColumnsWhileAnyIsNotBlank()));
    }

    [Fact]
    public void TheRowAndColumnExtentsDisagreeOnThisGrid()
    {
      // The guard on the two tests above: if both re-exports were wired to the same strategy they
      // would still pass, so pin that the two axes genuinely see different things here.
      Assert.Equal("3x2", Measure(RowsWhileAnyIsNotBlank()));
      Assert.Equal("1x2", Measure(ColumnsWhileAnyIsNotBlank()));
    }

    // --- The transparency law -----------------------------------------------------------------------
    //
    // What a typed factory hands back IS the calculus's own object, wearing a demand. It introduces
    // no strategy of its own, so it cannot measure differently and it cannot lose an incremental
    // scan by wrapping one — the two failures a boxed rule could plausibly have.
    //
    // Every rule below is spelled twice with the SAME question, once through each door: a point over
    // a sheet still answers the canonical four, so the erased spelling is subsumed rather than
    // replaced, and the comparison is about the plumbing rather than about two rules that happen to
    // agree. What only the typed door can say — a cell's kind, its value — is measured where those
    // rules live.

    private static bool Valued<TSpace>(Point<TSpace> cell)
      where TSpace : class, ISpace
      => !cell.IsBlank();

    private static TheoryData<string> Data(string[] names)
    {
      var data = new TheoryData<string>();

      foreach (var name in names)
        data.Add(name);

      return data;
    }

    /// <summary>The two objects are the same thing: the same runtime type, so the same scan when asked.</summary>
    private static void AssertTransparent(object typed, object erased) => Assert.IsType(erased.GetType(), typed);

    private static (ISizeStrategy<ICellSpace> Typed, ISizeStrategy<ICellSpace> Erased) Extents(string name) => name switch
    {
      "RowsWhileAny" => (
        RowsWhileAny(cell => !cell.IsBlank()),
        SizeStrategies.RowsWhileAny<ICellSpace>(Valued)),
      "ColumnsWhileAny" => (
        ColumnsWhileAny(cell => !cell.IsBlank()),
        SizeStrategies.ColumnsWhileAny<ICellSpace>(Valued)),
      "RowsThenColumns" => (
        RowsThenColumns(TakeRowsWhileAny(cell => !cell.IsBlank()), TakeColumnsWhileAny(cell => !cell.IsBlank())),
        SizeStrategies.RowsThenColumns(
          RowStrategies.TakeRowsWhileAny<ICellSpace>(Valued),
          ColumnStrategies.TakeColumnsWhileAny<ICellSpace>(Valued))),
      "ColumnsThenRows" => (
        ColumnsThenRows(TakeColumnsWhileAny(cell => !cell.IsBlank()), TakeRowsWhileAny(cell => !cell.IsBlank())),
        SizeStrategies.ColumnsThenRows(
          ColumnStrategies.TakeColumnsWhileAny<ICellSpace>(Valued),
          RowStrategies.TakeRowsWhileAny<ICellSpace>(Valued))),

      _ => throw new ArgumentOutOfRangeException(nameof(name), name, "No such extent."),
    };

    /// <summary>The extent rules the law below covers, named so the census can count them.</summary>
    private static readonly string[] TheExtents =
      { "RowsWhileAny", "ColumnsWhileAny", "RowsThenColumns", "ColumnsThenRows" };

    public static TheoryData<string> Extent => Data(TheExtents);

    [Theory]
    [MemberData(nameof(Extent))]
    public void ATypedExtentIsTheExtentItBoxes(string name)
    {
      var (typed, erased) = Extents(name);

      Assert.Equal(Measure(erased), Measure(typed));
      AssertTransparent(typed, erased);
    }

    private static (ILineStrategy<ICellSpace> Typed, ILineStrategy<ICellSpace> Erased) RowRules(string name) => name switch
    {
      "TakeRowsWhile" => (
        TakeRowsWhile((region, row) => !region[0, row].IsBlank()),
        RowStrategies.TakeRowsWhile<ICellSpace>((region, row) => !region[0, row].IsBlank())),
      "TakeRowsWhile(column)" => (
        TakeRowsWhile(0, (cell, _) => !cell.IsBlank()),
        RowStrategies.TakeRowsWhile<ICellSpace>(0, (cell, _) => !cell.IsBlank())),
      "TakeRowsTo" => (
        TakeRowsTo((region, row) => !region[2, row].IsBlank()),
        RowStrategies.TakeRowsTo<ICellSpace>((region, row) => !region[2, row].IsBlank())),
      "TakeRowsWhileAll" => (
        TakeRowsWhileAll(cell => !cell.IsBlank()),
        RowStrategies.TakeRowsWhileAll<ICellSpace>(Valued)),
      "TakeRowsWhileAny" => (
        TakeRowsWhileAny(cell => !cell.IsBlank()),
        RowStrategies.TakeRowsWhileAny<ICellSpace>(Valued)),

      _ => throw new ArgumentOutOfRangeException(nameof(name), name, "No such rule."),
    };

    private static readonly string[] TheRowRules =
      { "TakeRowsWhile", "TakeRowsWhile(column)", "TakeRowsTo", "TakeRowsWhileAll", "TakeRowsWhileAny" };

    public static TheoryData<string> RowRule => Data(TheRowRules);

    [Theory]
    [MemberData(nameof(RowRule))]
    public void ATypedRowRuleIsTheRuleItBoxes(string name)
    {
      var (typed, erased) = RowRules(name);

      Assert.Equal(erased.SelectLines(Patchy()), typed.SelectLines(Patchy()));
      AssertTransparent(typed, erased);
    }

    private static (ILineStrategy<ICellSpace> Typed, ILineStrategy<ICellSpace> Erased) ColumnRules(string name) => name switch
    {
      "TakeColumnsWhile" => (
        TakeColumnsWhile((region, column) => !region[column, 0].IsBlank()),
        ColumnStrategies.TakeColumnsWhile<ICellSpace>((region, column) => !region[column, 0].IsBlank())),
      "TakeColumnsWhile(row)" => (
        TakeColumnsWhile(0, (cell, _) => !cell.IsBlank()),
        ColumnStrategies.TakeColumnsWhile<ICellSpace>(0, (cell, _) => !cell.IsBlank())),
      "TakeColumnsTo" => (
        TakeColumnsTo((region, column) => !region[column, 1].IsBlank()),
        ColumnStrategies.TakeColumnsTo<ICellSpace>((region, column) => !region[column, 1].IsBlank())),
      "TakeColumnsWhileAll" => (
        TakeColumnsWhileAll(cell => !cell.IsBlank()),
        ColumnStrategies.TakeColumnsWhileAll<ICellSpace>(Valued)),
      "TakeColumnsWhileAny" => (
        TakeColumnsWhileAny(cell => !cell.IsBlank()),
        ColumnStrategies.TakeColumnsWhileAny<ICellSpace>(Valued)),

      _ => throw new ArgumentOutOfRangeException(nameof(name), name, "No such rule."),
    };

    private static readonly string[] TheColumnRules =
      { "TakeColumnsWhile", "TakeColumnsWhile(row)", "TakeColumnsTo", "TakeColumnsWhileAll", "TakeColumnsWhileAny" };

    public static TheoryData<string> ColumnRule => Data(TheColumnRules);

    [Theory]
    [MemberData(nameof(ColumnRule))]
    public void ATypedColumnRuleIsTheRuleItBoxes(string name)
    {
      var (typed, erased) = ColumnRules(name);

      Assert.Equal(erased.SelectLines(Patchy()), typed.SelectLines(Patchy()));
      AssertTransparent(typed, erased);
    }

    private static (IOffsetStrategy<ICellSpace> Typed, IOffsetStrategy<ICellSpace> Erased) Offsets(string name) => name switch
    {
      "SkipRowsWhileAll" => (
        SkipRowsWhileAll(cell => !cell.IsBlank()),
        OffsetStrategies.SkipRowsWhileAll<ICellSpace>(Valued)),
      "SkipRowsWhileAny" => (
        SkipRowsWhileAny(cell => !cell.IsBlank()),
        OffsetStrategies.SkipRowsWhileAny<ICellSpace>(Valued)),
      "SkipColumnsWhileAll" => (
        SkipColumnsWhileAll(cell => !cell.IsBlank()),
        OffsetStrategies.SkipColumnsWhileAll<ICellSpace>(Valued)),
      "SkipColumnsWhileAny" => (
        SkipColumnsWhileAny(cell => !cell.IsBlank()),
        OffsetStrategies.SkipColumnsWhileAny<ICellSpace>(Valued)),
      "SelectOffset" => (
        SelectOffset(region => new Size(1, region.Extent.Height)),
        OffsetStrategies.SelectOffset<ICellSpace>(region => new Size(1, region.Extent.Height))),

      _ => throw new ArgumentOutOfRangeException(nameof(name), name, "No such offset."),
    };

    private static readonly string[] TheOffsets =
      { "SkipRowsWhileAll", "SkipRowsWhileAny", "SkipColumnsWhileAll", "SkipColumnsWhileAny", "SelectOffset" };

    public static TheoryData<string> Offset => Data(TheOffsets);

    [Theory]
    [MemberData(nameof(Offset))]
    public void ATypedOffsetIsTheOffsetItBoxes(string name)
    {
      var (typed, erased) = Offsets(name);

      Assert.Equal(erased.GetOffset(Patchy()), typed.GetOffset(Patchy()));
      AssertTransparent(typed, erased);
    }

    [Fact]
    public void ATypedSizeIsTheSizeItBoxes()
    {
      var typed = SelectSize(region => new Size(region.Width, 1));
      var erased = SizeStrategies.SelectSize<ICellSpace>(region => new Size(region.Width, 1));

      Assert.Equal(erased.GetSize(Patchy()), typed.GetSize(Patchy()));
      AssertTransparent(typed, erased);
    }

    private static readonly string[] TheRowMatchers = { "RowWhere", "RowWithCell" };

    public static TheoryData<string> RowMatcher => Data(TheRowMatchers);

    [Theory]
    [MemberData(nameof(RowMatcher))]
    public void ATypedRowMatcherIsTheMatcherItBoxes(string name)
    {
      var (typed, erased) = name == "RowWhere"
        ? (RowWhere((region, row) => !region[2, row].IsBlank()),
           RowLandmarks.RowWhere<ICellSpace>((region, row) => !region[2, row].IsBlank()))
        : (RowWithCell(cell => !cell.IsBlank()),
           RowLandmarks.RowWithCell<ICellSpace>(Valued));

      Assert.Equal(erased.Find(Patchy()), typed.Find(Patchy()));
      Assert.Equal(erased.Description, typed.Description);
      Assert.IsType(erased.GetType(), typed);
    }

    private static readonly string[] TheColumnMatchers = { "ColumnWhere", "ColumnWithCell" };

    public static TheoryData<string> ColumnMatcher => Data(TheColumnMatchers);

    [Theory]
    [MemberData(nameof(ColumnMatcher))]
    public void ATypedColumnMatcherIsTheMatcherItBoxes(string name)
    {
      var (typed, erased) = name == "ColumnWhere"
        ? (ColumnWhere((region, column) => !region[column, 0].IsBlank()),
           ColumnLandmarks.ColumnWhere<ICellSpace>((region, column) => !region[column, 0].IsBlank()))
        : (ColumnWithCell(cell => !cell.IsBlank()),
           ColumnLandmarks.ColumnWithCell<ICellSpace>(Valued));

      Assert.Equal(erased.Find(Patchy()), typed.Find(Patchy()));
      Assert.Equal(erased.Description, typed.Description);
      Assert.IsType(erased.GetType(), typed);
    }

    [Fact]
    public void TheTwoAxesMeetWhicheverOfThemNamesASpace()
    {
      // The combinators take each axis as it comes, so a rule from this vocabulary pairs with one
      // from the calculus without either being unwrapped — which is what lets a one-import file
      // write RowsThenColumns(TakeRows(3), AllColumns()) at all. All four spellings measure the same
      // region; only the demand they carry differs, and that is a compile-time matter.
      var typed = RowsThenColumns(TakeRowsWhileAny(cell => !cell.IsBlank()), TakeColumnsWhileAny(cell => !cell.IsBlank()));
      var erased = RowsThenColumns(RowStrategies.TakeRowsWhileAny<ICellSpace>(Valued), ColumnStrategies.TakeColumnsWhileAny<ICellSpace>(Valued));
      var typedRows = RowsThenColumns(TakeRowsWhileAny(cell => !cell.IsBlank()), ColumnStrategies.TakeColumnsWhileAny<ICellSpace>(Valued));
      var typedColumns = RowsThenColumns(RowStrategies.TakeRowsWhileAny<ICellSpace>(Valued), TakeColumnsWhileAny(cell => !cell.IsBlank()));

      Assert.Equal("1x2", Measure(erased));
      Assert.Equal(Measure(erased), Measure(typed));
      Assert.Equal(Measure(erased), Measure(typedRows));
      Assert.Equal(Measure(erased), Measure(typedColumns));

      // The spelling the erased/erased overload exists for: both axes from the re-exported
      // selectors, which name no space at all.
      Assert.Equal("3x2", Measure(RowsThenColumns(TakeRows(2), AllColumns())));
      Assert.Equal("3x2", Measure(ColumnsThenRows(AllColumns(), TakeRows(2))));
    }

    // --- Axis selectors -----------------------------------------------------------------------------

    [Fact]
    public void TheSelectorReExportsForwardToTheirStrategies()
    {
      Assert.Equal(RowStrategies.TakeRows<ICellSpace>(1).SelectLines(Block()), TakeRows(1).SelectLines(Block()));
      Assert.Equal(ColumnStrategies.TakeColumns<ICellSpace>(2).SelectLines(Block()), TakeColumns(2).SelectLines(Block()));
      Assert.Equal(RowStrategies.AllRows<ICellSpace>().SelectLines(Block()), AllRows().SelectLines(Block()));
      Assert.Equal(ColumnStrategies.AllColumns<ICellSpace>().SelectLines(Block()), AllColumns().SelectLines(Block()));
    }

    [Fact]
    public void AllRowsAndAllColumnsSeeTheWholeExtent()
    {
      Assert.Equal(2, AllRows().SelectLines(Block()));
      Assert.Equal(3, AllColumns().SelectLines(Block()));
    }

    // --- The spellings the re-exports exist for --------------------------------------------------------

    [Fact]
    public void AFullWidthRowIsSpelledWithAllColumns()
    {
      // The spelling that replaced an opaque (space, column) => true at the call site: a leaf
      // overload that already takes a column strategy, handed the one that means "all of them".
      Assert.Equal(3, Row(AllColumns(), s => s.Count).Map(Patchy()));

      // ...where the discovered default would have stopped at the blank column.
      Assert.Equal(1, Row(s => s.Count).Map(Patchy()));
    }

    [Fact]
    public void AFullHeightColumnIsSpelledWithAllRows()
    {
      var space = Grid(new[,] { { 1 }, { 0 }, { 3 } });

      Assert.Equal(3, Column(AllRows(), s => s.Count).Map(space));
      Assert.Equal(1, Column(s => s.Count).Map(space));
    }

    // --- The single-import claim, on whole declarations ---------------------------------------------------

    [Fact]
    public void ACaptionedSectionIsDeclarableFromTheOneImport()
    {
      // Where the single-import claim is actually made: Caption, Heading, On, Below and
      // RowContaining, with no member of Unrect.Strategies anywhere in the declaration. (Stronger
      // since the renovation: the old spelling reached To/Past through re-exports; the pipeline
      // entries ARE Projection's.)
      var space = Mixed(new object?[,]
      {
        { "junk" },
        { "Detail" },
        { "a" },
        { "b" },
      });

      var section = Heading("Detail").Of(Range(b => b.Height));
      var anchored = Below(RowContaining("Detail")).Of(TextCell());

      Assert.Equal(2, section.Map(space));
      Assert.Equal("a", anchored.Map(space));
      Assert.Equal("Detail", On(RowContaining("Detail")).Of(TextCell()).Map(space));
    }

    [Fact]
    public void ATypedTableAndALabelledBlockAreDeclarableFromTheOneImport()
    {
      // The phase C vocabulary, declared with nothing but `using static
      // Unrect.Projections.Projection`: the typed leaves, Table<T>, its binding lambda, Fields
      // and Field.
      var card = Mixed(new object?[,]
      {
        { "EIN:", "12-3456789", null },
        { null, null, null },
        { "Client", "Transaction Date", "Amount" },
        { "Acme", new DateTime(2026, 3, 4), 10m },
      });

      var report = VerticalFlow(v => new
      {
        Entity = v.Next(Fields(Field("EIN"))),
        Lines = v.Next(Table<Line>(bind => bind.Column(t => t.When, "Transaction Date"))),
      }).Map(card);

      Assert.Equal("12-3456789", report.Entity["EIN"].Text());
      Assert.Equal(new DateTime(2026, 3, 4), report.Lines[0].When);
      Assert.Equal(10m, report.Lines[0].Amount);

      // ...and the typed leaves, which are the other half of the phase's vocabulary.
      Assert.Equal("Acme", Down(3).Of(Text()).Map(card));
      Assert.Equal(10m, Down(3).Right(2).Of(Decimal()).Map(card));
    }

    [Fact]
    public void AReExportedExtentResolvesInsideSized()
    {
      // The single-import claim where it is most load-bearing: the Sized entry taking an
      // ISizeStrategy<ICellSpace>, handed a re-export, with no strategies import in scope at the call site.
      var projection = Sized(ColumnsWhileAnyIsNotBlank()).Of(Range(b => $"{b.Width}x{b.Height}"));

      Assert.Equal("1x2", projection.Map(Patchy()));
      Assert.Equal("3x2", Sized(RowsWhileAnyIsNotBlank()).Of(Range(b => $"{b.Width}x{b.Height}")).Map(Patchy()));
      Assert.Equal("2x1", Sized(Extent(2, 1)).Of(Range(b => $"{b.Width}x{b.Height}")).Map(Patchy()));
    }
  }
}
