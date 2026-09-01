using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Scans down for the first matching row and says where it is, or that there is none. The same
  /// scan a seek does, without the throwing: a landmark's caller decides what absence means.
  /// </summary>
  internal sealed class PredicateRowLandmark : IRowLandmark
  {
    public PredicateRowLandmark(Func<ISpace, int, bool> predicate, string description)
    {
      Predicate = predicate;
      Description = description;
    }

    private Func<ISpace, int, bool> Predicate { get; }

    public string Description { get; }

    public int? FindRow(ISpace space)
    {
      for (var row = 0; row < space.Area.Size.Height; row++)
        if (Predicate(space, row))
          return row;

      return null;
    }
  }
}
