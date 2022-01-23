using System;
using Unrect.Core;

namespace Unrect.Array
{
  public class ArraySpace<TSpace> : ISpace<TSpace>
  {
    public ArraySpace(TSpace[,] array) : this(array, default, new Area((uint)array.GetLength(1), (uint)array.GetLength(0)))
    {
    }

    public ArraySpace(
      TSpace[,] array,
      Offset offset,
      Area area)
    {
      Array = array;

      if (offset.Width + area.Width > array.GetLength(1) || offset.Height + area.Height > array.GetLength(0))
      {
        throw new OutOfBoundsException();
      }

      Offset = offset;
      Area = area;
    }

    private TSpace[,] Array { get; }
    private Offset Offset { get; }
    public Area Area { get; }

    public TSpace this[int column, int row]
    {
      get
      {
        if (column < 0 || column >= Area.Width) throw new IndexOutOfRangeException();
        if (row < 0 || row >= Area.Height) throw new IndexOutOfRangeException();

        return Array[Offset.Height + row, Offset.Width + column];
      }
    }

    public ISpace<TSpace> GetSubspace(Offset offset, Area area) => new ArraySpace<TSpace>(Array, offset + Offset, area);
  }
}
