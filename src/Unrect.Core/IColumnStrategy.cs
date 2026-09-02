namespace Unrect.Core
{
  /// <summary>The column twin of <see cref="IRowStrategy"/>: how many of a space's leading columns a shape claims.</summary>
  public interface IColumnStrategy
  {
    /// <summary>How many leading columns of <paramref name="space"/>, from the left, this strategy selects.</summary>
    int SelectColumns(ISpace space);
  }
}
