using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

using Unrect.Core;
using Unrect.Shapes;

using static Unrect.Shapes.Shape;

namespace Unrect.Benchmarks
{
  /// <summary>The tabular fixture's row, bound by caption with nothing declared.</summary>
  public sealed record TabularRow(
    string Investor,
    decimal Contribution,
    decimal Distribution,
    decimal Fee,
    decimal EndBalance,
    double Irr);

  /// <summary>
  /// The three ways to read a table, at two sizes, over one fixture -- the ladder a user climbs:
  /// a projection lambda, a bound record, a dictionary. They belong in one family precisely because
  /// the interesting number is the ratio between them, and a ratio is only trustworthy when both
  /// rows ran on the same machine in the same run.
  ///
  /// <para>The construction row is the odd one and the deliberate one. <c>TableRows&lt;T&gt;()</c>
  /// resolves its members reflectively and compiles a materializer when the SHAPE is built, not per
  /// map -- a cost paid once per declaration and then never again. It is measured separately so
  /// that a change making binding cheaper per row at the cost of a slower declaration (or the
  /// reverse) is visible as the trade it is, rather than averaged into one number.</para>
  /// </summary>
  [MemoryDiagnoser]
  [BenchmarkCategory("Tables")]
  public class Tables
  {
    private static readonly IShape<IReadOnlyList<decimal>> Projected =
      TableRows(r => r["Contribution"].GetDecimal());

    private static readonly IShape<IReadOnlyList<TabularRow>> Bound = TableRows<TabularRow>();

    private static readonly IShape<IReadOnlyList<IReadOnlyDictionary<string, CellValue>>> Dictionaries =
      TableRows();

    private ISpace _large = default!;
    private ISpace _mega = default!;

    [GlobalSetup]
    public void Setup()
    {
      _large = CanonicalSpaces.LargeTabular;
      _mega = CanonicalSpaces.MegaTabular;
    }

    [Benchmark]
    public int Lambda_10k() => Projected.Map(_large).Count;

    [Benchmark]
    public int Lambda_100k() => Projected.Map(_mega).Count;

    [Benchmark]
    public int Bound_10k() => Bound.Map(_large).Count;

    [Benchmark]
    public int Bound_100k() => Bound.Map(_mega).Count;

    [Benchmark]
    public int Dictionary_10k() => Dictionaries.Map(_large).Count;

    [Benchmark]
    public int Dictionary_100k() => Dictionaries.Map(_mega).Count;

    /// <summary>
    /// Declaration only: member resolution, nullability reading and materializer compilation, with
    /// no grid in sight. Sub-millisecond by design -- it is the one row in the suite exempt from the
    /// noise floor, because inflating it would mean declaring the same shape a thousand times,
    /// which measures a loop rather than a declaration.
    /// </summary>
    [Benchmark]
    public object Bound_ShapeConstruction() => TableRows<TabularRow>();
  }
}
