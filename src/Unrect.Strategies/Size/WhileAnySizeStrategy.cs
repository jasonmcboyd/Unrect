using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal class WhileAnySizeStrategy<TSpace> : ISizeStrategy<TSpace>
  {
    public WhileAnySizeStrategy(Func<TSpace, bool> predicate)
    {
      Predicate = predicate;
    }

    public Func<TSpace, bool> Predicate { get; set; }

    public Size GetSize(ISpace<TSpace> availableSpace)
    {
      uint width = 0;
      uint height = 0;



    }
  }
}
