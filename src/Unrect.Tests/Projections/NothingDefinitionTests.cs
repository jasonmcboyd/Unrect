using System;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.Observations;
using static Unrect.Tests.ProjectionTestSpaces;


namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The unit ε — <see cref="NothingDefinition{TSpace, T}"/> — and the laws that make a layout an
  /// algebra: a unit child is deletable from a flow at L3, with the two places the identity stops
  /// holding pinned by name; and a layout that declares nothing at all is refused where it is
  /// written rather than becoming a kind of nothing.
  /// </summary>
  public class NothingDefinitionTests
  {
    private static ICellSpace Numbers() => Ladder(3);


    /// <summary>The discovered extent: full width, and as many leading rows as hold anything.</summary>
    private static IProjectionDefinition<ICellSpace, int> Rows() => Range(RowsWhileAnyValue(), b => b.Height);

    /// <summary>A cell read as text — which is a failure over <see cref="Numbers"/>, and an absorbable one.</summary>
    private static IProjectionDefinition<ICellSpace, string> Title() => TextCell();

    /// <summary>The internal ε, at the one type these tests need it at.</summary>
    private static IProjectionDefinition<ICellSpace, int> Unit() => NothingDefinition<ICellSpace, int>.Instance;

    [Fact]
    public void ALayoutThatDeclaredNothingIsRefusedRatherThanReadingAsNothing()
    {
      // The table's last row, and the reason the join rule never has to answer for an empty child
      // list: "described nothing" is not a kind of nothing, it is an error in the declaration, and
      // it is refused where the declaration is written — before there is a space, a presence, or a
      // tolerance boundary to absorb it.
      var failure = Assert.Throws<InvalidOperationException>(() => VerticalFlow<int>(v =>  0));

      Assert.Equal("a flow must declare at least one projection; this one called Next zero times", failure.Message);
    }

    // --- The flow-unit law over ε -----------------------------------------------------------------

    [Fact]
    public void TheUnitAcceptsAnythingAndReadsNothing()
    {
      // ε's own denotation, which is what makes it deletable: it never fails and it occupies no
      // cells, so a repeat guard stops at it exactly as it stops at a region that was there and held
      // nothing.
      var applied = Unit().Apply(Numbers());

      Assert.Equal(0, applied.Value);
      Assert.Equal(0, applied.Consumed.Width);
      Assert.Equal(0, applied.Consumed.Height);

      // Nothing of its own is noticed. The one Info is the root's report of the space the
      // declaration did not describe, which is a statement about the sheet and not about ε.
      var note = Assert.Single(Unit().MapWithDiagnostics(Numbers()).Diagnostics);

      Assert.Equal(DiagnosticSeverity.Info, note.Severity);
      Assert.Contains("were not described", note.Message);
    }

    [Fact]
    public void TheUnitIsALeftIdentityForAFlow()
    {
      // ε · x ≡ x, and the pin is at L3: value, offset, consumed extent, advance, and the
      // diagnostics in order, subject and path included. The children are named by their use site,
      // which is why L3 reaches — see the negative pin below for the one thing the extra child does
      // move.
      var x = Rows();
      var plain = VerticalFlow(v =>
      {
        var x2 = v.Next(x);

        return x2;
      });
      var withUnit = VerticalFlow(v =>
      {
        v.Next(Unit());

        var x2 = v.Next(x);

        return x2;
      });

      AssertL3(Observe(plain, Numbers()), Observe(withUnit, Numbers()));

      // Again with a child that notices something, so the L3 half of the claim is not two empty
      // lists agreeing: the absorbed reading's warning is the same sentence about the same cell,
      // under the same path, on both sides of the equation.
      var tolerated = Title().Optional();

      AssertL3(
        Observe(VerticalFlow(v =>
        {
          var tolerated2 = v.Next(tolerated);

          return tolerated2;
        }), Numbers()),
        Observe(
          VerticalFlow(v =>
          {
            v.Next(Unit());

            var tolerated2 = v.Next(tolerated);

            return tolerated2;
          }),
          Numbers()));
    }

    [Fact]
    public void TheUnitIsARightIdentityForAFlow()
    {
      // x · ε ≡ x, the mirror. A unit taken after the cursor has moved slices the remainder it is
      // handed and adds nothing to the advance, so a following sibling — or an enclosing repeat —
      // steps by exactly what x consumed.
      var x = Rows();
      var plain = VerticalFlow(v =>
      {
        var x2 = v.Next(x);

        return x2;
      });
      var withUnit = VerticalFlow(v =>
      {
        var value = v.Next(x);

        v.Next(Unit());

        return value;
      });

      AssertL3(Observe(plain, Numbers()), Observe(withUnit, Numbers()));
    }

    [Fact]
    public void TheUnitShiftsAnInlineSiblingsOrdinalWhichIsWhereL3StopsHolding()
    {
      // The first of the two negative pins, and the reason the identity above is stated over
      // use-site-named children. A child written inline has no identifier to borrow and falls to the
      // naming ladder's last rung — its kind and its 1-based POSITION — which the extra child moves.
      var plain = VerticalFlow(v =>
      {
        var textCell = v.Next(TextCell());

        return textCell;
      });
      var withUnit = VerticalFlow(v =>
      {
        v.Next(Unit());

        var textCell = v.Next(TextCell());

        return textCell;
      });

      var one = Assert.Throws<ProjectionException>(() => plain.Map(Numbers()));
      var two = Assert.Throws<ProjectionException>(() => withUnit.Map(Numbers()));

      Assert.Equal("Text#1", one.Subject);
      Assert.Equal("Text#2", two.Subject);
    }

    [Fact]
    public void TheUnitIsNotAnIdentityForASiblingThatFailsWhereItStands()
    {
      // The second negative pin, and the sharper one: ε consumes nothing, so a child after it is a
      // child following a sibling that consumed nothing, and the flow's note fires. The note is
      // appended to the PROBLEM, so this divergence is at L1 — the level a tolerance boundary above
      // either spelling would act on — not at L3 where a naming difference would sit.
      var x = Title();
      var plain = VerticalFlow(v =>
      {
        var x2 = v.Next(x);

        return x2;
      });
      var withUnit = VerticalFlow(v =>
      {
        v.Next(Unit());

        var x2 = v.Next(x);

        return x2;
      });

      var one = Assert.Throws<ProjectionException>(() => plain.Map(Numbers()));
      var two = Assert.Throws<ProjectionException>(() => withUnit.Map(Numbers()));

      Assert.DoesNotContain("note:", one.Problem);
      Assert.EndsWith("; note: the preceding sibling consumed nothing at this position", two.Problem);
    }
  }
}
