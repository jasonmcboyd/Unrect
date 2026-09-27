using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// The scans the strategies here are made of: the whole-region form a strategy falls back to
  /// when asked along an axis it cannot stream, and the scans a predicate drives.
  /// </summary>
  internal static class Scanning
  {
    /// <summary>
    /// A size scan that answers only over the whole region: it takes every span and settles at
    /// the end with the size of everything it was shown. What a strategy builds
    /// along an axis it cannot stream.
    /// </summary>
    internal sealed class WholeSize<TSpace> : ISizeScan<TSpace>
      where TSpace : class, ISpace
    {
      private readonly Func<Plane<TSpace>, Size> _size;
      private readonly Orientation _along;
      private Plane<TSpace>? _asked;
      private Size _answer;

      internal WholeSize(Func<Plane<TSpace>, Size> size, Orientation along)
      {
        _size = size;
        _along = along;
      }

      public bool Incremental => false;

      public bool Take(Plane<TSpace> region, int taken) => true;

      public int? Across(Plane<TSpace> region, int taken, bool final) => final ? Spans.Across(Of(region), _along) : (int?)null;

      public int Along(Plane<TSpace> region, int taken) => Spans.Along(Of(region), _along);

      /// <summary>The size of <paramref name="region"/>, read once: the length and the width are two questions about one answer.</summary>
      private Size Of(Plane<TSpace> region)
      {
        if (_asked is not Plane<TSpace> asked || !asked.Equals(region))
        {
          _answer = _size(region);
          _asked = region;
        }

        return _answer;
      }

      public Size? Required => null;
    }

    /// <summary>
    /// A column scan over a rule that accumulates row by row. Asked about a column of a region that
    /// reaches past it — the whole-region fold — it accumulates over every row once and answers
    /// from the count, reading no column it has ruled out; asked about the newest column of a
    /// region that stops there — a column driver — it reads that column.
    /// </summary>
    internal sealed class RowMajorColumns<TSpace> : ILineScan<TSpace>
      where TSpace : class, ISpace
    {
      private readonly IRowMajorColumnStrategy<TSpace> _strategy;
      private readonly Func<Plane<TSpace>, int, bool> _column;
      private Plane<TSpace>? _folded;
      private int _count;

      internal RowMajorColumns(IRowMajorColumnStrategy<TSpace> strategy, Func<Plane<TSpace>, int, bool> column)
      {
        _strategy = strategy;
        _column = column;
      }

      public int? Required => null;

      public bool Includes(Plane<TSpace> space, int column)
      {
        // Already folded over this region: every column of it is answered from the count, the last
        // one included, so no column is read a second time on the way to it.
        if (_folded is Plane<TSpace> folded && folded.Equals(space))
          return column < _count;

        if (space.Width == column + 1)
          return _column(space, column);

        _count = ColumnAccumulators.Fold(_strategy.BeginColumns(space.Width), space);
        _folded = space;

        return column < _count;
      }
    }

    /// <summary>A column scan driven by a predicate over the whole column, with no columns owed.</summary>
    internal sealed class ColumnPredicate<TSpace> : ILineScan<TSpace>
      where TSpace : class, ISpace
    {
      private readonly Func<Plane<TSpace>, int, bool> _predicate;

      internal ColumnPredicate(Func<Plane<TSpace>, int, bool> predicate) => _predicate = predicate;

      public bool Includes(Plane<TSpace> space, int column) => _predicate(space, column);

      public int? Required => null;
    }
  }
}
