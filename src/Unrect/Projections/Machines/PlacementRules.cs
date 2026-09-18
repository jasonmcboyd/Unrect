using System;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Projections
{
  internal enum OffsetStep
  {
    /// <summary>This span is part of the offset; keep looking.</summary>
    Skip,

    /// <summary>The offset resolved; this span is the inner region's first.</summary>
    StartHere,

    /// <summary>The offset resolved past this span; the next span is the inner region's first.</summary>
    StartNext,
  }

  /// <summary>An offset strategy driven one span at a time along the driver's axis.</summary>
  internal abstract class OffsetRule
  {
    /// <param name="region">The spans this rule has been shown, the current one included, from its own start.</param>
    /// <param name="row">The current span's index within <paramref name="region"/>.</param>
    /// <param name="column">The across-axis offset, when the step resolves.</param>
    public abstract OffsetStep Next(Plane<ISpace> region, int row, out int column);
  }

  /// <summary>An area strategy driven one span at a time: whether to take a span, and the width once it can be known.</summary>
  internal abstract class SizeRule
  {
    /// <param name="region">The inner region so far, the current span included.</param>
    /// <param name="row">The current span's index within the inner region.</param>
    public abstract bool Take(Plane<ISpace> region, int row);

    /// <summary>The width, or null while it cannot be known yet. <paramref name="rowsSettled"/>: no more rows will be taken.</summary>
    public abstract int? Width(Plane<ISpace> region, int rows, bool rowsSettled);

    /// <summary>Whether <paramref name="rows"/> is enough — false only for an explicit height the feed could not supply.</summary>
    public virtual bool Complete(int rows) => true;

    /// <summary>The size an incomplete rule declared, for the failure that names it.</summary>
    public virtual Size Declared => default;
  }

  /// <summary>
  /// Which of the calculus's strategies can be driven per span, and the rule for each. Anything
  /// unrecognised is not streamed: the child is held and placed eagerly over its whole region, which
  /// is the pull engine's own semantics and therefore always right, only later.
  /// </summary>
  internal static class PlacementRules
  {
    internal static bool TryStream(Placement placement, Orientation driver, out OffsetRule? offset, out SizeRule? size, out bool derived)
    {
      size = null;
      derived = false;

      if (!TryOffset(placement.Offset, driver, out offset))
        return false;

      if (placement.Area is null)
      {
        derived = true;
        return true;
      }

      return driver == Orientation.Vertical ? TrySize(placement.Area, out size) : TrySizeAcross(placement.Area, out size);
    }

    /// <summary>The per-span form of <paramref name="strategy"/> along <paramref name="driver"/>, for a repeat's separator; false when it has none.</summary>
    internal static bool TryOffsetRule(IOffsetStrategy strategy, Orientation driver, out OffsetRule? rule) => TryOffset(strategy, driver, out rule);

    private static bool TryOffset(IOffsetStrategy strategy, Orientation driver, out OffsetRule? rule)
    {
      switch (strategy)
      {
        case OffsetStrategy offset:
          return TryOffsetSize(offset.Strategy, driver, out rule);
        case SkipToFirstNonBlankCellStrategy:
          rule = new FirstNonBlankOffsetRule(driver);
          return true;
        default:
          rule = null;
          return false;
      }
    }

    private static bool TryOffsetSize(ISizeStrategy strategy, Orientation driver, out OffsetRule? rule)
    {
      switch (strategy)
      {
        case ExplicitSizeStrategy explicitSize:
          rule = driver == Orientation.Vertical
            ? new ExplicitOffsetRule(explicitSize.Width, explicitSize.Height)
            : new ExplicitOffsetRule(explicitSize.Height, explicitSize.Width);
          return true;
        case RowOffsetSizeStrategy rows when driver == Orientation.Vertical:
          return TryRows(rows.RowSelectionStrategy, out rule);
        case ColumnOffsetSizeStrategy columns when driver == Orientation.Horizontal:
          return TryColumns(columns.ColumnSelectionStrategy, out rule);
        case CompositeOffsetSizeStrategy composite:
        {
          var stages = new OffsetRule[composite.Strategies.Length];

          for (var index = 0; index < stages.Length; index++)
          {
            if (!TryOffset(composite.Strategies[index], driver, out var stage))
            {
              rule = null;
              return false;
            }

            stages[index] = stage!;
          }

          rule = stages.Length == 0 ? new ExplicitOffsetRule(0, 0) : stages.Length == 1 ? stages[0] : new ChainOffsetRule(stages, driver);
          return true;
        }
        default:
          rule = null;
          return false;
      }
    }

    private static bool TryColumns(IColumnStrategy columns, out OffsetRule? rule)
    {
      switch (columns)
      {
        case ExplicitColumnCountStrategy count:
          rule = new ExplicitOffsetRule(0, count.Count);
          return true;
        case LandmarkColumnStrategy landmark:
          rule = new ColumnLandmarkOffsetRule(landmark.Landmark, landmark.Past);
          return true;
        case TakeWhileAllColumnStrategy all:
          rule = new ColumnPredicateOffsetRule(all.Predicate, every: true);
          return true;
        case TakeWhileAnyColumnStrategy any:
          rule = new ColumnPredicateOffsetRule(any.Predicate, every: false);
          return true;
        default:
          rule = null;
          return false;
      }
    }

    /// <summary>The size rules a column-span driver can run: an explicit size, or the whole extent.</summary>
    private static bool TrySizeAcross(IAreaStrategy area, out SizeRule? rule)
    {
      if (area is AreaStrategy plain)
      {
        switch (plain.Strategy)
        {
          case ExplicitSizeStrategy explicitSize:
            rule = new ExplicitSizeRule(explicitSize.Height, explicitSize.Width);
            return true;
          case MaxSizeStrategy:
            rule = new MaxSizeRule(Orientation.Horizontal);
            return true;
        }
      }

      rule = null;
      return false;
    }

    private static bool TryRows(IRowStrategy rows, out OffsetRule? rule)
    {
      switch (rows)
      {
        case ExplicitRowCountStrategy count:
          rule = new ExplicitOffsetRule(0, count.Count);
          return true;
        case LandmarkRowStrategy landmark:
          rule = new LandmarkOffsetRule(landmark.Landmark, landmark.Past);
          return true;
        case IIncrementalRowStrategy incremental:
          rule = new ScanOffsetRule(incremental.BeginRows());
          return true;
        default:
          rule = null;
          return false;
      }
    }

    private static bool TrySize(IAreaStrategy area, out SizeRule? rule)
    {
      switch (area)
      {
        case IncrementalAreaStrategy incremental:
          switch (incremental.Strategy)
          {
            case RowsWhileAnySizeStrategy rowsWhile:
              rule = new ScanSizeRule(rowsWhile.RowSelectionStrategy.BeginRows());
              return true;
            case InterleavedRowAndColumnSizeStrategy interleaved:
              rule = new InterleavedSizeRule(interleaved.RowSelectionStrategy.BeginRows(), interleaved.ColumnSelectionStrategy);
              return true;
            default:
              rule = null;
              return false;
          }
        case AreaStrategy plain:
          switch (plain.Strategy)
          {
            case ExplicitSizeStrategy explicitSize:
              rule = new ExplicitSizeRule(explicitSize.Width, explicitSize.Height);
              return true;
            case MaxSizeStrategy:
              rule = new MaxSizeRule(Orientation.Vertical);
              return true;
            case RowAndColumnSizeStrategy rowsThenColumns when rowsThenColumns.RowFirst:
              switch (rowsThenColumns.RowSelectionStrategy)
              {
                case ExplicitRowCountStrategy count:
                  rule = new RowsThenColumnsRule(count.Count, null, rowsThenColumns.ColumnSelectionStrategy);
                  return true;
                case IIncrementalRowStrategy incrementalRows:
                  rule = new RowsThenColumnsRule(null, incrementalRows.BeginRows(), rowsThenColumns.ColumnSelectionStrategy);
                  return true;
                default:
                  rule = null;
                  return false;
              }
            default:
              rule = null;
              return false;
          }
        default:
          rule = null;
          return false;
      }
    }

    // --- Offset rules -------------------------------------------------------------------------

    private sealed class ExplicitOffsetRule : OffsetRule
    {
      private readonly int _width;
      private readonly int _height;

      public ExplicitOffsetRule(int width, int height)
      {
        _width = width;
        _height = height;
      }

      public override OffsetStep Next(Plane<ISpace> region, int row, out int column)
      {
        column = _width;
        return row < _height ? OffsetStep.Skip : OffsetStep.StartHere;
      }
    }

    private sealed class ScanOffsetRule : OffsetRule
    {
      private readonly IRowScan _scan;

      public ScanOffsetRule(IRowScan scan) => _scan = scan;

      public override OffsetStep Next(Plane<ISpace> region, int row, out int column)
      {
        column = 0;
        return _scan.IncludesRow(region, row) ? OffsetStep.Skip : OffsetStep.StartHere;
      }
    }

    /// <summary>
    /// A row landmark, handed the region searched so far as it is handed the extent today; it can
    /// only match on the newest span, since nothing earlier did.
    /// </summary>
    private sealed class LandmarkOffsetRule : OffsetRule
    {
      private readonly IRowLandmark _landmark;
      private readonly bool _past;

      public LandmarkOffsetRule(IRowLandmark landmark, bool past)
      {
        _landmark = landmark;
        _past = past;
      }

      public override OffsetStep Next(Plane<ISpace> region, int row, out int column)
      {
        column = 0;

        if (_landmark.FindRow(region) is null)
          return OffsetStep.Skip;

        return _past ? OffsetStep.StartNext : OffsetStep.StartHere;
      }
    }

    private sealed class ColumnLandmarkOffsetRule : OffsetRule
    {
      private readonly IColumnLandmark _landmark;
      private readonly bool _past;

      public ColumnLandmarkOffsetRule(IColumnLandmark landmark, bool past)
      {
        _landmark = landmark;
        _past = past;
      }

      public override OffsetStep Next(Plane<ISpace> region, int row, out int column)
      {
        column = 0;

        if (_landmark.FindColumn(region) is null)
          return OffsetStep.Skip;

        return _past ? OffsetStep.StartNext : OffsetStep.StartHere;
      }
    }

    /// <summary>Skips column spans while every (or any) cell of the column satisfies the predicate.</summary>
    private sealed class ColumnPredicateOffsetRule : OffsetRule
    {
      private readonly Func<Point<ISpace>, bool> _predicate;
      private readonly bool _every;

      public ColumnPredicateOffsetRule(Func<Point<ISpace>, bool> predicate, bool every)
      {
        _predicate = predicate;
        _every = every;
      }

      public override OffsetStep Next(Plane<ISpace> region, int row, out int column)
      {
        column = 0;
        var height = region.Declared.Height;
        var satisfied = _every;

        for (var cell = 0; cell < height; cell++)
        {
          var holds = _predicate(region[row, cell]);

          if (_every && !holds)
          {
            satisfied = false;
            break;
          }

          if (!_every && holds)
          {
            satisfied = true;
            break;
          }
        }

        return satisfied ? OffsetStep.Skip : OffsetStep.StartHere;
      }
    }

    private sealed class FirstNonBlankOffsetRule : OffsetRule
    {
      private readonly Orientation _driver;

      public FirstNonBlankOffsetRule(Orientation driver) => _driver = driver;

      public override OffsetStep Next(Plane<ISpace> region, int row, out int column)
      {
        var across = _driver == Orientation.Vertical ? region.Width : region.Declared.Height;

        for (column = 0; column < across; column++)
        {
          var cell = _driver == Orientation.Vertical ? region[column, row] : region[row, column];

          if (cell.HasValue)
            return OffsetStep.StartHere;
        }

        column = 0;
        return OffsetStep.Skip;
      }
    }

    private sealed class ChainOffsetRule : OffsetRule
    {
      private readonly OffsetRule[] _stages;
      private readonly Orientation _driver;
      private int _stage;
      private int _stageStart;
      private int _column;

      public ChainOffsetRule(OffsetRule[] stages, Orientation driver)
      {
        _stages = stages;
        _driver = driver;
      }

      public override OffsetStep Next(Plane<ISpace> region, int row, out int column)
      {
        while (true)
        {
          var stageRegion = region.Slice(_driver == Orientation.Vertical ? new Offset(_column, _stageStart) : new Offset(_stageStart, _column));
          var step = _stages[_stage].Next(stageRegion, row - _stageStart, out var stageColumn);

          if (step == OffsetStep.Skip)
          {
            column = _column;
            return OffsetStep.Skip;
          }

          _column += stageColumn;
          column = _column;

          if (_stage == _stages.Length - 1)
            return step;

          _stage++;

          if (step == OffsetStep.StartHere)
          {
            _stageStart = row;
            continue;
          }

          _stageStart = row + 1;
          return OffsetStep.Skip;
        }
      }
    }

    // --- Size rules ---------------------------------------------------------------------------

    private sealed class ExplicitSizeRule : SizeRule
    {
      private readonly int _width;
      private readonly int _height;

      public ExplicitSizeRule(int width, int height)
      {
        _width = width;
        _height = height;
      }

      public override bool Take(Plane<ISpace> region, int row) => row < _height;

      public override int? Width(Plane<ISpace> region, int rows, bool rowsSettled) => _width;

      public override bool Complete(int rows) => rows == _height;

      public override Size Declared => new Size(_width, _height);
    }

    private sealed class MaxSizeRule : SizeRule
    {
      private readonly Orientation _driver;

      public MaxSizeRule(Orientation driver) => _driver = driver;

      public override bool Take(Plane<ISpace> region, int row) => true;

      public override int? Width(Plane<ISpace> region, int rows, bool rowsSettled)
        => _driver == Orientation.Vertical ? region.Width : region.Declared.Height;
    }

    private sealed class ScanSizeRule : SizeRule
    {
      private readonly IRowScan _rows;

      public ScanSizeRule(IRowScan rows) => _rows = rows;

      public override bool Take(Plane<ISpace> region, int row) => _rows.IncludesRow(region, row);

      public override int? Width(Plane<ISpace> region, int rows, bool rowsSettled) => region.Width;
    }

    private sealed class InterleavedSizeRule : SizeRule
    {
      private readonly IRowScan _rows;
      private readonly IRowMajorColumnStrategy _columns;
      private IColumnAccumulator? _accumulator;
      private bool _stopped;

      public InterleavedSizeRule(IRowScan rows, IRowMajorColumnStrategy columns)
      {
        _rows = rows;
        _columns = columns;
      }

      public override bool Take(Plane<ISpace> region, int row)
      {
        if (_stopped)
          return false;

        _accumulator ??= _columns.BeginColumns(region.Width);

        if (!_rows.IncludesRow(region, row))
        {
          _stopped = true;
          return false;
        }

        if (!_accumulator.IsSettled)
          _accumulator.Include(region, row);

        return true;
      }

      public override int? Width(Plane<ISpace> region, int rows, bool rowsSettled)
      {
        _accumulator ??= _columns.BeginColumns(region.Width);

        return _accumulator.IsSettled || _stopped || rowsSettled ? _accumulator.Count : (int?)null;
      }
    }

    private sealed class RowsThenColumnsRule : SizeRule
    {
      private readonly int? _count;
      private readonly IRowScan? _rows;
      private readonly IColumnStrategy _columns;
      private bool _stopped;

      public RowsThenColumnsRule(int? count, IRowScan? rows, IColumnStrategy columns)
      {
        _count = count;
        _rows = rows;
        _columns = columns;
      }

      public override bool Take(Plane<ISpace> region, int row)
      {
        if (_stopped)
          return false;

        var take = _count is int count ? row < count : _rows!.IncludesRow(region, row);

        if (!take)
          _stopped = true;

        return take;
      }

      public override int? Width(Plane<ISpace> region, int rows, bool rowsSettled)
      {
        var settled = rowsSettled || _stopped || (_count is int count && rows >= count);

        if (!settled)
          return null;

        return _columns.SelectColumns(region.Slice(new Area(region.Width, rows)));
      }

      public override bool Complete(int rows) => _count is not int count || rows == count;

      public override Size Declared => new Size(0, _count ?? 0);
    }
  }
}
