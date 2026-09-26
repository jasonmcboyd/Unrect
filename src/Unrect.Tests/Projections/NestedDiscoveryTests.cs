using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
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
  /// </summary>
  public class NestedDiscoveryTests
  {
    /// <summary>
    /// Three rows of numbers, then three of text, then two blank — a sheet on which "rows while any
    /// cell is a number" and "rows while any cell has a value" give different answers (3 and 6), so a
    /// child that resumed on the sheet instead of on its parent's region says 6 where it should say 3.
    /// </summary>
    private static ICellSpace Disagreeing()
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

    private static T Read<T>(IProjectionDefinition<ICellSpace, T> declaration) => declaration.Map(Disagreeing());

    /// <summary>The outer rule: rows while any cell of them is a number, which stops after row 2.</summary>
    private static IAreaStrategy<ICellSpace> NumericRowsOnly()
      => RowsWhileAny(cell => cell.IsDouble());

    /// <summary>
    /// The same rule as a row-and-column pair, which resolves to the interleaved strategy — the one
    /// whose scan carries replay state, and therefore the one that reads the space it was begun with
    /// rather than the space it is handed per row.
    /// </summary>
    private static IAreaStrategy<ICellSpace> NumericRowsAndValuedColumns()
      => RowsThenColumns(
        TakeRowsWhileAny(cell => cell.IsDouble()),
        TakeColumnsWhileAny(cell => !cell.IsBlank()));

    [Fact]
    public void AChildTakingTheWholeExtentTakesItsParentsAndNotTheSheets()
    {
      // The plainest statement of the rule: the child asks for everything there is, and everything
      // there is, is what the parent settled on. Six would mean the child had been handed the sheet.
      //
      // It is the statement rather than the guard: "the whole extent" is whatever region the child
      // is handed. The two tests after this one are the ones that go red when a child is handed the
      // sheet; this is the sentence a reader needs before those two make sense.
      var rows = Sized(NumericRowsOnly()).Of(
        VerticalFlow(v =>
        {
          var rangeSlot = v.Next(Range(WholeExtent(), block => block.Rows.Count));

          return rangeSlot;
        }));

      Assert.Equal(NumericRows, Read(rows));
    }

    [Fact]
    public void AChildDiscoveringItsOwnExtentStopsWhereItsParentDid()
    {
      // The sharp case. The child's rule would run to row 5 on its own — every one of those rows
      // carries a value — so the only thing that can stop it at row 2 is the region it was given. A
      // child resolved against the raw sheet says six here and nothing else in the reading looks
      // wrong.
      var rows = Sized(NumericRowsOnly()).Of(
        VerticalFlow(v =>
        {
          var rangeSlot = v.Next(Range(RowsWhileAnyValue(), block => block.Rows.Count));

          return rangeSlot;
        }));

      Assert.Equal(NumericRows, Read(rows));
    }

    [Fact]
    public void AndSoDoesOneUnderAnInterleavedRowAndColumnRule()
    {
      // An interleaved scan decides its width from the rows it walks itself and answers later calls
      // from what it recorded, so it is the one strategy for which being handed the parent's region
      // rather than the sheet is not merely tidy: the mismatch would be silent.
      var rows = Sized(NumericRowsAndValuedColumns()).Of(
        VerticalFlow(v =>
        {
          var rangeSlot = v.Next(Range(RowsWhileAnyValue(), block => block.Rows.Count));

          return rangeSlot;
        }));

      Assert.Equal(NumericRows, Read(rows));
    }

    [Fact]
    public void AndTheParentItselfStillStopsWhereItsOwnRuleSays()
    {
      // The control, without which every number above could be right for the wrong reason: the outer
      // rule really does stop at three, and the sheet really does have six valued rows for a child
      // to run on to.
      var outer = Sized(NumericRowsOnly()).Of(Range(WholeExtent(), block => block.Rows.Count));

      Assert.Equal(NumericRows, Read(outer));
      Assert.Equal(ValuedRows, Read(Range(RowsWhileAnyValue(), block => block.Rows.Count)));
      Assert.Equal(8, Disagreeing().Area.Height);
    }
  }
}
