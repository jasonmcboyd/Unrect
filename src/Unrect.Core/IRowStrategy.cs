namespace Unrect.Core
{
  public interface IRowStrategy<in TSpace>
  {
    uint SelectRows(ISpace<TSpace> space);
  }
}
