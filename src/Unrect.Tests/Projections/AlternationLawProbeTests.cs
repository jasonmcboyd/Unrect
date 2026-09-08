using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.Observations;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The open questions of v0.4 §14.2 (associativity of <c>Choice</c> and <c>Else</c>) and §14.3
  /// (idempotence of <c>Optional</c>), answered by experiment and pinned at the level each answer
  /// turned out to hold at. Nothing here was known before it was measured; the test names ARE the
  /// findings.
  /// <para>
  /// <strong>Choice is associative at L2 and not at L3, and not at L1 when nothing matches.</strong>
  /// All three parenthesizations choose the same alternative and consume the same extent, and where
  /// the first alternative wins they are identical to the last diagnostic. Once an alternative is
  /// passed over they part company three ways: the flat form reports one <c>Info</c> per loser;
  /// right-nesting reports the same number but RENUMBERS the inner ones from 1 again and deepens
  /// their paths; left-nesting reports ONE <c>Info</c>, because the outer choice rolls the inner
  /// one's near misses back and replaces them with its aggregate. When every alternative rejects the
  /// three aggregates name different rosters, so the failure differs at L1.
  /// </para>
  /// <para>
  /// <strong>Else is associative until a second boundary is exercised.</strong> Zero or one
  /// exercised boundary: identical at L3. Two: right-nesting warns once per boundary (two Warnings,
  /// each naming its own failed arm), left-nesting warns once (the outer boundary rolls the inner
  /// one's Warning back and reports the chain as a note on the stand-in). When every arm fails the
  /// two agree again, note chain and all.
  /// </para>
  /// <para>
  /// <strong>Optional is idempotent at L3</strong> — value, consumption and the single Warning are
  /// identical on success and on absorbed failure alike, because the second boundary sees the first
  /// one succeed. It differs only in the declaration tree, which is L0. There is no nullability
  /// question to answer: <c>T?</c> on an unconstrained <c>T</c> is an annotation, so
  /// <c>Optional()</c> on a projection of <c>int</c> hands back a projection of <c>int</c> and there
  /// is no <c>T??</c> for a second one to collapse.
  /// </para>
  /// <para>
  /// <strong>Reported, and deliberately not pinned:</strong> a nested aggregate is spliced into its
  /// parent's tally at the SAME indent as the parent's own alternatives, and the parent's " at
  /// {location}" suffix lands after the nested block's last line. A reader of
  /// <c>Choice(a, Choice(b, c))</c>'s total failure therefore sees "alternative 1, alternative 2,
  /// alternative 1, alternative 2" flat, with a doubled trailing location. That is a rendering
  /// defect rather than a semantic one, so the assertions below name which alternatives an aggregate
  /// lists and never how the list is laid out.
  /// </para>
  /// </summary>
  public class AlternationLawProbeTests
  {
    /// <summary>One text cell — enough for an arm to agree or disagree about, and nothing else.</summary>
    private static ISpace Sheet() => Mixed(new object?[,] { { "x" } });

    /// <summary>An arm that reads the cell and marks its answer with its own name.</summary>
    private static IProjection<string> Accepts(string name) => Cell(c => $"{c.GetString()}-{name}").Named(name);

    /// <summary>
    /// An arm that asks the same cell for a number, which it is not. A disagreement with the data,
    /// never a fault, so every tolerance operator here is being asked the question it exists for.
    /// </summary>
    private static IProjection<string> Rejects(string name) => Cell(c => c.GetInt().ToString()).Named(name);

    // --- Choice associativity: where it holds ------------------------------------------------------

    [Fact]
    public void ChoiceReassociationIsInvisibleWhenTheFirstAlternativeAccepts()
    {
      // Alternation that goes right the first time reports nothing, so there is nothing for the
      // parenthesization to differ about. The strongest form of the law, and the only scenario in
      // which it reaches L3.
      var a = Accepts("a");
      var b = Accepts("b");
      var c = Accepts("c");

      var flat = Observe(Choice(a, b, c), Sheet());

      AssertL3(flat, Observe(Choice(a, Choice(b, c)), Sheet()));
      AssertL3(flat, Observe(Choice(Choice(a, b), c), Sheet()));
    }

    [Fact]
    public void ChoiceReassociationKeepsTheReadingWhenALaterAlternativeWins()
    {
      // L2 is where the law lives: same winner, same value, same extent consumed from the same
      // place. Everything that follows is about what the three spellings SAY.
      var a = Rejects("a");
      var b = Rejects("b");
      var c = Accepts("c");

      var flat = Observe(Choice(a, b, c), Sheet());

      AssertL2(flat, Observe(Choice(a, Choice(b, c)), Sheet()));
      AssertL2(flat, Observe(Choice(Choice(a, b), c), Sheet()));
    }

    // --- Choice associativity: where it stops ------------------------------------------------------

    [Fact]
    public void RightNestingRenumbersThePassedOverAlternativeAndDeepensItsPath()
    {
      // Both spellings pass over two alternatives and say so twice, and the first Info is word for
      // word the same. The second is not: an inner choice counts its own alternatives from 1, so the
      // arm a reader wrote third is reported as "alternative 1" — and its path gains the segment of
      // a composite the reader wrote only to group it.
      var a = Rejects("a");
      var b = Rejects("b");
      var c = Accepts("c");

      var flat = Choice(a, b, c).MapWithDiagnostics(Sheet()).Diagnostics;
      var right = Choice(a, Choice(b, c)).MapWithDiagnostics(Sheet()).Diagnostics;

      Assert.Equal(2, flat.Count);
      Assert.Equal(2, right.Count);

      Assert.Equal(flat[0].Message, right[0].Message);
      Assert.Equal(flat[0].Path, right[0].Path);

      Assert.StartsWith("alternative 2 ('b') did not match: ", flat[1].Message);
      Assert.Equal("Choice -> 'b' (Cell)", flat[1].Path);

      Assert.StartsWith("alternative 1 ('b') did not match: ", right[1].Message);
      Assert.Equal("Choice -> Choice -> 'b' (Cell)", right[1].Path);
    }

    [Fact]
    public void LeftNestingCollapsesTwoNearMissesIntoOneAggregateInfo()
    {
      // The sharper difference, and the more expensive one. A losing alternative's diagnostics are
      // rolled back, and an inner choice's near misses are its diagnostics — so the outer choice
      // replaces both with one Info carrying the inner aggregate. The names survive inside the
      // message; the two paths that pointed AT the failing leaves do not.
      var a = Rejects("a");
      var b = Rejects("b");
      var c = Accepts("c");

      var flat = Choice(a, b, c).MapWithDiagnostics(Sheet()).Diagnostics;
      var left = Choice(Choice(a, b), c).MapWithDiagnostics(Sheet()).Diagnostics;

      Assert.Equal(2, flat.Count);
      Assert.Equal("Choice -> 'a' (Cell)", flat[0].Path);
      Assert.Equal("Choice -> 'b' (Cell)", flat[1].Path);

      var folded = Assert.Single(left);

      Assert.Equal(DiagnosticSeverity.Info, folded.Severity);
      Assert.Equal("Choice", folded.Subject);
      Assert.Equal("Choice -> Choice", folded.Path);
      Assert.StartsWith("alternative 1 (Choice) did not match: no alternative matched; ", folded.Message);
      Assert.Contains("alternative 1 ('a'): ", folded.Message);
      Assert.Contains("alternative 2 ('b'): ", folded.Message);
    }

    [Fact]
    public void ReassociationChangesTheAggregateWhenNoAlternativeMatches()
    {
      // L1 fails here, which is the answer §14.2 asked for. The three failures agree on everything
      // an exception is addressed by — subject, path, cell — and disagree on the roster the
      // aggregate lists, which is the failure's own problem text and therefore its identity.
      var a = Rejects("a");
      var b = Rejects("b");
      var c = Rejects("c");

      var flat = Assert.Throws<ProjectionException>(() => Choice(a, b, c).Map(Sheet()));
      var right = Assert.Throws<ProjectionException>(() => Choice(a, Choice(b, c)).Map(Sheet()));
      var left = Assert.Throws<ProjectionException>(() => Choice(Choice(a, b), c).Map(Sheet()));

      Assert.All(new[] { flat, right, left }, failure =>
      {
        Assert.Equal("Choice", failure.Subject);
        Assert.Equal("Choice", failure.Path);
        Assert.Equal("A1", failure.Location.A1);
      });

      // Three alternatives, each named for itself.
      Assert.Contains("alternative 3 ('c'): ", flat.Message);
      Assert.DoesNotContain("(Choice)", flat.Message);

      // Two, the second of which is a composite the reader has to open to find 'b' and 'c'.
      Assert.DoesNotContain("alternative 3", right.Message);
      Assert.Contains("alternative 2 (Choice): no alternative matched", right.Message);

      // Two, the first of which is that composite.
      Assert.DoesNotContain("alternative 3", left.Message);
      Assert.Contains("alternative 1 (Choice): no alternative matched", left.Message);
    }

    [Fact]
    public void RightNestingChangesWhichRejectionIsRetainedAsTheInnerFailure()
    {
      // "Retains the last rejection as its inner failure" is not stable under reassociation: the
      // last rejection of a right-nested choice is an aggregate, so a caller unwrapping
      // InnerException to reach the cell that disagreed finds another summary instead. Left-nesting
      // keeps the leaf, because its last alternative is still a leaf.
      var a = Rejects("a");
      var b = Rejects("b");
      var c = Rejects("c");

      var flat = Assert.Throws<ProjectionException>(() => Choice(a, b, c).Map(Sheet()));
      var right = Assert.Throws<ProjectionException>(() => Choice(a, Choice(b, c)).Map(Sheet()));
      var left = Assert.Throws<ProjectionException>(() => Choice(Choice(a, b), c).Map(Sheet()));

      Assert.Equal("Choice -> 'c' (Cell)", Assert.IsType<ProjectionException>(flat.InnerException).Path);
      Assert.Equal("Choice -> 'c' (Cell)", Assert.IsType<ProjectionException>(left.InnerException).Path);

      var retained = Assert.IsType<ProjectionException>(right.InnerException);
      Assert.Equal("Choice -> Choice", retained.Path);
      Assert.Equal("Choice", retained.Subject);
    }

    // --- Else associativity -----------------------------------------------------------------------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ElseReassociationIsInvisibleWhileAtMostOneBoundaryIsExercised(bool primaryAccepts)
    {
      // Nothing absorbed, or one thing absorbed: the two nestings are the same declaration down to
      // the Warning's subject and path. The second fallback is never reached, so where it was
      // written cannot matter.
      var x = primaryAccepts ? Accepts("x") : Rejects("x");
      var y = Accepts("y");
      var z = Accepts("z");

      AssertL3(Observe(x.Else(y).Else(z), Sheet()), Observe(x.Else(y.Else(z)), Sheet()));
    }

    [Fact]
    public void NestingElseRightWarnsPerBoundaryWhereNestingLeftFoldsTheChainIntoANote()
    {
      // Two boundaries exercised, and the reason they diverge is the same rollback that reshapes a
      // nested Choice: the outer boundary discards what its inner attempt tolerated. Right-nested,
      // the inner boundary is the FALLBACK and its Warning is part of the successful reading, so
      // both survive. The reading is identical either way — only the account of it differs.
      var x = Rejects("x");
      var y = Rejects("y");
      var z = Accepts("z");

      AssertL2(Observe(x.Else(y).Else(z), Sheet()), Observe(x.Else(y.Else(z)), Sheet()));

      var folded = Assert.Single(x.Else(y).Else(z).MapWithDiagnostics(Sheet()).Diagnostics);

      Assert.Equal(DiagnosticSeverity.Warning, folded.Severity);
      Assert.Equal("'y'", folded.Subject);
      Assert.Contains("it stands in for 'x', which failed too: ", folded.Message);

      var perBoundary = x.Else(y.Else(z)).MapWithDiagnostics(Sheet()).Diagnostics;

      Assert.Equal(2, perBoundary.Count);
      Assert.Equal("'x'", perBoundary[0].Subject);
      Assert.Equal("'y'", perBoundary[1].Subject);
      Assert.All(perBoundary, diagnostic =>
      {
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.DoesNotContain("stands in for", diagnostic.Message);
      });
    }

    [Fact]
    public void ElseReassociationAgreesEvenOnTheNoteChainWhenEveryArmFails()
    {
      // The scenario where a difference would have been most expected — every boundary exercised and
      // none of them helping — is the one where the two nestings are indistinguishable. Both blame
      // the last stand-in and carry the same chain in the same order, because a note is written as
      // the failure passes each boundary outwards and the boundaries are passed in the same order.
      var x = Rejects("x");
      var y = Rejects("y");
      var z = Rejects("z");

      AssertL3(Observe(x.Else(y).Else(z), Sheet()), Observe(x.Else(y.Else(z)), Sheet()));

      var failure = Assert.Throws<ProjectionException>(() => x.Else(y).Else(z).Map(Sheet()));

      Assert.Equal("'z'", failure.Subject);
      Assert.True(
        failure.Message.IndexOf("stands in for 'y'") < failure.Message.IndexOf("stands in for 'x'"),
        "the nearest stand-in is named first, then the one it stood in for");
    }

    // --- Optional idempotence ----------------------------------------------------------------------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OptionalIsIdempotentAtEveryLevelAReaderCanObserve(bool accepts)
    {
      // §14.3, answered: yes, at L3, in both scenarios. The second boundary is inert because the
      // first one never fails — it absorbs, reports, and returns a value — so there is no second
      // Warning, no second rollback and nothing to consume differently.
      var x = accepts ? Accepts("x") : Rejects("x");

      AssertL3(Observe(x.Optional(), Sheet()), Observe(x.Optional().Optional(), Sheet()));
    }

    [Fact]
    public void TheSecondOptionalIsAWrapperOnlyInspectionCanSee()
    {
      // Where the idempotence stops, which is L0: the doubled form is a boundary over a Select over
      // a boundary. Both describe themselves the same way and both are transparent, so the extra
      // level contributes no path segment and no description a diagnostic could carry — which is
      // exactly why the levels above cannot tell them apart.
      var x = Rejects("x");

      var once = x.Optional();
      var twice = x.Optional().Optional();

      Assert.Equal("Optional", once.Description);
      Assert.Equal("Optional", twice.Description);
      Assert.True(once.IsTransparent);
      Assert.True(twice.IsTransparent);

      Assert.Equal("Cell", Assert.Single(Assert.Single(once.Children).Children).Description);
      Assert.Equal("Optional", Assert.Single(Assert.Single(twice.Children).Children).Description);
    }

    [Fact]
    public void OptionalNeverAddedANullableForASecondOneToCollapse()
    {
      // The nullability half of the question turns out not to exist. T? on an unconstrained T is an
      // annotation and not Nullable<T>, so a projection of int stays a projection of int — the
      // absent reading is 0, as BoundaryProjectionTests pins — and both spellings have the same
      // static type at both arities.
      IProjection<int> once = Cell(c => c.GetInt()).Optional();
      IProjection<int> twice = Cell(c => c.GetInt()).Optional().Optional();

      Assert.Equal(0, once.Map(Sheet()));
      Assert.Equal(0, twice.Map(Sheet()));

      IProjection<string?> onceText = Accepts("x").Optional();
      IProjection<string?> twiceText = Accepts("x").Optional().Optional();

      Assert.Equal("x-x", onceText.Map(Sheet()));
      Assert.Equal("x-x", twiceText.Map(Sheet()));
    }

    // --- Choice(x, y) is not x.Else(y) --------------------------------------------------------------

    [Fact]
    public void ChoiceAndElseAgreeOnTheReadingAndDisagreeOnWhatTheyCallIt()
    {
      // v0.4 §9.1, pinned as a difference rather than argued. The two agree at L2 — same value, same
      // extent — and every L3 facet of the one thing they report differs: its severity, its subject,
      // its path and its sentence. Declared alternation is not exercised tolerance.
      var x = Rejects("x");
      var y = Accepts("y");

      AssertL2(Observe(Choice(x, y), Sheet()), Observe(x.Else(y), Sheet()));

      var passedOver = Assert.Single(Choice(x, y).MapWithDiagnostics(Sheet()).Diagnostics);
      var absorbed = Assert.Single(x.Else(y).MapWithDiagnostics(Sheet()).Diagnostics);

      Assert.Equal(DiagnosticSeverity.Info, passedOver.Severity);
      Assert.Equal("Choice", passedOver.Subject);
      Assert.Equal("Choice -> 'x' (Cell)", passedOver.Path);
      Assert.StartsWith("alternative 1 ('x') did not match: ", passedOver.Message);

      Assert.Equal(DiagnosticSeverity.Warning, absorbed.Severity);
      Assert.Equal("'x'", absorbed.Subject);
      Assert.Equal("'x' (Cell)", absorbed.Path);
      Assert.DoesNotContain("alternative", absorbed.Message);
    }

    [Fact]
    public void WhenBothArmsFailChoiceAggregatesWhereElseBlamesTheStandIn()
    {
      // The other half of §9.1: total failure is not merely worded differently, it is ATTRIBUTED
      // differently. A choice that ran out of alternatives failed as itself; a fallback that failed
      // too is blamed on the fallback, with the primary carried along as a note.
      var x = Rejects("x");
      var y = Rejects("y");

      var choice = Assert.Throws<ProjectionException>(() => Choice(x, y).Map(Sheet()));
      var tolerance = Assert.Throws<ProjectionException>(() => x.Else(y).Map(Sheet()));

      Assert.Equal("Choice", choice.Subject);
      Assert.Equal("Choice", choice.Path);
      Assert.Contains("no alternative matched", choice.Message);
      Assert.DoesNotContain("stands in for", choice.Message);

      Assert.Equal("'y'", tolerance.Subject);
      Assert.Equal("'y' (Cell)", tolerance.Path);
      Assert.Contains("it stands in for 'x', which failed too: ", tolerance.Message);
      Assert.DoesNotContain("no alternative matched", tolerance.Message);
    }
  }
}
