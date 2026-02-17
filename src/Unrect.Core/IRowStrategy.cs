namespace Unrect.Core
{
  public interface IRowStrategy<in TSpace>
  {
    int SelectRows(ISpace<TSpace> space);
  }
}
