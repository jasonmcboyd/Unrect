using System;

namespace Unrect.Core
{
  public interface IRowSelectionStrategy<in TSpace>
  {
    uint SelectRows(ISpace<TSpace> space);
  }
}
