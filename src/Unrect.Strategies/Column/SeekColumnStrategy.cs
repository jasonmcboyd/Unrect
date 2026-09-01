using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Scans right to the first matching column and selects the columns before it, so an offset
  /// lifted from this strategy lands the region ON the match. The column twin of
  /// <see cref="SeekRowStrategy"/>.
  /// </summary>
  internal class SeekColumnStrategy : IColumnStrategy
  {
    public SeekColumnStrategy(Func<ISpace, int, bool> predicate, string description)
    {
      Predicate = predicate;
      Description = description;
    }

    private Func<ISpace, int, bool> Predicate { get; }
    private string Description { get; }

    public int SelectColumns(ISpace space)
    {
      for (var column = 0; column < space.Area.Size.Width; column++)
        if (Predicate(space, column))
          return column;

      throw new AnchorNotFoundException(Description);
    }
  }
}
