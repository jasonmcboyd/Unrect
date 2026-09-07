using System;
using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Spreadsheets.SpreadsheetProjections;

namespace Unrect.Tests
{
  /// <summary>
  /// The boundary half of the absence rule, at every place a failure can be forgiven.
  /// <para>
  /// "I could not look" and "I looked and it is not there" are different answers, and only the second
  /// one is a statement about the document. So a boundary handed a space that cannot answer it faults,
  /// and a fault is not absorbable: <c>Optional</c>, <c>Else</c>, <c>Choice</c> and a repeat's
  /// stopping condition exist to tolerate a section that is genuinely absent, and a wrong backend is
  /// not an absent section. Absorbing one would turn a mis-wired declaration into a quietly empty
  /// result — the worst outcome in the taxonomy, because nothing anywhere would say so.
  /// </para>
  /// <para>
  /// Every test here comes in two halves on purpose. The fault must be loud, and the same declaration
  /// with an ordinary missing anchor must be quiet — otherwise "loud" would only mean that these
  /// boundaries are loud about everything, which is a different (and wrong) system. <c>Optional</c>
  /// is the one shape not repeated here: it is pinned where the capability itself is, in
  /// <see cref="FormulaCapabilityTests"/>.
  /// </para>
  /// </summary>
  public class CapabilityFaultTests
  {
    /// <summary>A space with no formulas in it and no way to answer about formulas.</summary>
    private static ISpace Plain() => GridSpace.Create(new[,] { { "a" }, { "b" } });

    /// <summary>
    /// A declaration that lands on the first row holding a formula, reached through the plain lift.
    /// <para>
    /// The runtime path the typed layer cannot close: <c>Landmark</c> hands back the untyped matcher,
    /// so the demand is dropped at the seam and the mismatch survives to run time. Every fault below
    /// is reached this way, which is the only way it can be reached at all.
    /// </para>
    /// </summary>
    private static IProjection<string> CannotLook() => Text().On(RowWithFormula().Landmark);

    /// <summary>Its well-typed twin: a boundary that can look, and does not find what it wants.</summary>
    private static IProjection<string> LooksAndFindsNothing() => Text().On(RowContaining("no such caption"));

    private static ProjectionException Faults<T>(IProjection<T> projection)
    {
      var failure = Assert.Throws<ProjectionException>(() => projection.Map(Plain()));

      Assert.True(failure.IsFault, "a boundary that could not look must be a fault");
      Assert.IsType<MissingCapabilityException>(failure.InnerException);

      return failure;
    }

    // --- The three tolerances, and what each one does with a fault ----------------------------------

    [Fact]
    public void ElseDoesNotAbsorbABoundaryThatCouldNotLook()
    {
      var failure = Faults(CannotLook().Else("fallback"));

      Assert.Contains("IFormulaSpace", failure.Message, StringComparison.Ordinal);
      Assert.Contains("RowWithFormula", failure.Message, StringComparison.Ordinal);

      // ...and the same shape over an anchor that is genuinely absent is exactly what Else is for.
      Assert.Equal("fallback", LooksAndFindsNothing().Else("fallback").Map(Plain()));
    }

    [Fact]
    public void ChoiceDoesNotTryTheNextAlternativeAfterAFault()
    {
      // The most tempting absorption of the three, and the most dangerous: a Choice exists precisely
      // to move on, and moving on here would mean answering with a section chosen because the reader
      // could not read the file it was pointed at.
      Faults(Choice(CannotLook(), Text()));

      Assert.Equal("a", Choice(LooksAndFindsNothing(), Text()).Map(Plain()));
    }

    [Fact]
    public void ARepeatDoesNotTreatAFaultAsHavingRunOutOfSections()
    {
      // A repeat stops when its item's PLACEMENT fails, and this fault happens during exactly that.
      // Absorbed, it would report zero occurrences of a section the file may well be full of.
      Faults(VerticalRepeat(CannotLook()));

      Assert.Empty(VerticalRepeat(LooksAndFindsNothing()).Map(Plain()));
    }

    [Fact]
    public void ABoundThatCouldNotLookIsAFaultToo()
    {
      // The other lift, and the other strategy slot: Until bounds an extent rather than placing it,
      // so the demand is made from the area strategy instead of the offset strategy. Two code paths
      // wrap a foreign exception, and the fault list is consulted at both.
      var failure = Faults(VerticalFlow(v => v.Next(Text())).Until(RowWithFormula().Landmark).Optional());

      Assert.Contains("IFormulaSpace", failure.Message, StringComparison.Ordinal);
    }

    // --- The same failure whenever it is discovered -------------------------------------------------

