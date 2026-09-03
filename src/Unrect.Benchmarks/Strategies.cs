using BenchmarkDotNet.Attributes;

using Unrect.Core;
using Unrect.Shapes;

using static Unrect.Shapes.Shape;

namespace Unrect.Benchmarks
{
  /// <summary>
  /// Boundary resolution: the scans that decide where a region starts and stops. Every row here is
  /// dominated by one strategy walking the grid, with the smallest possible shape wrapped around it
  /// so the scan is what gets measured.
  ///
  /// <para>The seek rows are the family's point. A content anchor is a linear scan, so its cost
  /// depends on how far in the answer is -- and the interesting case is the one that never finds
  /// it. That row is wrapped in <c>Optional</c> deliberately: the scan is the subject, and an
  /// unabsorbed miss would measure exception construction and path rendering instead (which the
  /// Diagnostics family measures on purpose).</para>
  /// </summary>
  [MemoryDiagnoser]
  [BenchmarkCategory("Strategies")]
  public class Strategies
  {
    private static readonly IShape<int> WholeHeight = Range(RowsWhileAnyValue(), b => b.Height);

    private static readonly IShape<int> Seek =
      Row(r => r.Count).After(To(RowContaining(CanonicalSpaces.Landmark)));

    // The miss: absorbed, so the row measures the full-grid scan and not the throw.
    private static readonly IShape<int> SeekMiss = Seek.Optional();

    private static readonly IShape<int> Bounded =
      Range(RowsWhileAnyValue(), b => b.Height).Until(RowContaining(CanonicalSpaces.Landmark));

    private static readonly IShape<int> SkipBlanks = Row(r => r.Count).After(BlankRows());

    private ISpace _dense = default!;
    private ISpace _sparse = default!;
    private ISpace _near = default!;
    private ISpace _far = default!;
    private ISpace _absent = default!;
    private ISpace _blankLed = default!;

    [GlobalSetup]
    public void Setup()
    {
      _dense = CanonicalSpaces.MegaDenseNumeric;
      _sparse = CanonicalSpaces.MegaSparse;
      _near = CanonicalSpaces.LandmarkNear;
      _far = CanonicalSpaces.LandmarkFar;
      _absent = CanonicalSpaces.LandmarkAbsent;
      _blankLed = CanonicalSpaces.BlankLed;
    }

    /// <summary>"Rows while any cell has a value" over a grid where the answer is every row.</summary>
    [Benchmark]
    public int RowsWhileAnyValue_FullHeight() => WholeHeight.Map(_dense);

    /// <summary>
    /// The same scan over the K-1 shape: same extent, same height, three quarters of the cells
    /// blank. Paired with the row above deliberately -- they differ only in blankness, so the
    /// difference between them is what the blankness predicate costs at scale, which is the one
    /// number that moves if the value model changes.
    /// </summary>
    [Benchmark]
    public int RowsWhileAnyValue_Sparse() => WholeHeight.Map(_sparse);

    /// <summary>A content seek answered a tenth of the way down.</summary>
    [Benchmark]
    public int Seek_HitAt10Percent() => Seek.Map(_near);

    /// <summary>The same seek, nine tenths of the way down: the scan is the cost.</summary>
    [Benchmark]
    public int Seek_HitAt90Percent() => Seek.Map(_far);

    /// <summary>The full-grid scan that finds nothing, absorbed so only the scan is measured.</summary>
    [Benchmark]
    public int Seek_MissWholeGrid() => SeekMiss.Map(_absent);

    /// <summary>An extent bounded by a landmark rather than by running out of values.</summary>
    [Benchmark]
    public int Until_BoundResolution() => Bounded.Map(_far);

    /// <summary>Skipping fifty thousand leading blank rows to reach the first content row.</summary>
    [Benchmark]
    public int BlankRows_Skip() => SkipBlanks.Map(_blankLed);
  }
}
