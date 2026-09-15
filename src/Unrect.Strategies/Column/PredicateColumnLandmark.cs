using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The column twin of <see cref="PredicateRowLandmark"/>.</summary>
  internal sealed class PredicateColumnLandmark : IColumnLandmark
  {
    public PredicateColumnLandmark(Func<Plane<ISpace>, int, bool> predicate, string description)
    {
      Predicate = predicate;
      Description = description;
    }

    private Func<Plane<ISpace>, int, bool> Predicate { get; }

    public string Description { get; }

    public int? FindColumn(Plane<ISpace> space)
    {
      for (var column = 0; column < space.Width; column++)
        if (Predicate(space, column))
          return column;

      return null;
    }
  }
}
