using System;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// An anchor row, declared rather than searched for. Its placement finds the row, its extent is
  /// that row at the full available width, and its projection asserts the match and yields what the
  /// cell actually says.
  /// <para>
  /// The literal in the declaration is the <em>matcher</em>; the cell is the <em>datum</em>. So a
  /// caption yields the file's text verbatim — untrimmed, in the file's own casing — while matching
  /// on the trimmed, case-insensitive, whole-cell rule every other content locator uses.
  /// </para>
  /// </summary>
  public class CaptionShapeTests
  {
    // A junk row, then the caption written as the file has it, then two data rows.
    private static ISpace Sheet() => Mixed(new object?[,]
    {
      { "junk", null },
      { "  EIN:  ", null },
      { "a", 1 },
      { "b", 2 },
    });

    // --- Placement ---------------------------------------------------------------------------------

    [Fact]
    public void ACaptionFindsItsRowAheadOfTheCursor()
    {
      var applied = Caption("ein:").Apply(Sheet());

      Assert.Equal(0, applied.Offset.Size.Width);
      Assert.Equal(1, applied.Offset.Size.Height);
    }

    [Fact]
    public void ACaptionConsumesExactlyOneRowAtTheFullWidth()
    {
      // One row, because it is a row; full width, because a caption owns its whole line rather than
      // just the cell that matched.
      var applied = Caption("ein:").Apply(Sheet());

      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(1, applied.Consumed.Height);
    }

    [Fact]
    public void TheNextSiblingStartsOnTheRowBelowTheCaption()
    {
      var read = VerticalFlow(v => $"{v.Next(Caption("ein:"))}|{v.Next(Cell(c => c.GetString()))}").Map(Sheet());

      Assert.Equal("  EIN:  |a", read);
    }

    // --- The value ---------------------------------------------------------------------------------------

    [Fact]
    public void ACaptionYieldsTheFilesTextVerbatim()
    {
      // Not the declaration's literal, and not trimmed: the reader asked where the section starts,
      // and what the sheet actually calls it is a fact worth keeping.
      Assert.Equal("  EIN:  ", Caption("ein:").Map(Sheet()));
    }

    // --- Matching ----------------------------------------------------------------------------------------

    [Theory]
    [InlineData("EIN:")]
    [InlineData("ein:")]
    [InlineData("  EIN:  ")]
    public void ACaptionMatchesTrimmedAndCaseInsensitively(string declared)
    {
      // The same rule as RowContaining and the To/Past lifts, from the same helper.
      Assert.Equal("  EIN:  ", Caption(declared).Map(Sheet()));
    }

    [Theory]
    [InlineData("EIN")]
    [InlineData("IN:")]
    [InlineData("EIN: number")]
    public void ACaptionDoesNotMatchASubstring(string declared)
    {
      // Whole cell values only; a substring rule would anchor on the first row that merely mentions
      // the word.
      Assert.Throws<ShapeException>(() => Caption(declared).Map(Sheet()));
    }

    // --- The two failures --------------------------------------------------------------------------------

    [Fact]
    public void ACaptionThatIsNotThere_SaysWhatItLookedFor()
    {
      var failure = Assert.Throws<ShapeException>(() => Caption("Nope").Map(Sheet()));

      Assert.Equal("Caption(\"Nope\")", failure.Subject);
      Assert.Contains("no row containing 'Nope' exists in the available space", failure.Message);
    }

    [Fact]
    public void ACaptionMissIsAbsorbedByAToleranceBoundary()
    {
      // A section whose caption is absent is an absent section, which is what Optional is for.
      Assert.Null(Caption("Nope").Optional().Map(Sheet()));
    }

    [Fact]
    public void ACaptionPlacedSomewhereElse_AssertsWhereItLanded()
    {
      // Reachable only when the placement was replaced. The seek half is gone, so what is left is
      // the assertion half: the row it was pointed at is not the row it was promised.
      var space = Mixed(new object?[,] { { "x" }, { "y" } });

      var failure = Assert.Throws<ShapeException>(() => Caption("ein:").After(SkipRows(1)).Map(space));

      Assert.Contains("expected a row containing 'ein:' here", failure.Message);
      Assert.Equal("A2", failure.Location.A1);
    }

    [Fact]
    public void ACaptionForcedToMoreThanOneRow_Throws()
    {
      var failure = Assert.Throws<ShapeException>(() => Caption("ein:").Sized(WholeExtent()).Map(Sheet()));

      Assert.Contains("a Caption must be exactly one row tall; this one is 3 rows tall", failure.Message);
    }

    // --- Inspection --------------------------------------------------------------------------------------

    [Fact]
    public void ACaptionDescribesItselfByWhatItLooksFor()
    {
      // A path segment that can be grepped back to the line that produced it.
      var caption = Caption("IRR Details");

      Assert.Equal("Caption(\"IRR Details\")", caption.Description);
      Assert.Empty(caption.Children);
      Assert.False(caption.IsTransparent);
    }

    [Fact]
    public void ANamedCaptionIsNamedInTheSubjectAndThePath()
    {
      var failure = Assert.Throws<ShapeException>(() => Caption("Nope").Named("section header").Map(Sheet()));

      Assert.Equal("'section header'", failure.Subject);
      Assert.Equal("'section header' (Caption)", failure.Path);
    }

    // --- Guards -------------------------------------------------------------------------------------------

    [Fact]
    public void ACaptionRejectsATextItCouldNeverMatch()
    {
      Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => Caption(null!)).ParamName);
      Assert.Equal("text", Assert.Throws<ArgumentException>(() => Caption("")).ParamName);
      Assert.Equal("text", Assert.Throws<ArgumentException>(() => Caption("   ")).ParamName);
    }
  }
}
