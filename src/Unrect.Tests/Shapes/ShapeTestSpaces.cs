using System;

using Unrect.Array;
using Unrect.Core;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// Grid builders for the shape tests. Two flavours: an int grid where zero means blank (the same
  /// convention the substrate tests use), and a mixed grid for tables, where a header of strings has
  /// to sit above a body of numbers and dates.
  /// </summary>
  internal static class ShapeTestSpaces
  {
    /// <summary>A grid of numbers in which zero means an empty cell.</summary>
    public static ISpace Grid(int[,] values) => ArraySpace.Create(values, isBlank: v => v == 0);

    /// <summary>
    /// A grid of heterogeneous values: null and "" are blank, everything else adapts to the cell
    /// kind its CLR type implies. This is the array-adapter equivalent of a real sheet.
    /// </summary>
    public static ISpace Mixed(object?[,] values) => ArraySpace.Create(values, Adapt);

    private static CellValue Adapt(object? value) =>
      value switch
      {
        null => CellValue.Blank,
        // An already-canonical value passes straight through, so a grid can carry an error
        // cell — the one kind with no CLR literal to write it as.
        CellValue cell => cell,
        string text => text.Length == 0 ? CellValue.Blank : CellValue.Of(text),
        int number => CellValue.Of(number),
        long number => CellValue.Of(number),
        double number => CellValue.Of(number),
        decimal number => CellValue.Of(number),
        DateTime moment => CellValue.Of(moment),
        bool flag => CellValue.Of(flag),
        _ => throw new ArgumentException($"No canonical cell kind for {value.GetType()}.", nameof(value))
      };
  }
}
