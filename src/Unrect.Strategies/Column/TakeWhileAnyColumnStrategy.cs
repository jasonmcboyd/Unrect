using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// The leading columns in which some cell satisfies the predicate. Asked column by column it
  /// reads the column; asked a row at a time it accumulates, and settles once every column has
  /// matched.
  /// </summary>
  internal sealed class TakeWhileAnyColumnStrategy : IRowMajorColumnStrategy
  {
    public TakeWhileAnyColumnStrategy(Func<Point<ISpace>, bool> predicate, bool afterLead = false)
    {
      Predicate = predicate;
      AfterLead = afterLead;
    }

    /// <summary>
    /// Whether the run may begin after a lead: the leading columns whose cell in the FIRST row does
    /// not satisfy the predicate are taken too. The first row alone decides the lead, so a value
    /// further down one of those columns cannot move where the run begins.
    /// </summary>
    internal bool AfterLead { get; }

    internal Func<Point<ISpace>, bool> Predicate { get; }

    public IColumnAccumulator BeginColumns(int width) => new Accumulator(Predicate, width, AfterLead);

    public IColumnScan Begin() => new Scanning.RowMajorColumns(this, (space, column) =>
    {
      if (AfterLead && space.HasRow(0) && InLead(space, column))
        return true;

      for (var row = 0; space.HasRow(row); row++)
        if (Predicate(space[column, row]))
          return true;

      return false;
    });

    private bool InLead(Plane<ISpace> space, int column)
    {
      for (var before = 0; before <= column; before++)
        if (Predicate(space[before, 0]))
          return false;

      return true;
    }

    private sealed class Accumulator : IColumnAccumulator
    {
      private readonly bool[] _matched;

      private readonly bool _afterLead;

      public Accumulator(Func<Point<ISpace>, bool> predicate, int width, bool afterLead)
      {
        Predicate = predicate;
        _matched = new bool[width];
        _afterLead = afterLead;
      }

      public int Count { get; private set; }

      public bool IsSettled => Count == _matched.Length;

      private Func<Point<ISpace>, bool> Predicate { get; }

      public void Include(Plane<ISpace> space, int row)
      {
        // The lead is settled by the first row and counts as matched from then on.
        if (_afterLead && row == 0)
          for (var column = 0; column < _matched.Length && !Predicate(space[column, 0]); column++)
            _matched[column] = true;

        for (var column = Count; column < _matched.Length; column++)
        {
          if (!_matched[column] && Predicate(space[column, row]))
            _matched[column] = true;
        }

        while (Count < _matched.Length && _matched[Count])
          Count++;
      }
    }
  }
}
