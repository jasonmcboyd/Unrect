using System;
using Unrect.Core;

namespace Unrect.Array
{
  public class ArraySpace<T> : ISpace<T>
  {
    public ArraySpace(T[,] array) : this(array, default, new Size((uint)array.GetLength(0), (uint)array.GetLength(1)))
    {
    }

    public ArraySpace(
      T[,] array,
      Offset offset,
      Size size)
    {
      Array = array;

      if (offset.LeftOffset + size.Width > array.GetLength(0) || offset.TopOffset + size.Height > array.GetLength(1))
      {
        throw new OutOfBoundsException();
      }

      Offset = offset;
      Size = size;
    }

    private T[,] Array { get; }
    private Offset Offset { get; }
    public Size Size { get; }

    public T this[int x, int y]
    {
      get
      {
        if (x < 0 || x >= Size.Width) throw new IndexOutOfRangeException();
        if (y < 0 || y >= Size.Height) throw new IndexOutOfRangeException();

        return Array[Offset.TopOffset + y, Offset.LeftOffset + x];
      }
    }

    public ISpace<T> GetSubspace(Offset offset, Size size) => new ArraySpace<T>(Array, offset, size);
  }
}
