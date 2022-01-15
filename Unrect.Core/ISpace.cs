namespace Unrect.Core
{
  public interface ISpace<out T>
  {
    Size Size { get; }
    T this[int x, int y]
    {
        get;
    }
    ISpace<T> GetSubspace(Offset offset, Size size);
  }
}
