using System;
using System.IO;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Tests.Streaming;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// The grids and small helpers the test suites share. It sits at the project root rather than
  /// beside the projection tests because the strategy tests need the same grids, and one convention
  /// for what "blank" means is worth more than a folder boundary.
  /// </summary>
  internal static class ProjectionTestSpaces
  {
    /// <summary>A grid of numbers in which zero means an empty cell.</summary>
    public static ICellValues Grid(int[,] values) => GridSpace.Create(values, isBlank: v => v == 0);

    /// <summary>A grid of labels; the array adapter treats null and "" as empty cells.</summary>
    public static ICellValues Text(string?[,] values) => GridSpace.Create(values);

    // --- The doors ----------------------------------------------------------------------------------
    //
    // Every way a grid can enter the library: built in memory, read whole from a file, read a window
    // at a time. They live here rather than beside one suite because more than one suite states laws
    // across all of them, and two copies of the arrangement are two things to keep in step — which
    // is how a door once ended up shadowed by a nearer helper of the same name and quietly dropped
    // out of a theory.

    /// <summary>The three doors, as theory data: the name is what <see cref="Door"/> takes.</summary>
    public static TheoryData<string> Doors => new TheoryData<string> { "grid", "windowed", "xlsx" };

    /// <summary>
    /// A space behind <paramref name="door"/>. The two in-memory doors hold the same three-by-two
    /// grid, every cell saying its own <c>"c,r"</c>; the eager door is a real workbook, because a
    /// file is the only way that one can be entered, and it therefore carries the report fixture's
    /// own content rather than coordinates. What is stated across all three is shape and refusal,
    /// never what a particular cell says.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="door"/> names no door.</exception>
    public static ICellValues Door(string door)
    {
      switch (door)
      {
        case "grid":
          return Text(new[,]
          {
            { "0,0", "1,0", "2,0" },
            { "0,1", "1,1", "2,1" },
          });

        case "windowed":
          return Windowed(new FakeSheet("Data", 2, 3), chunkRows: 1);

        case "xlsx":
          return Eager("simple-report.xlsx", "Report");

        // Named rather than defaulted: a door that fell through to a neighbour would drop out of
        // every theory it is in and take its laws with it, silently.
        default:
          throw new ArgumentOutOfRangeException(nameof(door), door, "No such door.");
      }
    }

    /// <summary>
    /// <paramref name="sheet"/> read a window at a time, over a synthetic row source rather than a
    /// file: the streaming door's own machinery with nothing of the adapter's in the way.
    /// </summary>
    public static ICellValues Windowed(FakeSheet sheet, int chunkRows = 1, int windowChunks = 4)
    {
      var pool = new ReaderPool(new FakeRowSource(sheet), 1, warmReaders: false);

      return new WindowedSpace(
        new SheetStore(pool, 0, sheet.Name, sheet.RowCount, sheet.ColumnCount, chunkRows, windowChunks));
    }

    /// <summary>A sheet of <paramref name="file"/> in <c>TestData</c>, read whole.</summary>
    public static ICellValues Eager(string file, string sheet)
      => SpreadsheetSpace.Create(Path.Combine(AppContext.BaseDirectory, "TestData", file), sheet);

    /// <summary>
    /// A column of 1..<paramref name="height"/>, so an assertion reads as the row it came from.
    /// </summary>
    public static ICellValues Ladder(int height = 3)
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
    public static ICellValues CoordinateGrid(int width = 4, int height = 3)
    {
      var values = new int[height, width];

      for (var row = 0; row < height; row++)
        for (var column = 0; column < width; column++)
          values[row, column] = row * 10 + column + 1;

      return Grid(values);
    }

    /// <summary>A cell read as a number — the leaf most tests need and none of them vary.</summary>
    public static IProjection<int> IntCell() => Projection.Cell(v => v.GetInt());

    /// <summary>
    /// A cell read as text — the other leaf the suite reaches for by reflex, and the twin of
    /// <see cref="IntCell"/>. Neither is a bare identifier at the use site, so a child written as
    /// <c>v.Next(TextCell())</c> is named exactly as the inline lambda it replaced was: by kind and
    /// ordinal.
    /// </summary>
    public static IProjection<string> TextCell() => Projection.Cell(v => v.GetString());

    /// <summary>
    /// The problem text of a failure, without the subject the message template puts in front of it.
    /// </summary>
    public static string Problem(ProjectionException failure)
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
    public static ICellValues Mixed(object?[,] values) => GridSpace.Create(values, Adapt);

    /// <summary>
    /// One CLR value as the cell it stands for — the rule <see cref="Mixed"/> is built from, shared
    /// so a source that is not a grid (the streaming fake) writes its rows the same way.
    /// </summary>
    public static CellValue Adapt(object? value) =>
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
