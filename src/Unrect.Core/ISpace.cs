namespace Unrect.Core
{
  public interface ISpace
  {
    Area Area { get; }
    CellValue this[int column, int row] { get; }
    ISpace GetSubspace(Offset offset, Area area);
  }
}
