using Unrect.Core;
using Unrect.Projections;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// A discovery inside a discovery, where the two rules disagree about where the sheet ends.
  /// <para>
  /// This is the one thing a region still being discovered can get wrong without any answer looking
  /// odd. A child's extent is resolved against the space its parent handed down, and if that space
  /// is the raw sheet rather than the region the parent settled on, the child simply carries on past
  /// the row the parent excluded — and reports a perfectly plausible number of rows that the parent
  /// had already decided were not part of the section.
  /// </para>
  /// <para>
  /// Both forcing modes are asserted for every case, and that is the whole shape of the claim:
  /// laziness may change what a reading COSTS and may never change what it says. The eager reading
  /// is the definition, so a disagreement between the two columns is always the lazy path being
  /// wrong.
  /// </para>
  /// </summary>
  public class NestedDiscoveryTests
  {
    /// <summary>
    /// Three rows of numbers, then three of text, then two blank — a sheet on which "rows while any
    /// cell is a number" and "rows while any cell has a value" give different answers (3 and 6), so a
    /// child that resumed on the sheet instead of on its parent's region says 6 where it should say 3.
    /// </summary>
    private static ICellValues Disagreeing()
    {
      var values = new object?[8, 2];

      for (var row = 0; row < 3; row++)
      {
        values[row, 0] = row + 1;
        values[row, 1] = (row + 1) * 10;
      }

      for (var row = 3; row < 6; row++)
      {
        values[row, 0] = $"row {row}";
        values[row, 1] = "text";
      }

      return Mixed(values);
    }

    private const int NumericRows = 3;
    private const int ValuedRows = 6;

    /// <summary>The two ways the engine may resolve an extent, so every law here is stated over both.</summary>
    public static TheoryData<bool> ForcingModes => new TheoryData<bool> { false, true };

    /// <summary>
    /// <paramref name="declaration"/> applied to the disagreeing sheet, eagerly or not. The eager
    /// switch is process-wide and scoped by the <c>using</c>, exactly as <see cref="LazyForcingTests"/>
    /// uses it.
    /// </summary>
    private static T Read<T>(IProjection<T> declaration, bool eager)
    {
      if (!eager)
        return declaration.Map(Disagreeing());

      using (ProjectionEngine.ForceEager())
        return declaration.Map(Disagreeing());
    }

    /// <summary>
    /// The outer rule: rows while any cell of them is a number, which stops after row 2.
    /// <para>
    /// Spelled over the canonical four as "has a value and is not text", which is exactly "is a
    /// number" for this fixture — it holds numbers, strings and blanks and nothing else. The kind
    /// predicate itself returns with the typed layer.
    /// </para>
    /// </summary>
    private static IAreaStrategy NumericRowsOnly() => RowsWhileAny(value => !value.IsBlank && !value.IsText);

    /// <summary>
    /// The same rule as a row-and-column pair, which resolves to the interleaved strategy — the one
    /// whose scan carries replay state, and therefore the one that reads the space it was begun with
    /// rather than the space it is handed per row.
    /// </summary>
    private static IAreaStrategy NumericRowsAndValuedColumns()
      => AreaStrategies.RowsThenColumns(
        RowStrategies.TakeRowsWhileAny(value => !value.IsBlank && !value.IsText),
        ColumnStrategies.TakeColumnsWhileAnyValue());

    [Theory]
    [MemberData(nameof(ForcingModes))]
    public void AChildTakingTheWholeExtentTakesItsParentsAndNotTheSheets(bool eager)
    {
      // The plainest statement of the rule: the child asks for everything there is, and everything
      // there is, is what the parent settled on. Six would mean the child had been handed the sheet.
      //
      // It is the statement rather than the guard. "The whole extent" is not a per-row rule, so it
      // is measured up front against whatever space it is given, and it was already right before the
      // engine started sharing one — the two tests after this one are the ones that go red when it
      // stops. Kept because it is the sentence a reader needs before those two make sense.
      var rows = Sized(NumericRowsOnly()).Of(
        VerticalFlow(v => v.Next(Range(WholeExtent(), block => block.Rows.Count))));

      Assert.Equal(NumericRows, Read(rows, eager));
    }

    [Theory]
    [MemberData(nameof(ForcingModes))]
    public void AChildDiscoveringItsOwnExtentStopsWhereItsParentDid(bool eager)
    {
      // The sharp case, and the one the shared space exists for. The child's rule would run to row 5
      // on its own — every one of those rows carries a value — so the only thing that can stop it at
      // row 2 is the region it was given. A child resolved against the raw sheet says six here and
      // nothing else in the reading looks wrong.
      var rows = Sized(NumericRowsOnly()).Of(
        VerticalFlow(v => v.Next(Range(RowsWhileAnyValue(), block => block.Rows.Count))));

      Assert.Equal(NumericRows, Read(rows, eager));
    }

    [Theory]
    [MemberData(nameof(ForcingModes))]
    public void AndSoDoesOneUnderAnInterleavedRowAndColumnRule(bool eager)
    {
      // The replay-state half. An interleaved scan decides its width from rows it walks itself and
      // answers later calls from what it recorded, so it is the one strategy for which "the same
      // space" is not merely tidy: begun over one object and folded over another, it replays state
      // taken from a sheet against a region, and the mismatch is silent.
      var rows = Sized(NumericRowsAndValuedColumns()).Of(
        VerticalFlow(v => v.Next(Range(RowsWhileAnyValue(), block => block.Rows.Count))));

      Assert.Equal(NumericRows, Read(rows, eager));
    }

    [Theory]
    [MemberData(nameof(ForcingModes))]
    public void AndTheParentItselfStillStopsWhereItsOwnRuleSays(bool eager)
    {
      // The control, without which every number above could be right for the wrong reason: the outer
      // rule really does stop at three, and the sheet really does have six valued rows for a child
      // to run on to.
      var outer = Sized(NumericRowsOnly()).Of(Range(WholeExtent(), block => block.Rows.Count));

      Assert.Equal(NumericRows, Read(outer, eager));
      Assert.Equal(ValuedRows, Read(Range(RowsWhileAnyValue(), block => block.Rows.Count), eager));
      Assert.Equal(8, Disagreeing().Area.Height);
    }
  }
}
