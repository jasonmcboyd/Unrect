using System;
using System.IO;

using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Tests.Streaming;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// The sheets and small helpers the test suites share. It sits at the project root rather than
  /// beside the projection tests because the strategy tests need the same grids, and one convention
  /// for what "blank" means is worth more than a folder boundary.
  /// <para>
  /// Every grid here is a <em>kinded</em> one: a number cell holds a number, a label cell holds text,
  /// which is what these fixtures have always meant and what lets a suite read a cell as the kind it
  /// wrote. The canonical half — a grid that answers the four questions and nothing else — is
  /// <see cref="ValueTestSpaces"/>.
  /// </para>
  /// </summary>
  internal static class ProjectionTestSpaces
  {
    /// <summary>A grid of numbers in which zero means an empty cell.</summary>
    public static ISheetCells Grid(int[,] values) => Cells(values, number => number == 0 ? Cell.Blank : Cell.Of(number));

    /// <summary>A grid of labels; null and "" are empty cells.</summary>
    public static ISheetCells Labels(string?[,] values)
      => Cells(values, text => string.IsNullOrEmpty(text) ? Cell.Blank : Cell.Of(text!));

    // --- The doors ----------------------------------------------------------------------------------
    //
    // Every way a sheet can enter the library: built in memory, read whole from a file, read a window
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
    public static ISheetCells Door(string door)
    {
      switch (door)
      {
        case "grid":
          return Labels(new[,]
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
    public static ISheetCells Windowed(FakeSheet sheet, int chunkRows = 1, int windowChunks = 4)
    {
      var pool = new ReaderPool(new FakeRowSource(sheet), 1, warmReaders: false);

      return new WindowedSpace(
        new SheetStore(pool, 0, sheet.Name, sheet.RowCount, sheet.ColumnCount, chunkRows, windowChunks));
    }

    /// <summary>A sheet of <paramref name="file"/> in <c>TestData</c>, read whole.</summary>
    public static ISheetCells Eager(string file, string sheet)
      => SpreadsheetSpace.Create(Path.Combine(AppContext.BaseDirectory, "TestData", file), sheet);

    /// <summary>
    /// A column of 1..<paramref name="height"/>, so an assertion reads as the row it came from.
    /// </summary>
    public static ISheetCells Ladder(int height = 3)
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
    public static ISheetCells CoordinateGrid(int width = 4, int height = 3)
    {
      var values = new int[height, width];

      for (var row = 0; row < height; row++)
        for (var column = 0; column < width; column++)
          values[row, column] = row * 10 + column + 1;

      return Grid(values);
    }

    /// <summary>A cell read as a number — the leaf most tests need and none of them vary.</summary>
    public static IProjection<ISheetCells, int> IntCell() => SpreadsheetProjections.Integer<ISheetCells>();

    /// <summary>
    /// A cell read as text — the other leaf the suite reaches for by reflex, and the twin of
    /// <see cref="IntCell"/>. Neither is a bare identifier at the use site, so a child written as
    /// <c>v.Next(TextCell())</c> is named exactly as the inline lambda it replaced was: by kind and
    /// ordinal.
    /// </summary>
    public static IProjection<ISheetCells, string> TextCell() => SpreadsheetProjections.Text<ISheetCells>();

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
    /// A grid of heterogeneous values: null and "" are blank, everything else adapts to the cell kind
    /// its CLR type implies. The array-adapter equivalent of a real sheet, and the kinded home of the
    /// adaptation table <see cref="SheetGrid.Of(object?[,])"/> ships.
    /// </summary>
    public static ISheetCells Mixed(object?[,] values) => SheetGrid.Of(values);

    /// <summary>
    /// One CLR value as the cell it stands for, shared so a source that is not a grid (the streaming
    /// fake) writes its rows the way <see cref="Mixed"/> does.
    /// </summary>
    public static Cell Adapt(object? value) => SheetGrid.Of(new[,] { { value } }).At(0, 0);

    private static ISheetCells Cells<T>(T[,] values, Func<T, Cell> adapt)
    {
      var cells = new Cell[values.GetLength(0), values.GetLength(1)];

      for (var row = 0; row < values.GetLength(0); row++)
        for (var column = 0; column < values.GetLength(1); column++)
          cells[row, column] = adapt(values[row, column]);

      return SheetGrid.Of(cells);
    }
  }
}
