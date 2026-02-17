namespace Unrect.Core
{
  public interface IColumnStrategy<in TSpace>
  {
    int SelectColumns(ISpace<TSpace> space);
  }
}
