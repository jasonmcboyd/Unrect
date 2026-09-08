using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.Observations;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The law: <c>x.Under(a, b)</c> IS the vertical flow that reads <c>a</c>, then <c>b</c>, then
  /// <c>x</c> — and the pin says at which level. The docs state it as an unqualified equation
  /// ("sugar for a vertical flow and nothing else"); the implementation is that expansion plus two
  /// deliberate diagnostic choices, so the equation is true of values and consumption and false of
  /// paths.
  /// <para>
  /// <strong>The law holds at L2 and first fails at L3</strong>, in exactly two ways, both
  /// deliberate and both pinned below with their direction: the sugar's flow describes itself as
  /// <c>Under</c> rather than <c>VerticalFlow</c>, and it passes <c>declared: null</c> to every
  /// <c>Next</c> so no identifier from inside the helper reaches a user's diagnostics — where a
  /// hand-written expansion borrows the locals the user wrote.
  /// </para>
  /// <para>
  /// <see cref="UnderTests"/> pins the consequences of the desugaring one property at a time; this
  /// suite pins the equation itself, which is what the property tests could never say. Evidence
  /// trail: SRC-17, and the v0.4 pinning backlog's item 2.
  /// </para>
  /// </summary>
  public class UnderExpansionLawTests
  {
    // A junk row, a caption, two data rows.
    private static ISpace Sheet() => Mixed(new object?[,]
    {
      { "junk", null },
      { "Detail", null },
      { "a", 1 },
      { "b", 2 },
    });

    // Two captions stacked, so the multi-caption arm of the equation has somewhere to run.
    private static ISpace TwoCaptionSheet() => Mixed(new object?[,]
    {
      { "Cap1" },
      { "Cap2" },
      { "a" },
      { "b" },
    });

    private static IProjection<int> Lines() => Range(b => b.Height);

    /// <summary>
    /// The right-hand side of the equation, written as the docs write it. The locals are named
    /// <c>caption</c> and <c>section</c> on purpose: a hand-written expansion has identifiers, and
    /// the naming ladder borrows them — which is the second of the two L3 differences and is pinned
    /// as such below.
    /// </summary>
    private static IProjection<int> Expansion(IProjection<int> section, params IProjection<string>[] captions)
      => VerticalFlow(v =>
      {
        foreach (var caption in captions)
          v.Next(caption);

        return v.Next(section);
      });

    // --- L2: the equation itself -------------------------------------------------------------------

    [Fact]
    public void TheSugarAndItsExpansionProjectAndConsumeIdentically()
    {
      // Value, offset, consumed extent, advance — everything a parent could see of either spelling.
      var sugar = Lines().Under(Caption("Detail"));
      var expansion = Expansion(Lines(), Caption("Detail"));

      AssertL2(Observe(expansion, Sheet()), Observe(sugar, Sheet()));
    }

    [Fact]
    public void TheEquationHoldsForMoreThanOneCaption()
    {
      // The captions are a params array on both sides, and the flow reads them in declaration order
      // with each seeking from where the last left off.
      var sugar = Lines().Under(Caption("Cap1"), Caption("Cap2"));
      var expansion = Expansion(Lines(), Caption("Cap1"), Caption("Cap2"));

      AssertL2(Observe(expansion, TwoCaptionSheet()), Observe(sugar, TwoCaptionSheet()));
    }

    [Fact]
    public void AMissingCaptionIsTheSameFailureInBothSpellings()
    {
      // Same problem, same cell, same fault classification — the failure's L1 identity, which is
      // what a tolerance boundary above either spelling would have to act on. The path is the one
      // thing that differs, and it is pinned separately below.
      var sugar = Lines().Under(Caption("Nope"));
      var expansion = Expansion(Lines(), Caption("Nope"));

      AssertL2(Observe(expansion, Sheet()), Observe(sugar, Sheet()));
    }

    [Fact]
    public void ADiagnosticRaisedInsideTheSectionDoesNotMoveTheEquation()
    {
      // The section absorbs a kind mismatch, so both spellings produce a Warning and carry on. What
      // is under test is that tolerating it consumed the same space either way.
      var sugar = Tolerated().Under(Caption("Detail"));
      var expansion = Expansion(Tolerated(), Caption("Detail"));

      AssertL2(Observe(expansion, Sheet()), Observe(sugar, Sheet()));
    }

    // --- L3: the two differences, each pinned in its own direction ---------------------------------

    [Fact]
    public void TheSugarSaysUnderWhereTheExpansionSaysVerticalFlow()
    {
      // A path segment should be greppable back to the line that produced it, and the line says
      // .Under. Both directions asserted: neither spelling may quietly start rendering as the other.
      var sugar = Assert.Throws<ProjectionException>(() => Lines().Under(Caption("Nope")).Map(Sheet()));
      var expansion = Assert.Throws<ProjectionException>(() => Expansion(Lines(), Caption("Nope")).Map(Sheet()));

      Assert.StartsWith("Under -> ", sugar.Path);
      Assert.DoesNotContain("VerticalFlow", sugar.Path);

      Assert.StartsWith("VerticalFlow -> ", expansion.Path);
      Assert.DoesNotContain("Under", expansion.Path);
    }

    [Fact]
    public void TheSugarSuppressesUseSiteCaptureWhereTheExpansionBorrowsItsLocals()
    {
      // The mandatory half of the divergence. Left to the compiler, the ladder would read the
      // argument text from inside the helper and label every caption in the codebase 'caption'; the
      // sugar therefore opts out and falls to rung 3, the description and the ordinal. A
      // hand-written flow has no such problem and keeps rung 2.
      var sugar = Assert.Throws<ProjectionException>(() => Lines().Under(Caption("Nope")).Map(Sheet()));
      var expansion = Assert.Throws<ProjectionException>(() => Expansion(Lines(), Caption("Nope")).Map(Sheet()));

      Assert.Equal("Caption(\"Nope\")#1", sugar.Subject);
      Assert.DoesNotContain("'caption'", sugar.Path);

      Assert.Equal("'caption'", expansion.Subject);
      Assert.Contains("'caption'", expansion.Path);
    }

    [Fact]
    public void TheSectionItselfIsLabelledByTheSameTwoRungs()
    {
      // The other end of the same choice: the projection the section is for falls to its ordinal in
      // the sugar and borrows the expansion's local in the hand-written flow.
      var sugar = Assert.Throws<ProjectionException>(() =>
        Cell(c => c.GetInt()).Under(Caption("Detail")).Map(Sheet()));

      var expansion = Assert.Throws<ProjectionException>(() =>
        Expansion(Cell(c => c.GetInt()), Caption("Detail")).Map(Sheet()));

      Assert.Equal("Cell#2", sugar.Subject);
      Assert.Equal("'section'", expansion.Subject);
    }

    [Fact]
    public void NothingElseAboutADiagnosticMoves()
    {
      // The law bounded from above as well as below: strip the subject and the path — the two things
      // the sugar deliberately changes — and the diagnostics of the two spellings are the same list
      // in the same order, severity, cell and sentence alike. Without this the L3 negatives above
      // would leave room for a third, unnoticed divergence.
      var sugar = Tolerated().Under(Caption("Detail")).MapWithDiagnostics(Sheet());
      var expansion = Expansion(Tolerated(), Caption("Detail")).MapWithDiagnostics(Sheet());

      Assert.NotEmpty(sugar.Diagnostics);
      Assert.Equal(WithoutLabels(expansion.Diagnostics), WithoutLabels(sugar.Diagnostics));
    }

    /// <summary>A section that raises a Warning of its own rather than failing the parse.</summary>
    private static IProjection<int> Tolerated() => Cell(c => c.GetInt()).Optional();

    /// <summary>A diagnostic with everything but its subject and path — the L3 residue the law keeps.</summary>
    private static IReadOnlyList<string> WithoutLabels(IReadOnlyList<ProjectionDiagnostic> diagnostics)
      => diagnostics.Select(d => $"{d.Severity} at {d.Location.A1}: {d.Message}").ToList();
  }
}
