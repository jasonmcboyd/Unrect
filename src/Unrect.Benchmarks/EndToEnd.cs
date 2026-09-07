using BenchmarkDotNet.Attributes;

using Unrect.Core;
using Unrect.Projections;

namespace Unrect.Benchmarks
{
  /// <summary>
  /// One realistic document, parsed complete, at two sizes -- the number a user would recognize as
  /// "how long does my report take". Everything the other families measure in isolation is in here
  /// at once: seeks, table binding, a repeat, two captioned sections and a landmark bound.
  ///
  /// <para>The two sizes are the family's point: a projection's cost should track the cells it
  /// reads, and two points on the same declaration are what would show it stopped doing so. They
  /// are the same document, not two documents -- only the investor count differs, by a factor of
  /// ten.</para>
  /// </summary>
  [MemoryDiagnoser]
  [BenchmarkCategory("EndToEnd")]
  public class EndToEnd
  {
    private ISpace _small = default!;
    private ISpace _large = default!;

    [GlobalSetup]
    public void Setup()
    {
      _small = CanonicalSpaces.SmallDocument;
      _large = CanonicalSpaces.LargeDocument;
    }

    [Benchmark]
    public int Document_400Investors() => IrrReport.Projection.Map(_small).Summary.Count;

    [Benchmark]
    public int Document_4000Investors() => IrrReport.Projection.Map(_large).Summary.Count;
  }
}
