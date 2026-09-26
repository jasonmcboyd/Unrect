using System;
using System.Reflection;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// A rule names the space it reads, in its type: a rule written over the sheet's own space is an
  /// <c>ISizeStrategy&lt;ICellSpace&gt;</c>, one written over the canonical surface an
  /// <c>ISizeStrategy&lt;ISpace&gt;</c>, and a member that takes one is written over the declaration's
  /// space, so a rule for another space is a compile error where it is written and never a cast at a
  /// cell. What is pinned here is the seam that remains: the members that take a rule, the argument
  /// they blame for a null, and that a rule reads the same whichever space it was written at.
  /// </summary>
  public class TypedStrategyTests
  {
    /// <summary>Rows while any cell in them carries a value, written over the sheet's own space.</summary>
    private static ISizeStrategy<ICellSpace> ValueRows() => SizeStrategies.RowsWhileAnyIsNotBlank<ICellSpace>();

    /// <summary>Leading columns that carry values, written over the sheet's own space.</summary>
    private static ILineStrategy<ICellSpace> ValueColumns() => ColumnStrategies.TakeColumnsWhileAnyIsNotBlank<ICellSpace>();

    /// <summary>Leading rows that carry values, written over the sheet's own space.</summary>
    private static ILineStrategy<ICellSpace> ValueRowCount() => RowStrategies.TakeRowsWhileAnyIsNotBlank<ICellSpace>();

    /// <summary>Past the blank rows in front, written over the sheet's own space.</summary>
    private static IOffsetStrategy<ICellSpace> PastBlankRows() => OffsetStrategies.SkipBlankRows<ICellSpace>();

    /// <summary>Three rows of values, a blank row, then two more — so a measurement has somewhere to stop.</summary>
    private static ICellSpace Sheet()
      => Grid(new[,]
      {
        { 1, 2, 0 },
        { 3, 4, 0 },
        { 5, 6, 0 },
        { 0, 0, 0 },
        { 7, 8, 0 },
        { 9, 0, 0 },
      });

    // --- A rule reads the same at the canonical space and at the sheet's own --------------------------

    [Fact]
    public void SizedMeasuresTheSameAtEitherSpace()
    {
      // The same rule, once as the sheet's declaration writes it and once as a helper with no space to
      // name would, applied to the same grid through a plane of each space.
      var typed = Sized(ValueRows()).Of(Range(block => block.Height)).Apply(Sheet());
      var canonical = SizeStrategies.RowsWhileAnyIsNotBlank<ISpace>().GetSize(Plane<ISpace>.Of(Sheet()));

      Assert.Equal(canonical, typed.Consumed);
      Assert.Equal(new Size(3, 3), typed.Consumed);
    }

    [Fact]
    public void RangeMeasuresTheSameAtEitherSpace()
    {
      var typed = Range(ValueRows(), block => block.Height);
      var canonical = ProjectionBuilders<ISpace>.Range(SizeStrategies.RowsWhileAnyIsNotBlank<ISpace>(), block => block.Height);

      Assert.Equal(canonical.Map(Sheet()), typed.Map(Sheet()));
      Assert.Equal(3, typed.Map(Sheet()));
    }

    [Fact]
    public void RowMeasuresTheSameAtEitherSpace()
    {
      var typed = Row(ValueColumns(), strip => strip.Count);
      var canonical = ProjectionBuilders<ISpace>.Row(ColumnStrategies.TakeColumnsWhileAnyIsNotBlank<ISpace>(), strip => strip.Count);

      Assert.Equal(canonical.Map(Sheet()), typed.Map(Sheet()));
      Assert.Equal(2, typed.Map(Sheet()));
    }

    [Fact]
    public void ColumnMeasuresTheSameAtEitherSpace()
    {
      var typed = Column(ValueRowCount(), strip => strip.Count);
      var canonical = ProjectionBuilders<ISpace>.Column(RowStrategies.TakeRowsWhileAnyIsNotBlank<ISpace>(), strip => strip.Count);

      Assert.Equal(canonical.Map(Sheet()), typed.Map(Sheet()));
      Assert.Equal(3, typed.Map(Sheet()));
    }

    [Fact]
    public void OffsetByStartsTheSectionInTheSamePlaceAtEitherSpace()
    {
      var blankLed = Grid(new[,] { { 0, 0 }, { 0, 0 }, { 5, 6 } });

      var typed = OffsetBy(PastBlankRows()).Of(Point()).Apply(blankLed);
      var canonical = ProjectionBuilders<ISpace>.OffsetBy(OffsetStrategies.SkipBlankRows<ISpace>()).Of(ProjectionBuilders<ISpace>.Point()).Apply(blankLed);

      Assert.Equal(canonical.Offset, typed.Offset);
      Assert.Equal(2, typed.Offset.Row);
    }

    [Fact]
    public void ARepeatSeparatesItsOccurrencesTheSameAtEitherSpace()
    {
      var block = Range(ValueRows(), region => region.Height);
      var canonicalBlock = ProjectionBuilders<ISpace>.Range(SizeStrategies.RowsWhileAnyIsNotBlank<ISpace>(), region => region.Height);

      var typed = VerticalRepeat(block, PastBlankRows()).Map(Sheet());
      var canonical = ProjectionBuilders<ISpace>.VerticalRepeat(canonicalBlock, OffsetStrategies.SkipBlankRows<ISpace>()).Map(Sheet());

      Assert.Equal(canonical, typed);
      Assert.Equal(new[] { 3, 2 }, typed);
    }

    // --- The separator overloads all resolve, and to the same reading ------------------------------

    [Fact]
    public void ARepeatTakesASeparatorTypedOrNotAndNoSpellingOfItIsAmbiguous()
    {
      // A compiling test IS the pin here: the four spellings below are the four that have to keep
      // working, bare, named, positional and with a count.
      var block = Range(ValueRows(), region => region.Height);
      var blankRows = SkipRowsWhileAll(cell => cell.IsBlank());

      var bare = VerticalRepeat(block);
      var named0 = VerticalRepeat(block, separatedBy: BlankRows());
      var typed = VerticalRepeat(block, blankRows);
      var named = VerticalRepeat(block, separatedBy: blankRows, atLeast: 1);

      // Every occurrence steps over the blank rows in front of it, so the band between the two
      // blocks is crossed with or without a separator that says so.
      Assert.Equal(new[] { 3, 2 }, bare.Map(Sheet()));
      Assert.Equal(new[] { 3, 2 }, named0.Map(Sheet()));
      Assert.Equal(named0.Map(Sheet()), typed.Map(Sheet()));
      Assert.Equal(named0.Map(Sheet()), named.Map(Sheet()));
    }

    // --- The guards blame the parameter the caller wrote -------------------------------------------

    [Theory]
    [InlineData("extent")]
    [InlineData("offset")]
    [InlineData("columns")]
    [InlineData("rows")]
    public void ATypedRuleIsCheckedForNullAndTheFailureNamesTheArgument(string parameter)
    {
      Action call = parameter switch
      {
        "extent" => () => Sized((ISizeStrategy<ICellSpace>)null!).Of(Point()),
        "offset" => () => OffsetBy((IOffsetStrategy<ICellSpace>)null!).Of(Point()),
        "columns" => () => Row((ILineStrategy<ICellSpace>)null!, strip => strip.Count),
        _ => () => Column((ILineStrategy<ICellSpace>)null!, strip => strip.Count),
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
      // A null rule is refused where the declaration is written, never later at a cell, and the name
      // is the caller's own parameter, because that is the argument a reader has to go back and write.
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

    /// <summary>Every other public member that takes a rule, and the argument it blames for a null. A repeat's separator is optional, so a null there is no separator rather than a mistake.</summary>
    public static TheoryData<string> TheUnwrappingMembers => new TheoryData<string>
    {
      "Sized(extent)", "OffsetBy(offset)", "Range(extent)", "Row(columns)", "Column(rows)",
      "RowsThenColumns(rows)", "RowsThenColumns(columns)", "ColumnsThenRows(columns)", "ColumnsThenRows(rows)",
      "On(landmark)", "Below(landmark)", "RightOf(landmark)",
      "Until(landmark)",
      "stage Sized(extent)", "stage Range(extent)", "stage Row(columns)", "stage Column(rows)",
      "stage Until(landmark)",
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
        "Sized(extent)" => () => Sized((ISizeStrategy<ICellSpace>)null!),
        "OffsetBy(offset)" => () => OffsetBy((IOffsetStrategy<ICellSpace>)null!),
        "Range(extent)" => () => Range((ISizeStrategy<ICellSpace>)null!, block => block.Height),
        "Row(columns)" => () => Row((ILineStrategy<ICellSpace>)null!, strip => strip.Count),
        "Column(rows)" => () => Column((ILineStrategy<ICellSpace>)null!, strip => strip.Count),
        "RowsThenColumns(rows)" => () => RowsThenColumns((ILineStrategy<ICellSpace>)null!, ValueColumns()),
        "RowsThenColumns(columns)" => () => RowsThenColumns(ValueRowCount(), (ILineStrategy<ICellSpace>)null!),
        "ColumnsThenRows(columns)" => () => ColumnsThenRows((ILineStrategy<ICellSpace>)null!, ValueRowCount()),
        "ColumnsThenRows(rows)" => () => ColumnsThenRows(ValueColumns(), (ILineStrategy<ICellSpace>)null!),
        "On(landmark)" => () => On((ILineLandmark<ICellSpace>)null!),
        "Below(landmark)" => () => Below((ILineLandmark<ICellSpace>)null!),
        "RightOf(landmark)" => () => RightOf((ILineLandmark<ICellSpace>)null!),
        "Until(landmark)" => () => Until((ILineLandmark<ICellSpace>)null!),
        "stage Sized(extent)" => () => Down(1).Sized((ISizeStrategy<ICellSpace>)null!),
        "stage Range(extent)" => () => Down(1).Range((ISizeStrategy<ICellSpace>)null!, block => block.Height),
        "stage Row(columns)" => () => Down(1).Row((ILineStrategy<ICellSpace>)null!, strip => strip.Count),
        "stage Column(rows)" => () => Down(1).Column((ILineStrategy<ICellSpace>)null!, strip => strip.Count),
        _ => () => Down(1).Until((ILineLandmark<ICellSpace>)null!),
      };

      var expected = member.Substring(member.IndexOf('(') + 1).TrimEnd(')');

      Assert.Equal(expected, Assert.Throws<ArgumentNullException>(call).ParamName);
    }

    // --- The refusals cover the typed members ----------------------------------------------------------

    [Fact]
    public void AHeadingRefusesABoundInTheLibrarysOwnWords()
    {
      // A heading is self-anchoring and already says where its section is, so a bound after it is
      // refused — in the library's words, not the compiler's.
      var refused = typeof(HeadingStage<ICellSpace>).GetMethod("Until", new[] { typeof(ILineLandmark<ICellSpace>), typeof(bool) });

      Assert.NotNull(refused);

      var refusal = Assert.IsType<ObsoleteAttribute>(refused!.GetCustomAttribute<ObsoleteAttribute>());

      Assert.Equal(PipelineRefusals.GeometryComesBeforeTheHeadings, refusal.Message);
      Assert.True(refusal.IsError);
    }
  }
}
