namespace Unrect.Core
{
  public interface ISpace<out T>
  {
    Size Size { get; }
    T this[int column, int row]
    {
        get;
    }
    ISpace<T> GetSubspace(Offset offset, Size size);
  }
}
