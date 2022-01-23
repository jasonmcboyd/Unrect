using System;

namespace Unrect.Core
{
  public interface IColumnSelectionStrategy<in TSpace>
  {
    uint SelectColumns(ISpace<TSpace> space);
  }
}
