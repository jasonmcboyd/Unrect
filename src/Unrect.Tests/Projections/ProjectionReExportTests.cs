using System;
using System.Linq;
using System.Reflection;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The single-import claim: <c>using static Unrect.Projections.ProjectionBuilders&lt;Unrect.Core.ISheetCells&gt;;</c> is all a
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
    private static ISheetCells Patchy() => Grid(new[,] { { 1, 0, 3 }, { 2, 0, 4 } });

    private static ISheetCells Block() => Grid(new[,] { { 1, 2, 3 }, { 4, 5, 6 } });

    /// <summary>The extent a strategy resolves to on the patchy grid, as "WxH".</summary>
    private static string Measure(IAreaStrategy area)
    {
      var size = area.GetArea(Patchy()).Size;

      return $"{size.Width}x{size.Height}";
    }

    // --- Extents ------------------------------------------------------------------------------------

    [Fact]
    public void TheExtentReExportsForwardToTheirStrategies()
    {
      Assert.Equal(Measure(AreaStrategies.MaxArea()), Measure(WholeExtent()));
      Assert.Equal(Measure(AreaStrategies.MinArea()), Measure(NoExtent()));
      Assert.Equal(Measure(AreaStrategies.ExplicitArea(2, 1)), Measure(Extent(2, 1)));
    }

    [Fact]
    public void TheRowExtentReExportsForwardToTheirStrategies()
    {
      Assert.Equal(
        Measure(SizeStrategies.RowsWhileAnyValue().ToAreaStrategy()),
        Measure(RowsWhileAnyValue()));
    }

    [Fact]
    public void TheColumnExtentReExportsForwardToTheirStrategies()
    {
      Assert.Equal(
        Measure(SizeStrategies.ColumnsWhileAnyValue().ToAreaStrategy()),
        Measure(ColumnsWhileAnyValue()));
    }

    [Fact]
    public void TheRowAndColumnExtentsDisagreeOnThisGrid()
    {
      // The guard on the two tests above: if both re-exports were wired to the same strategy they
      // would still pass, so pin that the two axes genuinely see different things here.
      Assert.Equal("3x2", Measure(RowsWhileAnyValue()));
      Assert.Equal("1x2", Measure(ColumnsWhileAnyValue()));
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

    private static bool Valued(Point<ISpace> cell) => cell.HasValue;

    private static TheoryData<string> Data(string[] names)
    {
      var data = new TheoryData<string>();

      foreach (var name in names)
        data.Add(name);

      return data;
    }

    /// <summary>
    /// The two objects say the same thing about their own nature: same runtime type, and the same
    /// answer to each incremental interface the engine type-tests for.
    /// </summary>
    private static void AssertTransparent(object typed, object erased)
    {
      Assert.IsType(erased.GetType(), typed);

      Assert.Equal(erased is IIncrementalAreaStrategy, typed is IIncrementalAreaStrategy);
      Assert.Equal(erased is IIncrementalRowStrategy, typed is IIncrementalRowStrategy);
      Assert.Equal(erased is IIncrementalSizeStrategy, typed is IIncrementalSizeStrategy);
    }

    private static (IAreaStrategy Typed, IAreaStrategy Erased) Extents(string name) => name switch
    {
      "RowsWhileAny" => (
        RowsWhileAny(cell => cell.HasValue).Strategy,
        SizeStrategies.RowsWhileAny(Valued).ToAreaStrategy()),
      "ColumnsWhileAny" => (
        ColumnsWhileAny(cell => cell.HasValue).Strategy,
        SizeStrategies.ColumnsWhileAny(Valued).ToAreaStrategy()),
      "SelectArea" => (
        SelectArea(region => new Size(region.Width, 1)).Strategy,
        AreaStrategies.SelectArea(region => new Size(region.Width, 1))),
      "RowsThenColumns" => (
        RowsThenColumns(TakeRowsWhileAny(cell => cell.HasValue), TakeColumnsWhileAny(cell => cell.HasValue)).Strategy,
        AreaStrategies.RowsThenColumns(
          RowStrategies.TakeRowsWhileAny(Valued),
          ColumnStrategies.TakeColumnsWhileAny(Valued))),
      "ColumnsThenRows" => (
        ColumnsThenRows(TakeColumnsWhileAny(cell => cell.HasValue), TakeRowsWhileAny(cell => cell.HasValue)).Strategy,
        AreaStrategies.ColumnsThenRows(
          ColumnStrategies.TakeColumnsWhileAny(Valued),
          RowStrategies.TakeRowsWhileAny(Valued))),

      _ => throw new ArgumentOutOfRangeException(nameof(name), name, "No such extent."),
    };

    /// <summary>The extent rules the law below covers, named so the census can count them.</summary>
    private static readonly string[] TheExtents =
      { "RowsWhileAny", "ColumnsWhileAny", "SelectArea", "RowsThenColumns", "ColumnsThenRows" };

    public static TheoryData<string> Extent => Data(TheExtents);

    [Theory]
    [MemberData(nameof(Extent))]
    public void ATypedExtentIsTheExtentItBoxes(string name)
    {
      var (typed, erased) = Extents(name);

      Assert.Equal(Measure(erased), Measure(typed));
      AssertTransparent(typed, erased);
    }

    private static (IRowStrategy Typed, IRowStrategy Erased) RowRules(string name) => name switch
    {
      "TakeRowsWhile" => (
        TakeRowsWhile((region, row) => region[0, row].HasValue).Strategy,
        RowStrategies.TakeRowsWhile((region, row) => region[0, row].HasValue)),
      "TakeRowsWhile(column)" => (
        TakeRowsWhile(0, (cell, _) => cell.HasValue).Strategy,
        RowStrategies.TakeRowsWhile(0, (cell, _) => cell.HasValue)),
      "TakeRowsTo" => (
        TakeRowsTo((region, row) => region[2, row].HasValue).Strategy,
        RowStrategies.TakeRowsTo((region, row) => region[2, row].HasValue)),
      "TakeRowsWhileAll" => (
        TakeRowsWhileAll(cell => cell.HasValue).Strategy,
        RowStrategies.TakeRowsWhileAll(Valued)),
      "TakeRowsWhileAny" => (
        TakeRowsWhileAny(cell => cell.HasValue).Strategy,
        RowStrategies.TakeRowsWhileAny(Valued)),

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

      Assert.Equal(erased.SelectRows(Patchy()), typed.SelectRows(Patchy()));
      AssertTransparent(typed, erased);
    }

    private static (IColumnStrategy Typed, IColumnStrategy Erased) ColumnRules(string name) => name switch
    {
      "TakeColumnsWhile" => (
        TakeColumnsWhile((region, column) => region[column, 0].HasValue).Strategy,
        ColumnStrategies.TakeColumnsWhile((region, column) => region[column, 0].HasValue)),
      "TakeColumnsWhile(row)" => (
        TakeColumnsWhile(0, (cell, _) => cell.HasValue).Strategy,
        ColumnStrategies.TakeColumnsWhile(0, (cell, _) => cell.HasValue)),
      "TakeColumnsTo" => (
        TakeColumnsTo((region, column) => region[column, 1].HasValue).Strategy,
        ColumnStrategies.TakeColumnsTo((region, column) => region[column, 1].HasValue)),
      "TakeColumnsWhileAll" => (
        TakeColumnsWhileAll(cell => cell.HasValue).Strategy,
        ColumnStrategies.TakeColumnsWhileAll(Valued)),
      "TakeColumnsWhileAny" => (
        TakeColumnsWhileAny(cell => cell.HasValue).Strategy,
        ColumnStrategies.TakeColumnsWhileAny(Valued)),

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

      Assert.Equal(erased.SelectColumns(Patchy()), typed.SelectColumns(Patchy()));
      AssertTransparent(typed, erased);
    }

    private static (IOffsetStrategy Typed, IOffsetStrategy Erased) Offsets(string name) => name switch
    {
      "SkipRowsWhileAll" => (
        SkipRowsWhileAll(cell => cell.HasValue).Strategy,
        OffsetStrategies.SkipRowsWhileAll(Valued)),
      "SkipRowsWhileAny" => (
        SkipRowsWhileAny(cell => cell.HasValue).Strategy,
        OffsetStrategies.SkipRowsWhileAny(Valued)),
      "SkipColumnsWhileAll" => (
        SkipColumnsWhileAll(cell => cell.HasValue).Strategy,
        OffsetStrategies.SkipColumnsWhileAll(Valued)),
      "SkipColumnsWhileAny" => (
        SkipColumnsWhileAny(cell => cell.HasValue).Strategy,
        OffsetStrategies.SkipColumnsWhileAny(Valued)),
      "SelectOffset" => (
        SelectOffset(region => new Size(1, region.Area.Height)).Strategy,
        OffsetStrategies.SelectOffset(region => new Size(1, region.Area.Height))),

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
      var typed = SelectSize(region => new Size(region.Width, 1)).Strategy;
      var erased = SizeStrategies.SelectSize(region => new Size(region.Width, 1));

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
        ? (RowWhere((region, row) => region[2, row].HasValue).Landmark,
           RowLandmarks.RowWhere((region, row) => region[2, row].HasValue))
        : (RowWithCell(cell => cell.HasValue).Landmark,
           RowLandmarks.RowWithCell(Valued));

      Assert.Equal(erased.FindRow(Patchy()), typed.FindRow(Patchy()));
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
        ? (ColumnWhere((region, column) => region[column, 0].HasValue).Landmark,
           ColumnLandmarks.ColumnWhere((region, column) => region[column, 0].HasValue))
        : (ColumnWithCell(cell => cell.HasValue).Landmark,
           ColumnLandmarks.ColumnWithCell(Valued));

      Assert.Equal(erased.FindColumn(Patchy()), typed.FindColumn(Patchy()));
      Assert.Equal(erased.Description, typed.Description);
      Assert.IsType(erased.GetType(), typed);
    }

    /// <summary>The seven interfaces a factory returns when what it hands back carries a demand.</summary>
    private static bool IsPhantom(Type type)
      => type.IsGenericType
        && type.Namespace == "Unrect.Projections"
        && new[]
        {
          typeof(IRowLandmark<>),
          typeof(IColumnLandmark<>),
          typeof(Unrect.Projections.ISizeStrategy<>),
          typeof(Unrect.Projections.IOffsetStrategy<>),
          typeof(Unrect.Projections.IAreaStrategy<>),
          typeof(Unrect.Projections.IRowStrategy<>),
          typeof(Unrect.Projections.IColumnStrategy<>),
        }.Contains(type.GetGenericTypeDefinition());

    [Fact]
    public void EveryFactoryThatHandsBackADemandHasATransparencyPin()
    {
      // The census that keeps the law above honest. The transparency claim is about plumbing, so it
      // is worth exactly as much as its coverage: a factory added to the vocabulary without a pin
      // would be the one whose boxing nobody checked. The expectation is read off the theories, so
      // adding a factory and adding its pin are the same edit.
      var pinned = TheExtents
        .Concat(TheRowRules)
        .Concat(TheColumnRules)
        .Concat(TheOffsets)
        .Concat(TheRowMatchers)
        .Concat(TheColumnMatchers)
        .Concat(new[] { "SelectSize" })
        // A name spelled twice in a theory is one family with two overloads; the parenthesis says
        // which overload, and the census counts the family.
        .Select(name => name.Split('(')[0])
        .Distinct(StringComparer.Ordinal)
        .OrderBy(name => name, StringComparer.Ordinal);

      var declared = typeof(ProjectionBuilders<ISheetCells>)
        .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(member => IsPhantom(member.ReturnType))
        .ToList();

      Assert.Equal(pinned, declared.Select(member => member.Name).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal));

      // ...and the families that carry more than one overload, so an overload cannot be added
      // without a pin either. Nineteen families, twenty-nine demanding overloads: TakeRowsWhile and
      // TakeColumnsWhile have two each (a region rule and a one-cell-per-line rule), and each
      // combinator has three (both axes demanding, and either one of them alone — the fourth
      // spelling, both axes erased, demands nothing and is not counted here).
      Assert.Equal(29, declared.Count);
    }

    [Fact]
    public void TheTwoAxesMeetWhicheverOfThemNamesASpace()
    {
      // The combinators take each axis as it comes, so a rule from this vocabulary pairs with one
      // from the calculus without either being unwrapped — which is what lets a one-import file
      // write RowsThenColumns(TakeRows(3), AllColumns()) at all. All four spellings measure the same
      // region; only the demand they carry differs, and that is a compile-time matter.
      var typed = RowsThenColumns(TakeRowsWhileAny(cell => cell.HasValue), TakeColumnsWhileAny(cell => cell.HasValue));
      var erased = RowsThenColumns(RowStrategies.TakeRowsWhileAny(Valued), ColumnStrategies.TakeColumnsWhileAny(Valued));
      var typedRows = RowsThenColumns(TakeRowsWhileAny(cell => cell.HasValue), ColumnStrategies.TakeColumnsWhileAny(Valued));
      var typedColumns = RowsThenColumns(RowStrategies.TakeRowsWhileAny(Valued), TakeColumnsWhileAny(cell => cell.HasValue));

      Assert.Equal("1x2", Measure(erased));
      Assert.Equal(Measure(erased), Measure(typed.Strategy));
      Assert.Equal(Measure(erased), Measure(typedRows.Strategy));
      Assert.Equal(Measure(erased), Measure(typedColumns.Strategy));

      // The spelling the erased/erased overload exists for: both axes from the re-exported
      // selectors, which name no space at all.
      Assert.Equal("3x2", Measure(RowsThenColumns(TakeRows(2), AllColumns())));
      Assert.Equal("3x2", Measure(ColumnsThenRows(AllColumns(), TakeRows(2))));
    }

    // --- Axis selectors -----------------------------------------------------------------------------

    [Fact]
    public void TheSelectorReExportsForwardToTheirStrategies()
    {
      Assert.Equal(RowStrategies.TakeRows(1).SelectRows(Block()), TakeRows(1).SelectRows(Block()));
      Assert.Equal(ColumnStrategies.TakeColumns(2).SelectColumns(Block()), TakeColumns(2).SelectColumns(Block()));
      Assert.Equal(RowStrategies.AllRows().SelectRows(Block()), AllRows().SelectRows(Block()));
      Assert.Equal(ColumnStrategies.AllColumns().SelectColumns(Block()), AllColumns().SelectColumns(Block()));
    }

    [Fact]
    public void AllRowsAndAllColumnsSeeTheWholeExtent()
    {
      Assert.Equal(2, AllRows().SelectRows(Block()));
      Assert.Equal(3, AllColumns().SelectColumns(Block()));
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
      // IAreaStrategy, handed a re-export, with no strategies import in scope at the call site.
      var projection = Sized(ColumnsWhileAnyValue()).Of(Range(b => $"{b.Width}x{b.Height}"));

      Assert.Equal("1x2", projection.Map(Patchy()));
      Assert.Equal("3x2", Sized(RowsWhileAnyValue()).Of(Range(b => $"{b.Width}x{b.Height}")).Map(Patchy()));
      Assert.Equal("2x1", Sized(Extent(2, 1)).Of(Range(b => $"{b.Width}x{b.Height}")).Map(Patchy()));
    }
  }
}
