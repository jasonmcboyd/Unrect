using System.Collections.Generic;

namespace Unrect.Core
{
  public interface IRegion
  {
    ISpace Space { get; }
    IEnumerable<IRegion> GetSubregions();
  }
}
