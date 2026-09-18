using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class ExplicitColumnCountStrategy : IColumnStrategy
  {
    public ExplicitColumnCountStrategy(int count)
    {
      if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

      Count = count;
    }

    internal int Count { get; }

    public int SelectColumns(Plane<ISpace> space)
      => Count <= space.Width ? Count : throw new OutOfBoundsException();
  }
}
