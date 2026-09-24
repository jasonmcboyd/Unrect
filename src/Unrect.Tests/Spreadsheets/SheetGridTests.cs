using System;

using Unrect.Core;
using Unrect.Spreadsheets;

using Xunit;

namespace Unrect.Tests.Spreadsheets
{
  /// <summary>
  /// <see cref="SheetGrid"/> is the eager door's grid and what every adapter ends up holding: an
  /// array of cells whose kinds are already decided, read as a sheet.
  /// </summary>
  public class SheetGridTests
  {
    [Fact]
    public void ACellNobodyFilledInIsBlank()
    {
      // A CellValue is a value type whose default IS blank, so an array with a gap in it holds an empty
      // cell rather than something broken — the state is unrepresentable rather than merely
      // rejected, and this is where that structural fact is pinned.
      var values = new CellValue[1, 2];
      values[0, 0] = CellValue.Of(1);

      var grid = SheetGrid.Of(values);

      Assert.False(grid.IsBlank(0, 0));
      Assert.Equal("1", grid.AsText(0, 0));
      Assert.True(grid.IsBlank(1, 0));
      Assert.Null(grid.AsText(1, 0));
    }

    [Fact]
    public void TheArrayIsRowMajorAndTheSheetIsColumnThenRow()
    {
      // The transposition this type exists to perform, and the reason no adapter has to think about
      // it: the literal reads as rows, the sheet is addressed as a spreadsheet address is.
      var grid = SheetGrid.Of(new object?[,]
      {
        { "a", "b", "c" },
        { "d", "e", "f" },
      });

      Assert.Equal(3, grid.Area.Size.Width);
      Assert.Equal(2, grid.Area.Size.Height);
      Assert.Equal("c", grid.AsText(2, 0));
      Assert.Equal("d", grid.AsText(0, 1));
    }

    [Fact]
    public void Of_AdaptsEachValueToTheKindItsClrTypeImplies()
    {
      // The adaptation table, which is what lets a fixture be written as a literal. A CellValue passes
      // straight through, because an error is the one kind with no CLR literal to write it as.
      var grid = SheetGrid.Of(new object?[,]
      {
        { "word", 42, 3.5m, new DateTime(2026, 1, 15), true, null, "", CellValue.OfError(CellError.Value) },
      });

      var cells = Plane<SheetGrid>.Of(grid);

      Assert.Equal("word", grid.AsText(0, 0));
      Assert.True(grid.IsText(0, 0));

      Assert.Equal(42, cells[1, 0].Integer());
      Assert.Equal(3.5m, cells[2, 0].Decimal());
      Assert.Equal(new DateTime(2026, 1, 15), cells[3, 0].Date());
      Assert.True(cells[4, 0].Boolean());

      Assert.True(grid.IsBlank(5, 0));
      Assert.True(grid.IsBlank(6, 0));

      Assert.True(grid.ValueAt(7, 0).Kind == CellKind.Error);
      Assert.Equal("#VALUE!", grid.AsText(7, 0));
      Assert.Equal("Error(#VALUE!)", grid.Describe(7, 0));
    }

    // --- The two array doors, and what tells them apart ----------------------------------------------

    [Fact]
    public void TheKindedDoorAndTheCanonicalDoorReadTheSameLiteralsTheSameWay()
    {
      // SheetGrid.Of and GridSpace.Create take the same array of the same literals, and everything
      // an ISpace can be asked about them is identical: how big, which cells are blank, which are
      // their own text, and what each of them says. That is the floor, and it is the whole of what a
      // declaration written over the canonical surface can see.
      var values = new object?[,] { { "word", 42, 3.5m, new DateTime(2026, 1, 15), true, null, "" } };

      ISpace kinded = SheetGrid.Of(values);
      ISpace canonical = GridSpace.Create(values);

      Assert.Equal(kinded.Area.Size.Width, canonical.Area.Size.Width);
      Assert.Equal(kinded.Area.Size.Height, canonical.Area.Size.Height);

      for (var column = 0; column < 7; column++)
      {
        Assert.Equal(kinded.IsBlank(column, 0), canonical.IsBlank(column, 0));
        Assert.Equal(kinded.IsText(column, 0), canonical.IsText(column, 0));
        Assert.Equal(kinded.AsText(column, 0), canonical.AsText(column, 0));
      }
    }

    [Fact]
    public void AndTheDifferenceIsTheKindsOneOfThemKeeps()
    {
      // The ceiling, and the reason there are two doors rather than one. A SheetGrid remembers what
      // KIND each cell is, so a declaration over it can ask for a decimal and be told no; a
      // GridSpace remembers only the rendering, so the same question is unspellable — the extension
      // is not on a point over a plain space at all, and nothing about a canonical cell could answer
      // it if it were.
      var values = new object?[,] { { 3.5m } };

      Assert.Equal(3.5m, Plane<SheetGrid>.Of(SheetGrid.Of(values))[0, 0].Decimal());

      // What the canonical door has instead: the rendering, which is the same string either way.
      Assert.Equal("3.5", GridSpace.Create(values).AsText(0, 0));
      Assert.Equal("3.5", SheetGrid.Of(values).AsText(0, 0));
    }

    [Fact]
    public void AnErrorCellIsTheOneLiteralOnlyTheKindedDoorTakes()
    {
      // A CellValue passes through SheetGrid.Of because an error is the one kind with no CLR literal to
      // write it as — and the canonical door has no vocabulary for one, so it refuses the value
      // rather than rendering something. Where the two doors differ, they differ loudly.
      var values = new object?[,] { { CellValue.OfError(CellError.Value) } };

      Assert.Equal("#VALUE!", SheetGrid.Of(values).AsText(0, 0));

      // And they refuse at different moments, which is the other half of the difference: the kinded
      // door decides every cell's kind when the grid is made, so a value it has no kind for is an
      // error THERE; the canonical door decides nothing until a cell is read, so the same value is
      // an error at the read. Both are ArgumentException — a value nobody can adapt is an argument
      // bug either way, never a bounds condition a declaration could recover from.
      var canonical = GridSpace.Create(values);

      Assert.Throws<ArgumentException>(() => canonical.AsText(0, 0));
    }

    [Fact]
    public void Of_RefusesAValueWithNoCellKind()
    {
      // An error where the grid is built, not a cell that reads as something surprising.
      Assert.Throws<ArgumentException>(() => SheetGrid.Of(new object?[,] { { new object() } }));
      Assert.Throws<ArgumentNullException>(() => SheetGrid.Of((object?[,])null!));
      Assert.Throws<ArgumentNullException>(() => SheetGrid.Of((CellValue[,])null!));
    }
  }
}
