using System;
using Unrect.Core;

namespace Unrect.Array
{
  public class ArraySpace : ISpace
  {
    public ArraySpace(CellValue[,] values)
      : this(ValidateNoNulls(values), default, new Area(values.GetLength(1), values.GetLength(0)))
    {
    }

    private ArraySpace(
      CellValue[,] values,
      Offset offset,
      Area area)
    {
      Values = values;

      if (offset.Size.Width + area.Size.Width > values.GetLength(1) || offset.Size.Height + area.Size.Height > values.GetLength(0))
      {
        throw new OutOfBoundsException();
      }

      Offset = offset;
      Area = area;
    }

    public static ArraySpace Create<T>(T[,] values, Func<T, CellValue> map)
    {
      var cells = new CellValue[values.GetLength(0), values.GetLength(1)];

      for (int row = 0; row < values.GetLength(0); row++)
        for (int column = 0; column < values.GetLength(1); column++)
          cells[row, column] =
            map(values[row, column])
            ?? throw new ArgumentException($"Map returned null for the value at column {column}, row {row}.", nameof(map));

      return new ArraySpace(cells, default, new Area(cells.GetLength(1), cells.GetLength(0)));
    }

    public static ArraySpace Create(int[,] values, Func<int, bool>? isBlank = null)
      => Create(values, v => isBlank?.Invoke(v) == true ? CellValue.Blank : CellValue.Of(v));

    public static ArraySpace Create(double[,] values, Func<double, bool>? isBlank = null)
      => Create(values, v => isBlank?.Invoke(v) == true ? CellValue.Blank : CellValue.Of(v));

    // This adapter's blankness default: a null or empty string is an empty cell.
    public static ArraySpace Create(string?[,] values)
      => Create(values, v => string.IsNullOrEmpty(v) ? CellValue.Blank : CellValue.Of(v));

    private CellValue[,] Values { get; }
    private Offset Offset { get; }
    public Area Area { get; }

    public CellValue this[int column, int row]
    {
      get
      {
        if (column < 0 || column >= Area.Size.Width) throw new IndexOutOfRangeException();
        if (row < 0 || row >= Area.Size.Height) throw new IndexOutOfRangeException();

        return Values[Offset.Size.Height + row, Offset.Size.Width + column];
      }
    }

    public ISpace GetSubspace(Offset offset, Area area)
    {
      if (offset.Size.Width + area.Size.Width > Area.Size.Width || offset.Size.Height + area.Size.Height > Area.Size.Height)
        throw new OutOfBoundsException();

      return new ArraySpace(Values, offset + Offset, area);
    }

    private static CellValue[,] ValidateNoNulls(CellValue[,] values)
    {
      for (int row = 0; row < values.GetLength(0); row++)
        for (int column = 0; column < values.GetLength(1); column++)
          if (values[row, column] is null)
            throw new ArgumentException($"The value at column {column}, row {row} is null.", nameof(values));

      return values;
    }
  }
}
