using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Unrect.Core;

namespace Unrect
{
  public class SuperStackRegionBuilder<TSpace, TRegion> : StackRegionBuilderBase<TSpace, SuperRegion<TSpace, TRegion>>
    where TRegion : IRegion<TSpace>
  {
    public SuperStackRegionBuilder(
      Func<IRegionBuilder<TSpace, TRegion>> subregionBuilderFactory)
    {
      SubregionBuilderFactory = subregionBuilderFactory;
    }

    public Func<IRegionBuilder<TSpace, TRegion>> SubregionBuilderFactory { get; }

    protected override IEnumerable<IRegionBuilder<TSpace>> GetSubregionBuilders()
    {
      while (true)
        yield return SubregionBuilderFactory();
    }

    public override SuperRegion<TSpace, TRegion> Build(ISpace<TSpace> space)
    {
      throw new System.NotImplementedException();
    }
  }
}
