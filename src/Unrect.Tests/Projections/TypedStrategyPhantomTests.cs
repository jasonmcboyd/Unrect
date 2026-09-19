using System;
using System.Reflection;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The typed strategy phantoms: a rule that names the space it must be able to look at, carried
  /// through a member that takes one and unwrapped at the lift.
  /// <para>
  /// The rules here are written by hand over the canonical factory they box, which is the sharpest
  /// form of the claim: what the calculus receives through the typed door is the object it receives
  /// through the canonical one, so the two doors measure identically. The factories that build one
  /// are pinned in <see cref="ProjectionReExportTests"/>; here the subject is the seam itself — the
  /// members that take a phantom, the refusals that turn one away, and what happens when a rule
  /// reaches a space it was not written for.
  /// </para>
  /// </summary>
  public class TypedStrategyPhantomTests
  {
    /// <summary>Rows while any cell in them carries a value, demanding a sheet.</summary>
    private sealed class ValueRows : IAreaStrategy<ISheetCells>
    {
      public Unrect.Core.IAreaStrategy Strategy { get; } = SizeStrategies.RowsWhileAnyValue().ToAreaStrategy();
    }

    /// <summary>Leading columns that carry values, demanding a sheet.</summary>
    private sealed class ValueColumns : IColumnStrategy<ISheetCells>
    {
      public Unrect.Core.IColumnStrategy Strategy { get; } = ColumnStrategies.TakeColumnsWhileAnyValue();
    }

    /// <summary>Leading rows that carry values, demanding a sheet.</summary>
    private sealed class ValueRowCount : IRowStrategy<ISheetCells>
    {
      public Unrect.Core.IRowStrategy Strategy { get; } = RowStrategies.TakeRowsWhileAnyValue();
    }

    /// <summary>Past the blank rows in front, demanding a sheet.</summary>
    private sealed class PastBlankRows : IOffsetStrategy<ISheetCells>
    {
      public Unrect.Core.IOffsetStrategy Strategy { get; } = OffsetStrategies.SkipBlankRows();
    }

    /// <summary>Three rows of values, a blank row, then two more — so a measurement has somewhere to stop.</summary>
    private static ISheetCells Sheet()
      => Grid(new[,]
      {
        { 1, 2, 0 },
        { 3, 4, 0 },
        { 5, 6, 0 },
        { 0, 0, 0 },
        { 7, 8, 0 },
        { 9, 0, 0 },
      });

    // --- The two doors measure the same thing -----------------------------------------------------

    [Fact]
    public void SizedMeasuresTheSameThroughEitherDoor()
    {
      var typed = Sized(new ValueRows()).Of(Range(block => block.Height)).Apply(Sheet());
      var canonical = Sized(new ValueRows().Strategy).Of(Range(block => block.Height)).Apply(Sheet());

      Assert.Equal(canonical.Consumed, typed.Consumed);
      Assert.Equal(new Size(3, 3), typed.Consumed);
    }

    [Fact]
    public void RangeMeasuresTheSameThroughEitherDoor()
    {
      var typed = Range(new ValueRows(), block => block.Height);
      var canonical = Range(new ValueRows().Strategy, block => block.Height);

      Assert.Equal(canonical.Map(Sheet()), typed.Map(Sheet()));
      Assert.Equal(3, typed.Map(Sheet()));
    }

    [Fact]
    public void RowMeasuresTheSameThroughEitherDoor()
    {
      var typed = Row(new ValueColumns(), strip => strip.Count);
      var canonical = Row(new ValueColumns().Strategy, strip => strip.Count);

      Assert.Equal(canonical.Map(Sheet()), typed.Map(Sheet()));
      Assert.Equal(2, typed.Map(Sheet()));
    }

    [Fact]
    public void ColumnMeasuresTheSameThroughEitherDoor()
    {
      var typed = Column(new ValueRowCount(), strip => strip.Count);
      var canonical = Column(new ValueRowCount().Strategy, strip => strip.Count);

      Assert.Equal(canonical.Map(Sheet()), typed.Map(Sheet()));
      Assert.Equal(3, typed.Map(Sheet()));
    }

    [Fact]
    public void OffsetByStartsTheSectionInTheSamePlaceThroughEitherDoor()
    {
      var blankLed = Grid(new[,] { { 0, 0 }, { 0, 0 }, { 5, 6 } });

      var typed = OffsetBy(new PastBlankRows()).Of(Point()).Apply(blankLed);
      var canonical = OffsetBy(new PastBlankRows().Strategy).Of(Point()).Apply(blankLed);

      Assert.Equal(canonical.Offset, typed.Offset);
      Assert.Equal(2, typed.Offset.Height);
    }

    [Fact]
    public void ARepeatSeparatesItsOccurrencesTheSameThroughEitherDoor()
    {
      var block = Range(new ValueRows(), region => region.Height);

      var typed = VerticalRepeat(block, new PastBlankRows()).Map(Sheet());
      var canonical = VerticalRepeat(block, new PastBlankRows().Strategy).Map(Sheet());

      Assert.Equal(canonical, typed);
      Assert.Equal(new[] { 3, 2 }, typed);
    }

    // --- The separator overloads all resolve, and to the same reading ------------------------------

    [Fact]
    public void ARepeatTakesASeparatorTypedOrNotAndNoSpellingOfItIsAmbiguous()
    {
      // A compiling test IS the pin here. Doubling an overload that differs only in an OPTIONAL
      // parameter's type makes the no-separator spelling ambiguous, which is why the typed twin
      // demands its separator — so the four spellings below are exactly the four that have to keep
      // working, and three of them would stop compiling if the twin were declared the obvious way.
      var block = Range(new ValueRows(), region => region.Height);
      var blankRows = SkipRowsWhileAll(cell => cell.IsBlank);

      var bare = VerticalRepeat(block);
      var erased = VerticalRepeat(block, separatedBy: BlankRows());
      var typed = VerticalRepeat(block, blankRows);
      var named = VerticalRepeat(block, separatedBy: blankRows, atLeast: 1);

      // Every occurrence steps over the blank rows in front of it, so the band between the two
      // blocks is crossed with or without a separator that says so.
      Assert.Equal(new[] { 3, 2 }, bare.Map(Sheet()));
      Assert.Equal(new[] { 3, 2 }, erased.Map(Sheet()));
      Assert.Equal(erased.Map(Sheet()), typed.Map(Sheet()));
      Assert.Equal(erased.Map(Sheet()), named.Map(Sheet()));
    }

    // --- The guards blame the parameter the caller wrote -------------------------------------------

    [Theory]
    [InlineData("area")]
    [InlineData("offset")]
    [InlineData("columns")]
    [InlineData("rows")]
    [InlineData("separatedBy")]
    public void ATypedRuleIsCheckedForNullAndTheFailureNamesTheArgument(string parameter)
    {
      Action call = parameter switch
      {
        "area" => () => Sized((IAreaStrategy<ISheetCells>)null!).Of(Point()),
        "offset" => () => OffsetBy((IOffsetStrategy<ISheetCells>)null!).Of(Point()),
        "columns" => () => Row((IColumnStrategy<ISheetCells>)null!, strip => strip.Count),
        "rows" => () => Column((IRowStrategy<ISheetCells>)null!, strip => strip.Count),
        _ => () => VerticalRepeat(Point(), (IOffsetStrategy<ISheetCells>)null!),
      };

      Assert.Equal(parameter, Assert.Throws<ArgumentNullException>(call).ParamName);
    }

    [Theory]
    [InlineData("RowWhere", "predicate")]
    [InlineData("RowWithCell", "anyCell")]
    [InlineData("ColumnWhere", "predicate")]
    [InlineData("ColumnWithCell", "anyCell")]
    [InlineData("RowsWhileAny", "anyCell")]
    [InlineData("TakeRowsWhileAny", "predicate")]
    [InlineData("SkipRowsWhileAny", "predicate")]
    [InlineData("SelectSize", "selector")]
    public void ATypedFactoryRefusesANullPredicateWhereItIsWritten(string factory, string parameter)
    {
      // A typed factory lowers its predicate before handing it on, and a lowered null is a live
      // delegate: the erased factory's own guard sees a well-formed lambda and lets it through, so
      // without a check here the declaration is built and fails later, at a cell, saying nothing
      // about the rule that was never supplied. The name is the caller's own parameter, because
      // that is the argument a reader has to go back and write.
      Action call = factory switch
      {
        "RowWhere" => () => RowWhere(null!),
        "RowWithCell" => () => RowWithCell(null!),
        "ColumnWhere" => () => ColumnWhere(null!),
        "ColumnWithCell" => () => ColumnWithCell(null!),
        "RowsWhileAny" => () => RowsWhileAny(null!),
        "TakeRowsWhileAny" => () => TakeRowsWhileAny(null!),
        "SkipRowsWhileAny" => () => SkipRowsWhileAny(null!),
        _ => () => SelectSize(null!),
      };

      Assert.Equal(parameter, Assert.Throws<ArgumentNullException>(call).ParamName);
    }

    /// <summary>Every other public overload that unwraps a phantom, and the argument it blames.</summary>
    public static TheoryData<string> TheUnwrappingMembers => new TheoryData<string>
    {
      "Sized(area)", "OffsetBy(offset)", "Range(area)", "Row(columns)", "Column(rows)",
      "RowsThenColumns(rows)", "RowsThenColumns(columns)", "ColumnsThenRows(columns)", "ColumnsThenRows(rows)",
      "VerticalRepeat(separatedBy)", "HorizontalRepeat(separatedBy)",
      "On(row)", "On(column)", "Below(landmark)", "RightOf(landmark)",
      "Until(landmark)", "UntilColumn(landmark)",
      "stage Sized(area)", "stage Range(area)", "stage Row(columns)", "stage Column(rows)",
      "stage VerticalRepeat(separatedBy)", "stage HorizontalRepeat(separatedBy)",
      "stage Until(landmark)", "stage UntilColumn(landmark)",
    };

    [Theory]
    [MemberData(nameof(TheUnwrappingMembers))]
    public void AndEveryMemberThatUnwrapsOneBlamesItsOwnParameter(string member)
    {
      // Unwrapping is the one thing a typed overload does that its canonical twin does not, so it
      // is the one place a null can be dereferenced instead of reported. The name matters as much
      // as the throw: a declaration that named the wrong argument would send a reader to the wrong
      // line of a pipeline whose members all take one rule each.
      Action call = member switch
      {
        "Sized(area)" => () => Sized((IAreaStrategy<ISheetCells>)null!),
        "OffsetBy(offset)" => () => OffsetBy((IOffsetStrategy<ISheetCells>)null!),
        "Range(area)" => () => Range((IAreaStrategy<ISheetCells>)null!, block => block.Height),
        "Row(columns)" => () => Row((IColumnStrategy<ISheetCells>)null!, strip => strip.Count),
        "Column(rows)" => () => Column((IRowStrategy<ISheetCells>)null!, strip => strip.Count),
        "RowsThenColumns(rows)" => () => RowsThenColumns((IRowStrategy<ISheetCells>)null!, new ValueColumns()),
        "RowsThenColumns(columns)" => () => RowsThenColumns(new ValueRowCount(), (IColumnStrategy<ISheetCells>)null!),
        "ColumnsThenRows(columns)" => () => ColumnsThenRows((IColumnStrategy<ISheetCells>)null!, new ValueRowCount()),
        "ColumnsThenRows(rows)" => () => ColumnsThenRows(new ValueColumns(), (IRowStrategy<ISheetCells>)null!),
        "VerticalRepeat(separatedBy)" => () => VerticalRepeat(Point(), (IOffsetStrategy<ISheetCells>)null!),
        "HorizontalRepeat(separatedBy)" => () => HorizontalRepeat(Point(), (IOffsetStrategy<ISheetCells>)null!),
        "On(row)" => () => On((IRowLandmark<ISheetCells>)null!),
        "On(column)" => () => On((IColumnLandmark<ISheetCells>)null!),
        "Below(landmark)" => () => Below((IRowLandmark<ISheetCells>)null!),
        "RightOf(landmark)" => () => RightOf((IColumnLandmark<ISheetCells>)null!),
        "Until(landmark)" => () => Until((IRowLandmark<ISheetCells>)null!),
        "UntilColumn(landmark)" => () => UntilColumn((IColumnLandmark<ISheetCells>)null!),
        "stage Sized(area)" => () => Down(1).Sized((IAreaStrategy<ISheetCells>)null!),
        "stage Range(area)" => () => Down(1).Range((IAreaStrategy<ISheetCells>)null!, block => block.Height),
        "stage Row(columns)" => () => Down(1).Row((IColumnStrategy<ISheetCells>)null!, strip => strip.Count),
        "stage Column(rows)" => () => Down(1).Column((IRowStrategy<ISheetCells>)null!, strip => strip.Count),
        "stage VerticalRepeat(separatedBy)" => () => Down(1).VerticalRepeat(Point(), (IOffsetStrategy<ISheetCells>)null!),
        "stage HorizontalRepeat(separatedBy)" => () => Down(1).HorizontalRepeat(Point(), (IOffsetStrategy<ISheetCells>)null!),
        "stage Until(landmark)" => () => Down(1).Until((IRowLandmark<ISheetCells>)null!),
        _ => () => Down(1).UntilColumn((IColumnLandmark<ISheetCells>)null!),
      };

      var expected = member.Substring(member.IndexOf('(') + 1).TrimEnd(')');

      Assert.Equal(
        expected switch { "row" or "column" => "landmark", _ => expected },
        Assert.Throws<ArgumentNullException>(call).ParamName);
    }

    // --- The refusals are doubled with the declarations --------------------------------------------

    [Theory]
    [InlineData("Until")]
    [InlineData("UntilColumn")]
    public void AHeadingRefusesATypedBoundInTheSameWordsAsACanonicalOne(string member)
    {
      // The hole this closes: a heading is self-anchoring and already says where its section is, so
      // a bound after it is refused — but only the canonical spelling was refused, and the typed one
      // fell through to the compiler's own words about an IRowLandmark<TSpace> not being an
      // IRowLandmark. Same refusal, both spellings.
      var canonical = Refusal(member, Landmark(member, typed: false));
      var typed = Refusal(member, Landmark(member, typed: true));

      Assert.Equal(PipelineRefusals.GeometryComesBeforeTheHeadings, canonical.Message);
      Assert.Equal(canonical.Message, typed.Message);
      Assert.True(typed.IsError);
    }

    private static Type Landmark(string member, bool typed)
      => (member, typed) switch
      {
        ("Until", false) => typeof(IRowLandmark),
        ("Until", true) => typeof(IRowLandmark<ISheetCells>),
        (_, false) => typeof(IColumnLandmark),
        _ => typeof(IColumnLandmark<ISheetCells>),
      };

    private static ObsoleteAttribute Refusal(string member, Type landmark)
    {
      var refused = typeof(HeadingStage<ISheetCells>).GetMethod(member, new[] { landmark, typeof(bool) });

      Assert.NotNull(refused);

      return Assert.IsType<ObsoleteAttribute>(refused!.GetCustomAttribute<ObsoleteAttribute>());
    }

    // --- A rule that reaches a space it was not written for ------------------------------------------

    [Fact]
    public void ARuleUnwrappedIntoTheWrongSpaceFaultsAndNoToleranceBoundaryAbsorbsIt()
    {
      // The demand is discharged by the phantom's own type, so the only way to lose it is to unwrap
      // the rule and hand the bare strategy through the canonical door. The predicate then meets a
      // sheet where it was promised a spreadsheet, and the cast it does per cell fails.
      //
      // It is a fault and not a statement about the data: nothing was read and nothing was absent —
      // the declaration was wired to a space that cannot answer it. Absorbing that as "no section
      // here" is exactly the lie the typed layer exists to prevent, so Optional and Else must let it
      // out.
      var smuggled = ProjectionBuilders<ISpreadsheetSpace>.RowsWhileAny(cell => cell.HasValue).Strategy;
      var declaration = Sized(smuggled).Of(Range(block => block.Height));

      var failure = Assert.Throws<ProjectionException>(() => declaration.Map(Sheet()));

      Assert.True(failure.IsFault);
      Assert.IsType<InvalidCastException>(failure.InnerException);

      var underOptional = Assert.Throws<ProjectionException>(() => declaration.Optional().Map(Sheet()));
      var underElse = Assert.Throws<ProjectionException>(() => declaration.Else(Range(_ => -1)).Map(Sheet()));
      var underChoice = Assert.Throws<ProjectionException>(() => Choice(declaration, Range(_ => -1)).Map(Sheet()));

      Assert.True(underOptional.IsFault);
      Assert.True(underElse.IsFault);
      Assert.True(underChoice.IsFault);
    }

    /// <summary>A rule and a matcher promised a spreadsheet, unwrapped so a sheet can be handed one.</summary>
    private static IProjectionDefinition<ISheetCells, int> Smuggled(string door) => door switch
    {
      "Sized" => Sized(ProjectionBuilders<ISpreadsheetSpace>.RowsWhileAny(cell => cell.HasValue).Strategy)
        .Of(Range(block => block.Height)),

      "On" => On(ProjectionBuilders<ISpreadsheetSpace>.RowWithCell(cell => cell.HasValue).Landmark)
        .Of(Range(block => block.Height)),

      "OffsetBy" => OffsetBy(ProjectionBuilders<ISpreadsheetSpace>.SkipRowsWhileAny(cell => cell.HasValue).Strategy)
        .Of(Range(block => block.Height)),

      "Row" => Row(ProjectionBuilders<ISpreadsheetSpace>.TakeColumnsWhileAny(cell => cell.HasValue).Strategy, strip => strip.Count),

      // A separator is never applied before the first occurrence, so this one faults on the second
      // — which is the interesting half: the declaration had already read something.
      _ => VerticalRepeat(
        Range(Extent(3, 1), block => block.Height),
        ProjectionBuilders<ISpreadsheetSpace>.SkipRowsWhileAny(cell => cell.HasValue).Strategy)
        .Select(occurrences => occurrences.Count),
    };

    [Theory]
    [InlineData("Sized")]
    [InlineData("On")]
    [InlineData("OffsetBy")]
    [InlineData("Row")]
    [InlineData("separatedBy")]
    public void EveryDoorThatTakesARuleFaultsWhenTheRuleMeetsTheWrongSpace(string door)
    {
      // Every member that takes a rule or a matcher can be handed one that was written for another
      // space, and each has to answer the same way. The doors differ in WHEN the cast runs — a
      // matcher while the anchor is searched, an offset while the section is placed, an extent while
      // it is measured, a separator between two occurrences — and a door that classified its own
      // moment as absence would turn a mis-wired declaration into a missing section.
      var declaration = Smuggled(door);

      var failure = Assert.Throws<ProjectionException>(() => declaration.Map(Sheet()));

      Assert.True(failure.IsFault, $"the {door} door did not fault");
      Assert.IsType<InvalidCastException>(failure.InnerException);

      Assert.True(Assert.Throws<ProjectionException>(() => declaration.Optional().Map(Sheet())).IsFault);
      Assert.True(Assert.Throws<ProjectionException>(() => declaration.Else(Range(_ => -1)).Map(Sheet())).IsFault);
    }

    // --- Retyping a region: the way back from the canonical surface ---------------------------------

    [Fact]
    public void RetypingARegionKeepsItsOriginAndItsExtent()
    {
      var sheet = CoordinateGrid(4, 10);
      var region = Plane<ISheetCells>.Of(sheet).Slice(new Offset(1, 2)).Narrowed(3);

      var retyped = region.Erased().Retyped<ISheetCells>();

      Assert.Same(region.Space, retyped.Space);
      Assert.Equal(region.Origin, retyped.Origin);
      Assert.Equal(region.Width, retyped.Width);
      Assert.Equal(new Area(3, 8), retyped.Area);

      for (var row = 0; row < 10; row++)
        Assert.Equal(region.HasRow(row), retyped.HasRow(row));
    }

    [Fact]
    public void RetypingARegionToASpaceItIsNotFails()
    {
      var region = Plane<ISheetCells>.Of(CoordinateGrid(2, 2)).Erased();

      Assert.Throws<InvalidCastException>(() => region.Retyped<ISpreadsheetSpace>());
    }
  }
}
