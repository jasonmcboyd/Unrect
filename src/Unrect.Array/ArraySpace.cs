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

      if (offset.Size.Width + area.Size.Width > array.GetLength(1) || offset.Size.Height + area.Size.Height > array.GetLength(0))
      {
        throw new OutOfBoundsException();
      }

      Offset = offset;
      Area = area;
    }

    private TSpace[,] Array { get; }
    private Offset Offset { get; }
    public Area Area { get; }

    public TSpace this[uint column, uint row]
    {
      get
      {
        if (column < 0 || column >= Area.Size.Width) throw new IndexOutOfRangeException();
        if (row < 0 || row >= Area.Size.Height) throw new IndexOutOfRangeException();

        return Array[Offset.Size.Height + row, Offset.Size.Width + column];
      }
    }

    public ISpace<TSpace> GetSubspace(Offset offset, Area area) => new ArraySpace<TSpace>(Array, offset + Offset, area);
  }
}
