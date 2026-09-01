using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Scans down to the first matching row and selects the rows before it, so an offset lifted from
  /// this strategy lands the region ON the match. Anchoring on presence rather than absence is what
  /// survives inserted junk above the thing being looked for.
  /// </summary>
  internal class SeekRowStrategy : IRowStrategy
  {
    public SeekRowStrategy(Func<ISpace, int, bool> predicate, string description)
    {
      Predicate = predicate;
      Description = description;
    }

    private Func<ISpace, int, bool> Predicate { get; }
    private string Description { get; }

    public int SelectRows(ISpace space)
    {
      for (var row = 0; row < space.Area.Size.Height; row++)
        if (Predicate(space, row))
          return row;

      throw new AnchorNotFoundException(Description);
    }
  }
}
