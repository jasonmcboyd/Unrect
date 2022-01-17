using System.Collections.Generic;

namespace Unrect.Core
{
  public interface IRegion<out TSpace>
  {
    ISpace<TSpace> Space { get; }
    IEnumerable<IRegion<TSpace>> GetSubregions();
  }
}
