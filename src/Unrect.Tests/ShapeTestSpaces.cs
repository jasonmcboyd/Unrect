using System;

using Unrect.Core;
using Unrect.Shapes;

namespace Unrect.Tests
{
  /// <summary>
  /// The grids and small helpers the test suites share. It sits at the project root rather than
  /// beside the shape tests because the strategy tests need the same grids, and one convention for
  /// what "blank" means is worth more than a folder boundary.
  /// </summary>
  internal static class ShapeTestSpaces
  {
    /// <summary>A grid of numbers in which zero means an empty cell.</summary>
    public static ISpace Grid(int[,] values) => GridSpace.Create(values, isBlank: v => v == 0);

    /// <summary>A grid of labels; the array adapter treats null and "" as empty cells.</summary>
    public static ISpace Text(string?[,] values) => GridSpace.Create(values);

    /// <summary>
    /// A column of 1..<paramref name="height"/>, so an assertion reads as the row it came from.
    /// </summary>
    public static ISpace Ladder(int height = 3)
    {
      var values = new int[height, 1];

      for (var row = 0; row < height; row++)
        values[row, 0] = row + 1;

      return Grid(values);
    }

    /// <summary>
    /// A grid whose every cell is (row * 10 + column + 1), so an assertion reads as a coordinate:
    /// 1 2 3 4 / 11 12 13 14 / 21 22 23 24. The +1 keeps cell (0, 0) non-blank.
    /// </summary>
    public static ISpace CoordinateGrid(int width = 4, int height = 3)
    {
      var values = new int[height, width];

      for (var row = 0; row < height; row++)
        for (var column = 0; column < width; column++)
          values[row, column] = row * 10 + column + 1;

      return Grid(values);
    }

    /// <summary>A cell read as a number — the leaf most tests need and none of them vary.</summary>
    public static IShape<int> IntCell() => Shape.Cell(v => v.GetInt());

    /// <summary>
    /// The problem text of a failure, without the subject the message template puts in front of it.
    /// </summary>
    public static string Problem(ShapeException failure)
    {
      var line = failure.Message.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[0];

      return line.Substring(line.IndexOf(": ", StringComparison.Ordinal) + 2);
    }

    /// <summary>How many times <paramref name="value"/> occurs in <paramref name="text"/>.</summary>
    public static int Occurrences(string text, string value)
    {
      var count = 0;

      for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + 1, StringComparison.Ordinal))
        count++;

      return count;
    }

    /// <summary>
    /// A grid of heterogeneous values: null and "" are blank, everything else adapts to the cell
    /// kind its CLR type implies. This is the array-adapter equivalent of a real sheet.
    /// </summary>
    public static ISpace Mixed(object?[,] values) => GridSpace.Create(values, Adapt);

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
