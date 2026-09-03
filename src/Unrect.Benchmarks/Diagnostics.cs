using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

using Unrect.Core;
using Unrect.Shapes;

using static Unrect.Shapes.Shape;

namespace Unrect.Benchmarks
{
  /// <summary>
  /// What observability and tolerance cost. Four questions, all of them about paths not taken on a
  /// clean parse:
  ///
  /// <list type="bullet">
  ///   <item><b>The diagnostic channel.</b> <c>MapWithDiagnostics</c> against <c>Map</c> on the same
  ///     document, in the same run: the difference is the price of collecting, and it is the pair
  ///     that has to stay on one machine to mean anything.</item>
  ///   <item><b>Rollback.</b> A <c>Choice</c> whose first alternative loses does the work, discards
  ///     it, and starts over -- the cost of an alternative that was tried and abandoned.</item>
  ///   <item><b>Absorption.</b> <c>Optional</c> around a section that is not there. The failure is
  ///     still constructed with its full path and location before it is swallowed, so this measures
  ///     what tolerance actually costs when it triggers.</item>
  ///   <item><b>Rendering.</b> A failure allowed to escape, with its message and path materialized.
  ///     A library's error path is a feature; this is the row that stops it quietly becoming
  ///     expensive.</item>
  /// </list>
  ///
  /// <para>All four run against the same document the EndToEnd family parses, so the clean-parse
  /// baseline in this leg is directly comparable to the failure rows beside it.</para>
  /// </summary>
  [MemoryDiagnoser]
  [BenchmarkCategory("Diagnostics")]
  public class Diagnostics
  {
    private static readonly IShape<int> Section = Range(RowsWhileAnyValue(), b => b.Height);

    // The loser goes first: a caption that is not in the document, so the choice pays for a full
    // failed attempt before the second alternative succeeds.
    private static readonly IShape<int> FirstAlternativeLoses = Choice(
      Section.Under(Caption("No Such Caption Exists Here")),
      Section.Under(Caption(CanonicalSpaces.DetailsCaption)));

    private static readonly IShape<int> AbsorbedFailure =
      Section.Under(Caption("No Such Caption Exists Here")).Optional();

    private ISpace _document = default!;

    [GlobalSetup]
    public void Setup() => _document = CanonicalSpaces.SmallDocument;

    /// <summary>The baseline the next row is measured against. Same shape, same document.</summary>
    [Benchmark(Baseline = true)]
    public int Map_Plain() => IrrReport.Shape.Map(_document).Summary.Count;

    /// <summary>The same parse with the diagnostic channel collecting.</summary>
    [Benchmark]
    public int Map_WithDiagnostics()
    {
      var result = IrrReport.Shape.MapWithDiagnostics(_document);

      return result.Value.Summary.Count + result.Diagnostics.Count;
    }

    /// <summary>A first alternative that does the work, fails, and is rolled back.</summary>
    [Benchmark]
    public int Choice_FirstAlternativeLoses() => FirstAlternativeLoses.Map(_document);

    /// <summary>A failure built in full -- path, location, message -- and then swallowed.</summary>
    [Benchmark]
    public int Optional_AbsorbsFailure() => AbsorbedFailure.Map(_document);

    /// <summary>
    /// The escaping failure, rendered. Returns the message length so the render survives dead-code
    /// elimination -- an unread exception would measure the throw and none of the formatting.
    /// <para>
    /// Read it against <c>Map_Plain</c>, not on its own: the failure is the report's LAST child, so
    /// this row parses the header, the summary and the first series before anything goes wrong. It
    /// measures a failing parse end to end, which is what a user actually waits for -- isolating the
    /// render alone would mean failing at the first child, and that would stop measuring the deep
    /// path that makes a path expensive to build.
    /// </para>
    /// </summary>
    [Benchmark]
    public int ShapeException_Render()
    {
      try
      {
        IrrReport.WithMissingSection.Map(_document);

        return 0;
      }
      catch (ShapeException failure)
      {
        return failure.Message.Length + failure.Path.Length + failure.Location.ToString().Length;
      }
    }
  }
}
