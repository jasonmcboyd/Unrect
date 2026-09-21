using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The two leaves the core vocabulary has, and the only two it will have: <c>Point()</c>, which
  /// hands over the address of one cell, and <c>AsText()</c>, which reads what that cell says.
  /// <para>
  /// <b>They are the canonical surface's whole reading vocabulary, and that is a deliberate floor
  /// rather than a gap.</b> An <see cref="ISpace"/> answers four questions — how big it is, whether
  /// a cell is blank, whether it is text, and what it says — so a leaf over the canonical surface can
  /// project a cell's address or a cell's rendering, and nothing else. Everything that reads a KIND
  /// lives in a backend, because a kind is a backend's word; everything that converts lives in
  /// <c>Select</c>, because a conversion is the declaration's own.
  /// </para>
  /// <para>
  /// <b><c>AsText()</c> is TOTAL, which is what makes it different in kind from the six kinded
  /// leaves.</b> Every space renders every cell it has, so the only thing that can go wrong is that
  /// there is nothing there — and that is the only sentence it has. It never speaks of kinds, never
  /// converts, and therefore never discriminates: <see cref="ChoiceProjectionTests"/> pins the
  /// consequence, which is that an <c>AsText()</c> alternative always wins.
  /// </para>
  /// </summary>
  public class CanonicalLeafTests
  {
    /// <summary>One cell, holding whatever is asked for.</summary>
    private static ICellSpace One(object? value) => Mixed(new object?[,] { { value } });

    /// <summary>
    /// A blank cell with a neighbour. The neighbour is the point: a row that is blank all the way
    /// across is a gap, which every placement steps over, so a blank that is a VALUE is a blank
    /// cell in a row that has something else in it.
    /// </summary>
    private static ICellSpace BlankCell() => Mixed(new object?[,] { { null, "." } });

    // --- Point: the address, and exactly one cell of it --------------------------------------------

    [Fact]
    public void APointLeafHandsOverTheAddressOfTheCellItWasPlacedOn()
    {
      // Nothing is read. The leaf's whole job is to say WHERE, in the sheet's own coordinates, and
      // the reading is asked of the point afterwards by whoever wanted one.
      var point = Right(1).Down(2).Of(Point()).Map(CoordinateGrid(4, 4));

      Assert.Equal(1, point.Column);
      Assert.Equal(2, point.Row);
      Assert.Equal("22", point.AsText());
    }

    [Fact]
    public void APointLeafConsumesExactlyOneCell()
    {
      var applied = Point().Apply(CoordinateGrid(4, 4));

      Assert.Equal(1, applied.Consumed.Width);
      Assert.Equal(1, applied.Consumed.Height);
    }

    [Fact]
    public void APointLeafReadsNothingAtAll_NotEvenABlankOrAnError()
    {
      // The total-est leaf there is: a blank cell and an error cell both have addresses, so both are
      // projected without a word. This is what makes Point() the escape hatch for a column of no one
      // kind — there is no reading to disagree with the data.
      Assert.True(Point().Map(BlankCell()).IsBlank);
      Assert.False(Point().Map(One(Cell.OfError(CellError.DivisionByZero))).IsBlank);
    }

    [Fact]
    public void APointLeafDescribesItselfByItsFactory()
    {
      Assert.Equal("Point", Point().Description);
    }

    // --- AsText: total, except for the one thing that can be missing --------------------------------

    [Fact]
    public void AsTextReadsWhatEveryKindOfCellSays()
    {
      // Every kind but Blank, through the one leaf, with no declaration of what any of them is. The
      // renderings are the backend's — pinned per door in SpaceContractTests — and what is pinned
      // here is that the leaf hands them back untouched.
      Assert.Equal("hello", AsText().Map(One("hello")));
      Assert.Equal("1.5", AsText().Map(One(1.5m)));
      Assert.Equal("TRUE", AsText().Map(One(true)));
      Assert.Equal("#DIV/0!", AsText().Map(One(Cell.OfError(CellError.DivisionByZero))));
    }

    [Fact]
    public void AsTextOverABlankCellIsTheOneSentenceItHas()
    {
      // Absence, said in the canonical vocabulary: there is no kind to have expected and none to
      // report finding, so the sentence speaks of a value and a blank cell and nothing else. The
      // A1 is inside it, exactly as a kinded leaf's is.
      var failure = Assert.Throws<ProjectionException>(() => Right(1).Of(AsText()).Map(Mixed(new object?[,] { { "a", null } })));

      Assert.Equal("expected a value at B1, found a blank cell", Problem(failure));
      Assert.Equal("AsText", failure.Subject);
      Assert.Equal("B1", failure.Location.A1);
    }

    [Fact]
    public void AsTextOrBlankReadsTheAbsenceAsNullAndSaysNothing()
    {
      // Quietly — the declaration said the cell may be absent, so its absence is the answer rather
      // than something to report. That is the whole difference from Optional, which absorbs a
      // failure and raises a Warning about it.
      var read = AsText().OrBlank().MapWithDiagnostics(BlankCell());

      Assert.Null(read.Value);
      Assert.DoesNotContain(read.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void AsTextOrBlankStillReadsAValueWhenThereIsOne()
    {
      // Non-vacuity: the tolerant form is the same reading, not a reading that stopped happening.
      Assert.Equal("hello", AsText().OrBlank().Map(One("hello")));
    }

    [Fact]
    public void TheTolerantFormSaysSoInItsOwnDescription()
    {
      // The same convention the kinded leaves follow — Decimal becomes Decimal? — so a path segment
      // says which of the two readings was declared.
      Assert.Equal("AsText", AsText().Description);
      Assert.Equal("AsText?", AsText().OrBlank().Description);
    }

    [Fact]
    public void AsTextSizedLargerThanOneCellThrows()
    {
      // A leaf validates the extent IT was given, and names itself doing it. Declared on the leaf
      // rather than through a Select, because a Select is placed by the engine like any other
      // projection and would re-place its inner leaf at the leaf's own 1x1 default.
      var failure = Assert.Throws<ProjectionException>(() =>
        Sized(Unrect.Strategies.AreaStrategies.ExplicitArea(2, 1)).Of(AsText()).Map(CoordinateGrid(4, 4)));

      Assert.Contains("an AsText must be exactly one cell; this one is 2x1", failure.Message);
    }

    // --- Both leaves are absorbable, because both speak about the data ------------------------------

    [Fact]
    public void ABlankRefusedByAsTextIsAbsorbable()
    {
      // A blank cell is a statement about the data, not a broken declaration, so a tolerance
      // boundary may take it — which is the property a fault would remove.
      var read = AsText().Optional().MapWithDiagnostics(BlankCell());

      Assert.Null(read.Value);
      Assert.Contains(read.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }
  }
}
