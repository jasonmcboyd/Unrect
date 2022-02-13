namespace Unrect.Core
{
  public interface ISpace<out T>
  {
    Area Area { get; }
    T this[int column, int row]
    {
      get
      {
        checked
        {
          return this[(uint)column, (uint)row];
        }
      }
    }
    T this[uint column, uint row]
    {
      get;
    }
    ISpace<T> GetSubspace(Offset offset, Area area);
  }
}
