using System;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Spreadsheets
{
  /// <summary>
  /// Reading a cell through the point that addresses it — <c>row["Amount"].Decimal()</c> — which is
  /// the whole of what the kinded surface is, once the leaves and the views are taken away.
  /// <para>
  /// <b>The receiver's type carries the requirement.</b> These are extensions on a
  /// <c>Point&lt;TSpace&gt;</c> where <c>TSpace</c> is an <see cref="ICellSpace"/>, so a declaration
  /// written over a plain grid cannot reach them at all and one written over a sheet needs nothing
  /// annotated to. That is the capability seam in its entirety: no chart, no query, no
  /// <c>MissingCapability</c> — a type parameter, and the compiler.
  /// </para>
  /// <para>
  /// <b>Two families, one law between them.</b> A strict read refuses a blank as the wrong kind; an
  /// <c>OrBlank</c> read hands back null for it. Neither tolerates a cell of the WRONG KIND, and that
  /// asymmetry is the point: a missing value says something about the data, and a value of the wrong
  /// kind says something about the format, which is never something a declaration meant to accept.
  /// </para>
  /// </summary>
  public class PointReadTests
  {
    private static readonly DateTime Moment = new DateTime(2026, 3, 4, 13, 45, 0);

    /// <summary>The point addressing the only cell of a one-cell sheet holding <paramref name="value"/>.</summary>
    private static Point<ICellSpace> Of(object? value)
    {
      var sheet = Mixed(new object?[,] { { value } });

      return Plane<ICellSpace>.Of(sheet)[0, 0];
    }

    // --- The six strict reads ----------------------------------------------------------------------

    [Fact]
    public void EachReadHandsBackTheCellsValueAsTheKindItAsksFor()
    {
      Assert.Equal("hello", Of("hello").Text());
      Assert.Equal(1.5m, Of(1.5m).Decimal());
      Assert.Equal(42, Of(42).Integer());
      Assert.Equal(0.25, Of(0.25).Double());
      Assert.Equal(Moment, Of(Moment).Date());
      Assert.True(Of(true).Boolean());
    }

    [Theory]
    [InlineData("hello", "Text")]
    [InlineData(1.5, "Number")]
    [InlineData(true, "Boolean")]
    [InlineData(null, "Blank")]
    public void AStrictReadOfTheWrongKindSaysWhatWasThereInstead(object? value, string found)
    {
      // The document's vocabulary, never the reader's, and the A1 inside the sentence — which is what
      // makes a bare CellReadException readable on its own.
      var failure = Assert.Throws<CellReadException>(() => Of(value).Date());

      Assert.Equal($"expected Temporal at A1, found {found}", failure.Message);
    }

    [Fact]
    public void AndABlankIsRefusedLikeAnyOtherWrongKind()
    {
      // Stated once per family: the strict read has no notion of "absent", so a blank is simply not
      // the kind it asked for. The OrBlank twin below is the whole of the difference.
      Assert.Equal("expected Number at A1, found Blank", Assert.Throws<CellReadException>(() => Of(null).Decimal()).Message);
      Assert.Equal("expected Text at A1, found Blank", Assert.Throws<CellReadException>(() => Of(null).Text()).Message);
    }

    [Fact]
    public void AConversionFailureIsAboutANumberThatIsReallyThere()
    {
      // Kind versus conversion: the cell IS a Number, so the sentence is about the number rather
      // than about the kind, and it shows the value as the cell holds it.
      Assert.Equal("the Number at A1 (1.5) is not a whole number", Assert.Throws<CellReadException>(() => Of(1.5).Integer()).Message);
      Assert.Equal("the Number at A1 (5000000000) is outside the range of a 32-bit integer", Assert.Throws<CellReadException>(() => Of(5e9).Integer()).Message);
      Assert.Equal("the Number at A1 (1E+30) is not representable as a decimal", Assert.Throws<CellReadException>(() => Of(1e30).Decimal()).Message);
    }

    // --- The six OrBlank reads ---------------------------------------------------------------------

    [Fact]
    public void EveryOrBlankReadTakesABlankAsNull()
    {
      // All six, because the tolerance is per accessor rather than a modifier over one: a family of
      // six with five members tolerant is a wart nobody would find except by hitting it.
      Assert.Null(Of(null).TextOrBlank());
      Assert.Null(Of(null).DecimalOrBlank());
      Assert.Null(Of(null).IntegerOrBlank());
      Assert.Null(Of(null).DoubleOrBlank());
      Assert.Null(Of(null).DateOrBlank());
      Assert.Null(Of(null).BooleanOrBlank());
    }

    [Fact]
    public void AndEveryOneOfThemStillReadsAValueWhenThereIsOne()
    {
      // Non-vacuity for the six above: tolerating a blank is not answering null to everything.
      Assert.Equal("hello", Of("hello").TextOrBlank());
      Assert.Equal(1.5m, Of(1.5m).DecimalOrBlank());
      Assert.Equal(42, Of(42).IntegerOrBlank());
      Assert.Equal(0.25, Of(0.25).DoubleOrBlank());
      Assert.Equal(Moment, Of(Moment).DateOrBlank());
      Assert.True(Of(true).BooleanOrBlank());
    }

    [Fact]
    public void AndNoneOfThemToleratesACellOfTheWrongKind()
    {
      // Blank tolerance is not kind tolerance — the law this family exists to state. The sentence is
      // the strict read's, word for word, because it is the same reading with one condition moved.
      Assert.Equal("expected Number at A1, found Text", Assert.Throws<CellReadException>(() => Of("x").DecimalOrBlank()).Message);
      Assert.Equal("expected Text at A1, found Number", Assert.Throws<CellReadException>(() => Of(5m).TextOrBlank()).Message);
      Assert.Equal("expected Temporal at A1, found Number", Assert.Throws<CellReadException>(() => Of(5m).DateOrBlank()).Message);
      Assert.Equal("expected Boolean at A1, found Number", Assert.Throws<CellReadException>(() => Of(1m).BooleanOrBlank()).Message);
      Assert.Equal("expected Number at A1, found Text", Assert.Throws<CellReadException>(() => Of("x").IntegerOrBlank()).Message);
      Assert.Equal("expected Number at A1, found Text", Assert.Throws<CellReadException>(() => Of("x").DoubleOrBlank()).Message);
    }

    [Fact]
    public void AndAConversionStillFailsThroughTheTolerantForm()
    {
      // The corner between the two: the cell is neither blank nor the wrong kind, so there is nothing
      // for OrBlank to have an opinion about and the conversion fails exactly as it would strictly.
      Assert.Equal(
        "the Number at A1 (1.5) is not a whole number",
        Assert.Throws<CellReadException>(() => Of(1.5).IntegerOrBlank()).Message);
    }

    // --- The canonical questions, which every point answers whatever its space is -------------------

    [Fact]
    public void ThePointStillAnswersTheCanonicalFourWithoutAnyKind()
    {
      // What is underneath the kinded reads, and what a declaration over a plain grid is left with.
      // Nothing here can fail: a blank cell has an address, an error cell has an address, and both
      // render.
      Assert.True(Of(null).IsBlank);
      Assert.False(Of("hello").IsBlank);
      Assert.True(Of("hello").IsText);
      Assert.False(Of(1.5m).IsText);
      Assert.Equal("1.5", Of(1.5m).AsText());
      Assert.Null(Of(null).AsText());
    }

    // --- Asking rather than asserting ---------------------------------------------------------------

    [Fact]
    public void EveryQuestionAnswersForEveryCellTheStrictReadsRefuse()
    {
      // What a predicate puts to a cell before deciding anything about it. None of these throws on
      // any cell: a rule that had to guard its question with a try would not be a rule.
      var cells = new[] { Of(null), Of("hello"), Of(1.5m), Of(Moment), Of(true), Of(Cell.OfError(CellError.DivisionByZero)) };

      Assert.Equal(new[] { false, true, false, false, false, false }, cells.Select(c => c.IsText).ToArray());
      Assert.Equal(new[] { false, false, true, false, false, false }, cells.Select(c => c.IsDouble()).ToArray());
      Assert.Equal(new[] { false, false, false, true, false, false }, cells.Select(c => c.IsDate()).ToArray());
      Assert.Equal(new[] { false, false, false, false, true, false }, cells.Select(c => c.IsBoolean()).ToArray());
      Assert.Equal(new[] { false, false, false, false, false, true }, cells.Select(c => c.IsError()).ToArray());
      Assert.Equal(new[] { true, false, false, false, false, false }, cells.Select(c => c.IsBlank).ToArray());
    }

    [Fact]
    public void AQuestionIsTrueExactlyWhenTheReadOfTheSameNameSucceeds()
    {
      // Is… is the read with its value thrown away, so a predicate that ruled a cell in and a leaf
      // that then refused it — two readings of one cell — cannot happen.
      foreach (var cell in new[] { Of(null), Of("x"), Of(1.5m), Of(2m), Of(1e30), Of(Moment), Of(true) })
      {
        Assert.Equal(cell.IsText, Succeeds(() => cell.Text()));
        Assert.Equal(cell.IsDouble(), Succeeds(() => cell.Double()));
        Assert.Equal(cell.IsDecimal(), Succeeds(() => cell.Decimal()));
        Assert.Equal(cell.IsInteger(), Succeeds(() => cell.Integer()));
        Assert.Equal(cell.IsDate(), Succeeds(() => cell.Date()));
        Assert.Equal(cell.IsBoolean(), Succeeds(() => cell.Boolean()));
      }

      static bool Succeeds(Action read)
      {
        try
        {
          read();
          return true;
        }
        catch (CellReadException)
        {
          return false;
        }
      }
    }

    [Fact]
    public void AQuestionIsAboutAReadingAndACellHasSeveral()
    {
      // There is no "which one is it": 2 is a double, a decimal and an integer at once, 1.5 is two
      // of those, and 1e30 is one. Which is why nothing here hands back a single kind.
      Assert.True(Of(2m).IsDouble() && Of(2m).IsDecimal() && Of(2m).IsInteger());
      Assert.True(Of(1.5m).IsDouble() && Of(1.5m).IsDecimal() && !Of(1.5m).IsInteger());
      Assert.True(Of(1e30).IsDouble() && !Of(1e30).IsDecimal() && !Of(1e30).IsInteger());
    }

    [Fact]
    public void TheTryFormsHandBackTheValueAndSayNothingOnARefusal()
    {
      Assert.True(Of(1.5m).TryGetDouble(out var number));
      Assert.Equal(1.5, number);
      Assert.False(Of("x").TryGetDouble(out _));

      Assert.True(Of(Moment).TryGetDate(out var moment));
      Assert.Equal(Moment, moment);

      Assert.True(Of(true).TryGetBoolean(out var flag));
      Assert.True(flag);
    }
  }
}
