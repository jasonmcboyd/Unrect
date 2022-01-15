using System.Collections.Immutable;

namespace Unrect.Core
{
  public interface IRegion<out TSpace>
  {
    ISpace<TSpace> Space { get; }
  }
}
