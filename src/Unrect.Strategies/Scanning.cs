using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// The scans the strategies here are made of: the whole-region forms a strategy falls back to
  /// when asked along an axis it cannot stream, and the scans a predicate or a size drives.
  /// </summary>
  internal static class Scanning
  {
    /// <summary>
    /// A size scan that answers only over the whole region: it takes every span and settles at
    /// the end with the size of everything it was shown. What a strategy builds
    /// along an axis it cannot stream.
    /// </summary>
    internal sealed class WholeSize : ISizeScan
    {
      private readonly Func<Plane<ISpace>, Size> _size;
      private readonly Orientation _along;
      private Plane<ISpace>? _asked;
      private Size _answer;

      internal WholeSize(Func<Plane<ISpace>, Size> size, Orientation along)
      {
        _size = size;
        _along = along;
      }

      public bool Incremental => false;

      public bool Take(Plane<ISpace> region, int taken) => true;

      public int? Across(Plane<ISpace> region, int taken, bool final) => final ? Spans.Across(Of(region), _along) : (int?)null;

      public int Along(Plane<ISpace> region, int taken) => Spans.Along(Of(region), _along);

      /// <summary>The size of <paramref name="region"/>, read once: the length and the width are two questions about one answer.</summary>
      private Size Of(Plane<ISpace> region)
      {
        if (_asked is not Plane<ISpace> asked || !asked.Equals(region))
        {
          _answer = _size(region);
          _asked = region;
        }

        return _answer;
      }

      public bool Complete(int taken) => true;

      public Size Declared => default;
    }

    /// <summary>An offset scan that answers only over the whole region: it skips every span and settles at the end with the offset of everything.</summary>
    internal sealed class WholeOffset : IOffsetScan
    {
      private readonly Func<Plane<ISpace>, Offset> _offset;

      internal WholeOffset(Func<Plane<ISpace>, Offset> offset) => _offset = offset;

      public bool Incremental => false;

      public OffsetStep Next(Plane<ISpace> region, int index, out int across)
      {
        across = 0;
        return OffsetStep.Skip;
      }

      public Offset Settle(Plane<ISpace> region) => _offset(region);
    }

    /// <summary>
    /// A size read as an offset: skip while the size takes, start where it stops, as far across as
    /// it reaches. An explicit 2x3 skips three spans and starts two cells in; a rows-while-blank
    /// skips the blank rows.
    /// </summary>
    internal sealed class SizeAsOffset : IOffsetScan
    {
      private readonly ISizeScan _size;
      private readonly Orientation _along;

      internal SizeAsOffset(ISizeScan size, Orientation along)
      {
        _size = size;
        _along = along;
      }

      public bool Incremental => _size.Incremental;

      public OffsetStep Next(Plane<ISpace> region, int index, out int across)
      {
        if (_size.Take(region, index))
        {
          across = 0;
          return OffsetStep.Skip;
        }

        across = _size.Across(region, index, final: true) ?? 0;
        return OffsetStep.StartHere;
      }

      // The size, wherever it lands: a skip past the end is the placement's to report as not
      // fitting, with what was asked and what was there.
      public Offset Settle(Plane<ISpace> region) => new Offset(Scans.FoldSize(_size, region, _along));
    }

    /// <summary>
    /// A column scan over a rule that accumulates row by row. Asked about a column of a region that
    /// reaches past it — the whole-region fold — it accumulates over every row once and answers
    /// from the count, reading no column it has ruled out; asked about the newest column of a
    /// region that stops there — a column driver — it reads that column.
    /// </summary>
    internal sealed class RowMajorColumns : IColumnScan
    {
      private readonly IRowMajorColumnStrategy _strategy;
      private readonly Func<Plane<ISpace>, int, bool> _column;
      private Plane<ISpace>? _folded;
      private int _count;

      internal RowMajorColumns(IRowMajorColumnStrategy strategy, Func<Plane<ISpace>, int, bool> column)
      {
        _strategy = strategy;
        _column = column;
      }

      public int? Required => null;

      public bool IncludesColumn(Plane<ISpace> space, int column)
      {
        // Already folded over this region: every column of it is answered from the count, the last
        // one included, so no column is read a second time on the way to it.
        if (_folded is Plane<ISpace> folded && folded.Equals(space))
          return column < _count;

        if (space.Width == column + 1)
          return _column(space, column);

        _count = ColumnAccumulators.Fold(_strategy.BeginColumns(space.Width), space);
        _folded = space;

        return column < _count;
      }
    }

    /// <summary>A column scan driven by a predicate over the whole column, with no columns owed.</summary>
    internal sealed class ColumnPredicate : IColumnScan
    {
      private readonly Func<Plane<ISpace>, int, bool> _predicate;

      internal ColumnPredicate(Func<Plane<ISpace>, int, bool> predicate) => _predicate = predicate;

      public bool IncludesColumn(Plane<ISpace> space, int column) => _predicate(space, column);

      public int? Required => null;
    }
  }
}
