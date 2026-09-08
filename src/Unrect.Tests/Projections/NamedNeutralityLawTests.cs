using System.Collections.Generic;
using System.Linq;

using Unrect.Projections;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.Observations;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The law: <c>p.Named("n")</c> reads the same cells, projects the same value and consumes the
  /// same extent as <c>p</c>. Naming is allowed to change what a message calls a projection and
  /// nothing else — it is a clone modifier, one field of a copy of the receiver's own type.
  /// <para>
  /// <strong>The law holds at L2 and is deliberately false at L3</strong>, which is the whole point
  /// of the operator: the name is exactly the L3 difference, and the positive pins at the bottom
  /// assert where it lands.
  /// </para>
  /// <para>
  /// <strong>Where the failure identity was split, and why.</strong> A failure has two halves. What
  /// went wrong — the problem sentence, the cell, whether it was a fault, what it wrapped — is what
  /// a tolerance boundary acts on and what a caller catches; that is the outcome, so it lives at L1
  /// (<see cref="Observation.Failure"/>). Who it happened to — the subject and the path — is what a
  /// user reads; that is presentation, so it lives at L3. Without that split this law would be
  /// unstatable: <c>.Named</c> changes the subject and the path of every failure it wraps, so an L1
  /// facet that included them would report the operator's whole purpose as a breach of its
  /// neutrality.
  /// </para>
  /// <para>
  /// Evidence trail: SRC-18 (clone modifiers), TEST-62 (ordinals survive naming), and the v0.4
  /// pinning backlog's item 3, which asks for exactly this bound — value/consumption neutrality
  /// with the L3 non-neutrality retained.
  /// </para>
  /// </summary>
  public class NamedNeutralityLawTests
  {
    // --- L2: naming changes nothing a parent can see ------------------------------------------------

    [Fact]
    public void NamingALeafChangesNothingItProjects()
    {
      var leaf = IntCell();

      AssertL2(Observe(leaf, Ladder()), Observe(leaf.Named("total"), Ladder()));
    }

    [Fact]
    public void NamingALayoutChangesNothingItProjects()
    {
      // A composite's extent is derived from its children, so a name that reached the derivation
      // would show up here as a different consumed extent rather than as a different word.
      var flow = VerticalFlow(v => v.Next(IntCell()) + v.Next(IntCell()));

      AssertL2(Observe(flow, Ladder()), Observe(flow.Named("header"), Ladder()));
    }

    [Fact]
    public void NamingARepeatsItemChangesNothingTheRepeatCollects()
    {
      // The item is re-placed once per occurrence, so a modifier that disturbed its placement would
      // change how many occurrences the repeat found — the loudest possible way for this law to
      // fail, and the reason the item is worth a case of its own.
      var space = Mixed(new object?[,] { { "a" }, { null }, { "b" }, { null }, { "c" } });
      var item = Text();

      var plain = VerticalRepeat(item, separatedBy: BlankRows());
      var named = VerticalRepeat(item.Named("detail"), separatedBy: BlankRows());

      AssertL2(Observe(plain, space), Observe(named, space));
    }

    [Fact]
    public void NamingAFailingProjectionLeavesTheFailureItself()
    {
      // The L1 half: same problem, same cell, same fault flag, same inner exception. Only the two
      // labels move, and they are asserted below.
      var failing = Cell(c => c.GetString());

      AssertL2(Observe(failing, Ladder()), Observe(failing.Named("client"), Ladder()));
    }

    [Fact]
    public void NamingChangesTheSubjectAndThePathOfADiagnosticAndNothingElse()
    {
      // The law bounded from above: strip the two labels and the diagnostics of the two spellings
      // are the same list in the same order — same severity, same cell, same sentence.
      var leaf = IntCell();

      var plain = leaf.MapWithDiagnostics(Ladder());
      var named = leaf.Named("total").MapWithDiagnostics(Ladder());

      Assert.NotEmpty(plain.Diagnostics);
      Assert.Equal(WithoutLabels(plain.Diagnostics), WithoutLabels(named.Diagnostics));
    }

    // --- L3: the difference the operator exists to make ---------------------------------------------

    [Fact]
    public void TheNameLandsOnItsOwnSegmentAndReachesNoOther()
    {
      // Rung 1 of the naming ladder, at the segment the named projection owns: the enclosing flow
      // keeps its own description, and the name renders with the projection's kind beside it so a
      // reader can still tell what was being read.
      var summary = Text();

      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(summary.Named("summary"))}").Map(Ladder()));

      Assert.Equal("'summary'", failure.Subject);
      Assert.Equal("VerticalFlow -> 'summary' (Text)", failure.Path);
    }

    [Fact]
    public void NamingOneChildDoesNotRenumberItsSiblings()
    {
      // Ordinals count every child, named or not. Were they to count only the unnamed ones, adding a
      // name to one line would silently change what every path below it meant.
      var second = IntCell();

      var plain = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(second)}{v.Next(Text())}").Map(Ladder()));

      var named = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(second.Named("middle"))}{v.Next(Text())}").Map(Ladder()));

      Assert.Equal("Text#3", plain.Subject);
      Assert.Equal("Text#3", named.Subject);
    }

    /// <summary>A diagnostic with everything but its subject and path — the residue naming may not touch.</summary>
    private static IReadOnlyList<string> WithoutLabels(IReadOnlyList<ProjectionDiagnostic> diagnostics)
      => diagnostics.Select(d => $"{d.Severity} at {d.Location.A1}: {d.Message}").ToList();
  }
}
