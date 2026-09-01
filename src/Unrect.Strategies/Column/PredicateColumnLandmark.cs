using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The column twin of <see cref="PredicateRowLandmark"/>.</summary>
  internal sealed class PredicateColumnLandmark : IColumnLandmark
  {
    public PredicateColumnLandmark(Func<ISpace, int, bool> predicate, string description)
    {
      Predicate = predicate;
      Description = description;
    }

    private Func<ISpace, int, bool> Predicate { get; }

    public string Description { get; }

    public int? FindColumn(ISpace space)
    {
      for (var column = 0; column < space.Area.Size.Width; column++)
        if (Predicate(space, column))
          return column;

      return null;
    }
  }
}
