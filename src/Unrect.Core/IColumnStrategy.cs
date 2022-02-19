namespace Unrect.Core
{
  public interface IColumnStrategy<in TSpace>
  {
    uint SelectColumns(ISpace<TSpace> space);
  }
}
