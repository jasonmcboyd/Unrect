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

    private Func<TSpace, bool> Predicate { get; }

    public Size GetSize(ISpace<TSpace> availableSpace)
    {
      // Pass 1: take rows while any cell in the row matches
      int height = 0;
      while (height < availableSpace.Area.Size.Height)
      {
        bool anyMatch = false;
        for (int col = 0; col < availableSpace.Area.Size.Width; col++)
        {
          if (Predicate(availableSpace[col, height]))
          {
            anyMatch = true;
            break;
          }
        }
        if (!anyMatch)
          break;
        height++;
      }

      // Pass 2: within those rows, take columns while any cell matches
      int width = 0;
      while (width < availableSpace.Area.Size.Width)
      {
        bool anyMatch = false;
        for (int row = 0; row < height; row++)
        {
          if (Predicate(availableSpace[width, row]))
          {
            anyMatch = true;
            break;
          }
        }
        if (!anyMatch)
          break;
        width++;
      }

      return new Size(width, height);
    }
  }
}
