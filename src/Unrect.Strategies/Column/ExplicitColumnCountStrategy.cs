using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal class ExplicitColumnCountStrategy : IColumnStrategy
  {
    public ExplicitColumnCountStrategy(int count)
    {
      if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

      Count = count;
    }

    private int Count { get; }

    public int SelectColumns(ISpace space)
      => Count <= space.Area.Size.Width ? Count : throw new OutOfBoundsException();
  }
}