    [Theory]
    [InlineData("on")]
    [InlineData("until")]
    public void TheFaultReadsIdenticallyThroughTheDeferredAndTheMeasuredPath(string lift)
    {
      // A declared extent may be discovered as the projection consumes it, so a failure can arrive at
      // two different moments. Rule 3 of the lazy denotation: only the moment differs. It holds here
      // by construction — a capability is demanded before any row is read either way — and it is
      // pinned because a reader comparing two of these failures must not have to know which path
      // produced them.
      IProjection<string> projection = lift == "on"
        ? CannotLook()
        : VerticalFlow(v => v.Next(Text())).Until(RowWithFormula().Landmark);

      var deferred = Assert.Throws<ProjectionException>(() => projection.MapWithDiagnostics(Plain()));

      ProjectionException measured;
      using (ProjectionEngine.ForceEager())
        measured = Assert.Throws<ProjectionException>(() => projection.MapWithDiagnostics(Plain()));

      Assert.Equal(measured.Message, deferred.Message);
      Assert.Equal(measured.Subject, deferred.Subject);
      Assert.Equal(measured.Path, deferred.Path);
      Assert.Equal(measured.Location.ToString(), deferred.Location.ToString());
      Assert.Equal(measured.IsFault, deferred.IsFault);
      Assert.Equal(measured.InnerException?.GetType(), deferred.InnerException?.GetType());
    }

    // --- The door the faults come through -----------------------------------------------------------

    [Fact]
    public void TheDemandingDoorHandsBackTheCapabilityWhenThereIsOne()
    {
      // RequiredCapability is Capability with a different answer to absence, not a different search:
      // it walks the same charts and finds the same thing.
      var sheet = SpreadsheetSpace.CreateWithFormulas(
        System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", "formulas.xlsx"),
        "Formulas");

      Assert.Same(sheet, ((ISpace)sheet).RequiredCapability<IFormulaSpace>("RowWithFormula()"));
    }

    [Fact]
    public void TheDemandingDoorNamesWhatAskedAndWhatWasNeeded()
    {
      // The message is read by someone who wrote a declaration and pointed it at the wrong door, so
      // it has to say both halves: what could not be answered, and which part of their declaration
      // asked. The demander is passed as the matcher spells itself rather than as a type name.
      var missing = Assert.Throws<MissingCapabilityException>(
        () => Plain().RequiredCapability<IFormulaSpace>("RowWithFormula(\"SUBTOTAL\")"));

      Assert.Equal(typeof(IFormulaSpace), missing.Capability);
      Assert.Equal("RowWithFormula(\"SUBTOTAL\")", missing.DemandedBy);
      Assert.Contains("IFormulaSpace", missing.Message, StringComparison.Ordinal);
      Assert.Contains("RowWithFormula(\"SUBTOTAL\")", missing.Message, StringComparison.Ordinal);

      // It derives from InvalidOperationException, which the engine's fault list does NOT include as
      // a family — parse helpers throw that for data reasons. This type is named there explicitly,
      // and that naming is the whole of its unabsorbability.
      Assert.IsAssignableFrom<InvalidOperationException>(missing);
    }

    [Fact]
    public void TheDemandingDoorRefusesToBeAskedWithoutBothHalves()
    {
      Assert.Throws<ArgumentNullException>(() => new MissingCapabilityException(null!, "RowWithFormula()"));
      Assert.Throws<ArgumentNullException>(() => new MissingCapabilityException(typeof(IFormulaSpace), null!));
    }

    [Fact]
    public void AskingANullSpaceIsAnAbsenceRatherThanANullReference()
    {
      // Both doors take the space as a nullable receiver, so an unset space is answered by the rule
      // rather than by a NullReferenceException from inside the walk. Null offers no capability, so
      // one door says so and the other faults.
      ISpace? nothing = null;

      Assert.Null(nothing.Capability<IFormulaSpace>());
      Assert.Throws<MissingCapabilityException>(() => nothing.RequiredCapability<IFormulaSpace>("RowWithFormula()"));
    }

    // --- What the transport walks -------------------------------------------------------------------

    [Fact]
    public void TheSeamLooksThroughAChartAndStopsAtTheFirstAnswer()
    {
      // The unwrap protocol, stated on its own rather than through a declaration: a chart is a
      // different view of the same cells, so what it wraps answers for it. The property that makes
      // this safe is the one ISpaceChart names — coordinates do not move — so the capability that
      // comes back is about the cells the caller is holding.
      var values = new CellValue[2, 2];
      var formulas = new string?[2, 2] { { "A1*2", null }, { null, null } };
      var capable = new FormulaGridSpace(values, formulas);

      var charted = new Chart(new Chart(capable));

      Assert.Same(capable, charted.Capability<IFormulaSpace>());
      Assert.Null(new Chart(Plain()).Capability<IFormulaSpace>());
    }

    /// <summary>
    /// The minimum <see cref="ISpaceChart"/>: a different view of exactly the same cells, which is
    /// all the seam's walk is entitled to assume.
    /// </summary>
    private sealed class Chart : ISpace, ISpaceChart
    {
      private readonly ISpace _inner;

      internal Chart(ISpace inner) => _inner = inner;

      public ISpace Underlying => _inner;

      public Area Area => _inner.Area;

      public CellValue this[int column, int row] => _inner[column, row];

      public ISpace GetSubspace(Offset offset, Area area) => new Chart(_inner.GetSubspace(offset, area));
    }
  }
}
