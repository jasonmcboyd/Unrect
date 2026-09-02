namespace Unrect.Core
{
  /// <summary>How many of a space's leading rows a shape claims — the row half of an area, picked independently of the column half.</summary>
  public interface IRowStrategy
  {
    /// <summary>How many leading rows of <paramref name="space"/>, from the top, this strategy selects.</summary>
    int SelectRows(ISpace space);
  }
}
