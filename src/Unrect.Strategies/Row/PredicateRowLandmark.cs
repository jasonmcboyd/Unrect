using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Scans down for the first matching row and says where it is, or that there is none. The same
  /// scan a placement does, without the throwing: a matcher's caller decides what absence means.
  /// </summary>
  internal sealed class PredicateRowLandmark : ILineLandmark
  {
    public Orientation Along => Orientation.Vertical;

    public PredicateRowLandmark(Func<Plane<ISpace>, int, bool> predicate, string description)
    {
      Predicate = predicate;
      Description = description;
    }

    private Func<Plane<ISpace>, int, bool> Predicate { get; }

    public string Description { get; }

    public int? Find(Plane<ISpace> space)
    {
      for (var row = 0; row < space.Height; row++)
        if (Predicate(space, row))
          return row;

      return null;
    }
  }
}
