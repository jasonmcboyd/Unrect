using Unrect.Core;

namespace Unrect.Tests
{
  /// <summary>
  /// The canonical half of the fixtures: grids that answer the four questions every space answers,
  /// and hand back their values — and nothing else. A declaration written over one of these cannot
  /// reach a kind, which is the point: it is where the canonical layer is exercised on its own.
  /// </summary>
  internal static class ValueTestSpaces
  {
    /// <summary>A grid of numbers in which zero means an empty cell.</summary>
    public static IValueCells<int> Numbers(int[,] values) => GridSpace.Create(values, isBlank: value => value == 0);

    /// <summary>A grid of labels; the array adapter treats null and "" as empty cells.</summary>
    public static IValueCells<string?> Labels(string?[,] values) => GridSpace.Create(values);

    /// <summary>
    /// A grid of heterogeneous values, each rendering as the kind its CLR type implies — the
    /// canonical twin of <see cref="ProjectionTestSpaces.Mixed"/>, which reads the same literals as
    /// a sheet.
    /// </summary>
    public static IValueCells<object?> Values(object?[,] values) => GridSpace.Create(values);
  }
}
