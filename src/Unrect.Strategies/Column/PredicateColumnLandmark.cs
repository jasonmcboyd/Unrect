using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The column twin of <see cref="PredicateRowLandmark"/>.</summary>
  internal sealed class PredicateColumnLandmark : IColumnLandmark
  {
    public PredicateColumnLandmark(Func<ICellValues, int, bool> predicate, string description)
    {
      Predicate = predicate;
      Description = description;
    }

    private Func<ICellValues, int, bool> Predicate { get; }

    public string Description { get; }

    public int? FindColumn(ICellValues space)
    {
      for (var column = 0; column < space.Area.Width; column++)
        if (Predicate(space, column))
          return column;

      return null;
    }
  }
}
