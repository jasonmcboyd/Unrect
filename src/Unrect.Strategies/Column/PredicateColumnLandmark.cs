using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The column twin of <see cref="PredicateRowLandmark{TSpace}"/>.</summary>
  internal sealed class PredicateColumnLandmark<TSpace> : ILineLandmark<TSpace>
    where TSpace : class, ISpace
  {
    public Orientation Along => Orientation.Horizontal;

    public PredicateColumnLandmark(Func<Plane<TSpace>, int, bool> predicate, string description)
    {
      Predicate = predicate;
      Description = description;
    }

    private Func<Plane<TSpace>, int, bool> Predicate { get; }

    public string Description { get; }

    public int? Find(Plane<TSpace> space)
    {
      for (var column = 0; column < space.Width; column++)
        if (Predicate(space, column))
          return column;

      return null;
    }
  }
}
