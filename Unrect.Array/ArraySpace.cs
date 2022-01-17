using System;
using Unrect.Core;

namespace Unrect.Array
{
  public class ArraySpace<TSpace> : ISpace<TSpace>
  {
    public ArraySpace(TSpace[,] array) : this(array, default, new Size((uint)array.GetLength(1), (uint)array.GetLength(0)))
    {
    }

    public ArraySpace(
      TSpace[,] array,
      Offset offset,
      Size size)
    {
      Array = array;

      if (offset.LeftOffset + size.Width > array.GetLength(1) || offset.TopOffset + size.Height > array.GetLength(0))
      {
        throw new OutOfBoundsException();
      }

      Offset = offset;
      Size = size;
    }

    private TSpace[,] Array { get; }
    private Offset Offset { get; }
    public Size Size { get; }

    public TSpace this[int column, int row]
    {
      get
      {
        if (column < 0 || column >= Size.Width) throw new IndexOutOfRangeException();
        if (row < 0 || row >= Size.Height) throw new IndexOutOfRangeException();

        return Array[Offset.TopOffset + row, Offset.LeftOffset + column];
      }
    }

    public ISpace<TSpace> GetSubspace(Offset offset, Size size) => new ArraySpace<TSpace>(Array, offset + Offset, size);
  }
}
