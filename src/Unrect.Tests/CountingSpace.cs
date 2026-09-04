using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Tests
{
  /// <summary>
  /// A space that remembers what was read through it, so a test can assert on the reading itself
  /// rather than only on the answer.
  /// <para>
  /// Two claims need this and cannot be made any other way. A strategy that "settles early" is
  /// making a statement about cells it did NOT read, and an answer is the same either way. And a
  /// bound that is discovered as a projection consumes it is one whose rows are touched in step with
  /// the projection, which is a claim about <em>when</em>, not about what.
  /// </para>
  /// <para>
  /// Subspaces share the ledger and carry their origin, so a row is counted under the number the
  /// outermost space would call it — otherwise a nested read would be recorded against a coordinate
  /// system the assertion does not use.
  /// </para>
  /// </summary>
  internal sealed class CountingSpace : ISpace
  {
    public CountingSpace(ISpace inner)
      : this(inner, new Ledger(), 0)
    {
    }

    private CountingSpace(ISpace inner, Ledger reads, int rowOrigin)
    {
      Inner = inner;
      Reads = reads;
      RowOrigin = rowOrigin;
    }

    private ISpace Inner { get; }
    private Ledger Reads { get; }
    private int RowOrigin { get; }

    /// <summary>How many cells have been read through this space and every subspace of it.</summary>
    public int CellReads => Reads.Cells;

    /// <summary>How many distinct rows have been touched, numbered from the outermost space's origin.</summary>
    public int RowsTouched => Reads.Rows.Count;

    /// <inheritdoc/>
    public Area Area => Inner.Area;

    /// <inheritdoc/>
    public CellValue this[int column, int row]
    {
      get
      {
        Reads.Record(RowOrigin + row);

        return Inner[column, row];
      }
    }

    /// <inheritdoc/>
    public ISpace GetSubspace(Offset offset, Area area)
      => new CountingSpace(Inner.GetSubspace(offset, area), Reads, RowOrigin + offset.Height);

    private sealed class Ledger
    {
      public int Cells { get; private set; }

      public HashSet<int> Rows { get; } = new HashSet<int>();

      public void Record(int row)
      {
        Cells++;
        Rows.Add(row);
      }
    }
  }
}
