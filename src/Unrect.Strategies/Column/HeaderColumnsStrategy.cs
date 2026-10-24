using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// A headed table's columns: from the left edge through the last column that holds a cell in any
  /// of the header rows — the blank lead before the first caption, every column under a caption, and
  /// a blank header cell between two captions, but never a column to the right of the header's last
  /// cell, whatever its body holds. Read a row at a time it settles the moment the last header row is
  /// in, which is what lets a table narrower than its region stream.
  /// <para>
  /// The header is read across the top: this is the rule for a table whose records run down the
  /// rows. A header down a column is another rule.
  /// </para>
  /// </summary>
  internal sealed class HeaderColumnsStrategy<TSpace> : IRowMajorColumnStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public HeaderColumnsStrategy(int headerRows)
    {
      if (headerRows < 1)
        throw new ArgumentOutOfRangeException(nameof(headerRows), headerRows, "A header is at least one row.");

      HeaderRows = headerRows;
    }

    public Orientation Along => Orientation.Horizontal;

    internal int HeaderRows { get; }

    public IColumnAccumulator<TSpace> BeginColumns(int width) => new Accumulator(HeaderRows);

    // Asked about one column of a region it answers from the header rows of that region. Over a
    // whole region that is the rule above; over a growing prefix of columns it can see only the
    // header cells so far, so there a blank header cell ends the run where a later caption would
    // have carried it — the price of a header rule asked across the columns it is counting.
    public ILineScan<TSpace> Begin() => new Scanning.RowMajorColumns<TSpace>(this, (space, column) => column < Reach(space));

    private int Reach(Plane<TSpace> space)
    {
      var reach = 0;

      for (var row = 0; row < HeaderRows && space.HasRow(row); row++)
        for (var column = 0; column < space.Width; column++)
          if (!space[column, row].IsBlank())
            reach = Math.Max(reach, column + 1);

      return reach;
    }

    private sealed class Accumulator : IColumnAccumulator<TSpace>
    {
      private readonly int _headerRows;

      public Accumulator(int headerRows) => _headerRows = headerRows;

      public int Count { get; private set; }

      public bool IsSettled { get; private set; }

      public void Include(Plane<TSpace> space, int row)
      {
        if (row >= _headerRows)
          return;

        for (var column = Count; column < space.Width; column++)
          if (!space[column, row].IsBlank())
            Count = column + 1;

        if (row == _headerRows - 1)
          IsSettled = true;
      }
    }
  }
}
