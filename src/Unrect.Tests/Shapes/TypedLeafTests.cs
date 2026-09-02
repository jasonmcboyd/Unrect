using System;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// The typed leaves: a cell whose kind is part of the declaration rather than of the lambda that
  /// reads it. <c>Decimal()</c> says "a number lives here" where <c>Cell(c =&gt; c.GetDecimal())</c>
  /// only said "read this somehow, and hope".
  /// <para>
  /// The discipline these tests exist to protect is §1.4's: a <em>kind</em> failure speaks kinds
  /// ("expected Number, found Text") and a <em>conversion</em> failure speaks conversions ("is not a
  /// whole number"). A reader who sees the first goes to the column; a reader who sees the second
  /// goes to the cell.
  /// </para>
  /// </summary>
  public class TypedLeafTests
  {
    private static ISpace One(object? value) => Mixed(new object?[,] { { value } });

    // --- Each leaf reads its kind ------------------------------------------------------------------

    [Fact]
    public void EachLeafReadsTheKindItDeclares()
    {
      var moment = new DateTime(2026, 3, 4, 13, 45, 0);

      Assert.Equal("hello", Text().Map(One("hello")));
      Assert.Equal(1.5m, Decimal().Map(One(1.5m)));
      Assert.Equal(42, Integer().Map(One(42)));
      Assert.Equal(0.25, Double().Map(One(0.25)));
      Assert.Equal(moment, Date().Map(One(moment)));
      Assert.True(Boolean().Map(One(true)));
    }

    [Fact]
    public void EachLeafDescribesItselfByItsFactory()
    {
      Assert.Equal("Text", Text().Description);
      Assert.Equal("Decimal", Decimal().Description);
      Assert.Equal("Integer", Integer().Description);
      Assert.Equal("Double", Double().Description);
      Assert.Equal("Date", Date().Description);
      Assert.Equal("Boolean", Boolean().Description);
    }

    [Fact]
    public void EachLeafConsumesExactlyOneCell()
    {
      var applied = Decimal().Apply(One(1.5m));

      Assert.Equal(1, applied.Consumed.Width);
      Assert.Equal(1, applied.Consumed.Height);
    }

    [Fact]
    public void Date_KeepsTheTimeOfDay()
    {
      // A cell that carries a time carries it out: truncation is the caller's decision, taken in
      // Select where the meaning is known, not the leaf's.
      var moment = new DateTime(2026, 3, 4, 13, 45, 0);

      Assert.Equal(moment, Date().Map(One(moment)));
      Assert.Equal(13, Date().Map(One(moment)).Hour);
    }

    // --- Kind failures speak kinds ---------------------------------------------------------------------

    [Theory]
    [InlineData("x", "found Text")]
    [InlineData(null, "found Blank")]
    public void AKindFailureNamesTheKindThatWasThere(object? value, string found)
    {
      var failure = Assert.Throws<ShapeException>(() => Decimal().Map(One(value)));

      Assert.Equal($"expected Number at A1, {found}", Problem(failure));
      Assert.Equal("Decimal", failure.Subject);
      Assert.Equal("Decimal", failure.Path);
    }

    [Fact]
    public void AnErrorCellIsNamedAsTheErrorItIs()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Decimal().Map(One(CellValue.OfError(CellError.DivisionByZero))));

      Assert.Equal("expected Number at A1, found Error(#DIV/0!)", Problem(failure));
    }

    [Fact]
    public void TextOverANumberSpeaksKindsToo()
    {
      Assert.Equal("expected Text at A1, found Number", Problem(Assert.Throws<ShapeException>(() => Text().Map(One(1)))));
    }

    [Fact]
    public void AKindFailureNeverMentionsTheLeafsOwnName()
    {
      // The §1.4 pin. "expected Decimal, found Text" would be a category error: the cell holds a
      // Number or it does not, and Decimal is one of several ways to read a Number.
      var failure = Assert.Throws<ShapeException>(() => Decimal().Map(One("x")));

      Assert.DoesNotContain("Decimal", Problem(failure));
      Assert.Contains("Number", Problem(failure));
    }

    // --- Conversion failures speak conversions ------------------------------------------------------------

    [Fact]
    public void IntegerOverANonWholeNumber_SaysSo()
    {
      Assert.Equal(
        "the Number at A1 (1.5) is not a whole number",
        Problem(Assert.Throws<ShapeException>(() => Integer().Map(One(1.5)))));
    }

    [Fact]
    public void IntegerOutOfRange_SaysSo()
    {
      Assert.Equal(
        "the Number at A1 (5000000000) is outside the range of a 32-bit integer",
        Problem(Assert.Throws<ShapeException>(() => Integer().Map(One(5e9)))));
    }

    [Fact]
    public void DecimalOverAnUnrepresentableNumber_SaysSo()
    {
      Assert.Equal(
        "the Number at A1 (1E+30) is not representable as a decimal",
        Problem(Assert.Throws<ShapeException>(() => Decimal().Map(One(1e30)))));
    }

    [Fact]
    public void AConversionFailureShowsTheValueAsTheCellHoldsIt()
    {
      // A number that arrived as a decimal keeps its scale in the message: the cell says 1.50, so
      // the failure says 1.50. Rendering it through a double would print 1.5 and quietly disagree
      // with the sheet the reader is looking at.
      Assert.Equal(
        "the Number at A1 (1.50) is not a whole number",
        Problem(Assert.Throws<ShapeException>(() => Integer().Map(One(1.50m)))));

      // ...and one that arrived as a double is rendered as a double.
      Assert.Equal(
        "the Number at A1 (1.5) is not a whole number",
        Problem(Assert.Throws<ShapeException>(() => Integer().Map(One(1.5)))));
    }

    [Fact]
    public void AConversionFailureIsAboutTheCellNotTheColumn()
    {
      // The other half of the discipline: the kind was right, so the message does not talk about
      // kinds — it names the value that would not fit.
      var failure = Assert.Throws<ShapeException>(() => Integer().Map(One(1.5)));

      Assert.DoesNotContain("expected", Problem(failure));
      Assert.Contains("1.5", Problem(failure));
    }

    // --- Extent -------------------------------------------------------------------------------------------

    [Fact]
    public void ALeafForcedToMoreThanOneCell_Throws()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Decimal().Sized(WholeExtent()).Map(Mixed(new object?[,] { { 1, 2 } })));

      Assert.Equal("a Decimal must be exactly one cell; this one is 2x1", Problem(failure));
    }

    // --- Tolerance and alternation --------------------------------------------------------------------------

    [Fact]
    public void ALeafFailureIsAbsorbable()
    {
      var result = Decimal().Optional().MapWithDiagnostics(One("x"));

      Assert.Equal(0m, result.Value);

      var warning = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);

      Assert.Equal("Decimal", warning.Subject);
      Assert.Equal("A1", warning.Location.A1);
    }

    [Fact]
    public void AlternativesCanDiscriminateOnDeclaredKinds()
    {
      // The payoff the audit predicted. A Choice over typed leaves picks the arm whose kind matches,
      // which the untyped Cell(c => …) spelling could only do by throwing from inside a lambda.
      var readEither = Choice(
        Decimal().Select(amount => $"number:{amount}"),
        Text().Select(text => $"text:{text}"));

      var result = readEither.MapWithDiagnostics(One("n/a"));

      Assert.Equal("text:n/a", result.Value);

      var info = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Info);
      Assert.StartsWith("alternative 1 (Decimal) did not match: ", info.Message);

      Assert.Equal("number:1.5", readEither.Map(One(1.5m)));
    }

    // --- Composition -------------------------------------------------------------------------------------------

    [Fact]
    public void ALeafTakesItsUseSiteLabel()
    {
      var amount = Decimal();

      var failure = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v => v.Next(amount)).Map(One("x")));

      Assert.Equal("VerticalFlow -> 'amount' (Decimal)", failure.Path);
    }

    [Fact]
    public void AFourLeafHeaderConsumesWhatColumnFourDoes()
    {
      // The §1.6 claim, pinned side by side: naming the kinds costs nothing in placement.
      var space = Mixed(new object?[,]
      {
        { "Capital Activity Report" },
        { "Q2 2026" },
        { new DateTime(2026, 6, 30) },
        { "RPT-1" },
      });

      var byLeaves = VerticalFlow(v => new
      {
        Title = v.Next(Text()),
        Subtitle = v.Next(Text()),
        Date = v.Next(Date()),
        Id = v.Next(Text()),
      }).Apply(space);

      var byColumn = Column(4, c => c[0].GetString()).Apply(space);

      Assert.Equal(byColumn.Consumed.Width, byLeaves.Consumed.Width);
      Assert.Equal(byColumn.Consumed.Height, byLeaves.Consumed.Height);
      Assert.Equal(1, byLeaves.Consumed.Width);
      Assert.Equal(4, byLeaves.Consumed.Height);
      Assert.Equal("Capital Activity Report", byLeaves.Value.Title);
    }

    // --- The collision pin ----------------------------------------------------------------------------------------

    [Fact]
    public void TheLeafNamesDoNotShadowTheKeywordAliases()
    {
      // This test exists to fail at COMPILE time. Text, Decimal, Double, Date, Boolean and Integer
      // are imported here as static members of Shape; if any of them ever shadowed the framework
      // type of the same name, the declarations and calls below would stop resolving.
      Decimal money = 1m;
      Double ratio = 0.5;
      Boolean flag = true;
      Int32 count = 2;
      String label = "x";
      DateTime moment = DateTime.MinValue;

      Assert.Equal(1m, decimal.Parse("1"));
      Assert.False(double.IsNaN(ratio));
      Assert.True(bool.TryParse("true", out _));
      Assert.Equal(2, int.Parse("2"));

      Assert.Equal(1m, money);
      Assert.True(flag);
      Assert.Equal(2, count);
      Assert.Equal("x", label);
      Assert.Equal(DateTime.MinValue, moment);

      // ...and all six factories still resolve to the shapes.
      Assert.Equal("Text", Text().Description);
      Assert.Equal("Decimal", Decimal().Description);
      Assert.Equal("Integer", Integer().Description);
      Assert.Equal("Double", Double().Description);
      Assert.Equal("Date", Date().Description);
      Assert.Equal("Boolean", Boolean().Description);
    }

    /// <summary>The problem text, without the subject the template puts in front of it.</summary>
  }
}
