using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class ExplicitRowCountStrategy : IRowStrategy
  {
    public ExplicitRowCountStrategy(int count)
    {
      if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

      Count = count;
    }

    private int Count { get; }

    public int SelectRows(ISpace space)
      => Count <= space.Area.Height ? Count : throw new OutOfBoundsException();
  }
}
