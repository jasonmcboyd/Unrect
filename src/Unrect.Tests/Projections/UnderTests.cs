using System;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// <c>Heading(a).Heading(b).Of(projection)</c> — the successor to <c>.Under</c> now that geometry is
  /// spelled only through the pipeline — is sugar for a vertical flow that reads the caption rows and
  /// then the projection, and it is sugar in the strongest sense: it builds a real flow with real
  /// children, so every caption is a node the engine places, the naming ladder labels, <c>Until</c>
  /// bounds, <c>Optional</c> tolerates, and the consumed-space meter counts.
  /// <para>
  /// The tests below are mostly about that claim. Nothing here is new machinery — what is worth
  /// pinning is that nothing had to be. (The suite keeps its name from the retired modifier; the
  /// behaviour it pins is <c>Heading</c>'s.)
  /// </para>
  /// </summary>
  public class UnderTests
  {
    // A junk row, a caption, two data rows.
    private static ISheetCells Sheet() => Mixed(new object?[,]
    {
      { "junk", null },
      { "Detail", null },
      { "a", 1 },
      { "b", 2 },
    });

    private static IProjection<ISheetCells, int> Lines() => Range(b => b.Height);

    // --- The desugared tree ---------------------------------------------------------------------------

    [Fact]
    public void HeadingIsAFlowThatDescribesItselfAsHeading()
    {
      // Not "VerticalFlow": a path segment should be greppable back to the line that produced it,
      // and the line says Heading.
      var section = Heading("Detail").Of(Lines());

      Assert.Equal("Heading", section.Description);
      Assert.False(section.IsWrapper);
      Assert.Null(section.Placement.Area);
    }

    [Fact]
    public void HeadingExposesItsCaptionsAndThenItsSection()
    {
      var section = Heading("Detail").Of(Lines());

      Assert.Null(section.Opacity);
      Assert.Equal(2, section.Children.Count);
      Assert.Equal("Caption(\"Detail\")", section.Children[0].Projection.Description);
      Assert.Equal("Range", section.Children[1].Projection.Description);
    }

    [Fact]
    public void EveryCaptionIsARealChildAndRendersAsItsOwnPathSegment()
    {
      // The whole point of desugaring rather than carrying the caption as an attribute: a caption
      // that cannot be found is reported as the child it is, at its own ordinal.
      var failure = Assert.Throws<ProjectionException>(() => Heading("Nope").Of(Lines()).Map(Sheet()));

      Assert.Equal("Heading -> Caption(\"Nope\")#1", failure.Path);
      Assert.Equal("Caption(\"Nope\")#1", failure.Subject);
    }

    [Fact]
    public void TheSectionIsTheChildAfterTheCaptions()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        Heading("Detail").Of(IntCell()).Map(Sheet()));

      Assert.Equal("Heading -> Integer#2", failure.Path);
    }

    // --- Value and extent ------------------------------------------------------------------------------

    [Fact]
    public void TheValueIsTheInnerProjections_AndTheCaptionsAreDiscarded()
    {
      // The captions are read — they must be, or nothing would be verified — but their text is not
      // what the section is for.
      Assert.Equal(2, Heading("Detail").Of(Lines()).Map(Sheet()));
    }

    [Fact]
    public void ConsumedIncludesTheCaptionRowsAndTheSeekThatFoundThem()
    {
      // Flow arithmetic, unchanged: along the axis the sum of the children's advances, and a
      // caption's advance includes the offset it seeked over. The junk row, the caption row and the
      // two data rows are all described.
      var applied = Heading("Detail").Of(Lines()).Apply(Sheet());

      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(4, applied.Consumed.Height);
    }

    [Fact]
    public void CaptionsAreReadInDeclarationOrder()
    {
      var space = Mixed(new object?[,] { { "Cap1" }, { "Cap2" }, { "a" }, { "b" } });

      Assert.Equal(2, Heading("Cap1").Heading("Cap2").Of(Lines()).Map(space));
    }

    [Fact]
    public void EachCaptionSeeksFromWhereThePreviousChildLeftOff()
    {
      // So a stacked pair reads adjacent rows, and a gap between two captions is absorbed by the
      // second one's own seek rather than needing a modifier.
      var space = Mixed(new object?[,] { { "Cap1" }, { null }, { "Cap2" }, { "a" } });

      Assert.Equal(1, Heading("Cap1").Heading("Cap2").Of(Lines()).Map(space));
    }

    // --- Labels ------------------------------------------------------------------------------------------

    [Fact]
    public void TheUseSiteLabelLandsOnTheFlow()
    {
      var section = Heading("Nope").Of(Lines());

      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v =>
        {
          var section2 = v.Next(section);

          return v.Build(read => read.Of(section2));
        }).Map(Sheet()));

      Assert.Equal("VerticalFlow -> 'section' -> Caption(\"Nope\")#1", failure.Path);
    }

    [Fact]
    public void NamingTheResultNamesTheFlow()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        Heading("Nope").Of(Lines()).Named("details").Map(Sheet()));

      Assert.Equal("'details' -> Caption(\"Nope\")#1", failure.Path);
    }

    [Fact]
    public void NoChildEverCarriesAnIdentifierFromInsideTheHelper()
    {
      // The helper-leak pin. Heading builds its flow from a loop over the minted captions and the
      // section itself; without an explicit opt-out the naming ladder would capture identifiers from
      // inside the library and label every section in every declaration by them — identifiers no user
      // ever wrote.
      var captionMiss = Assert.Throws<ProjectionException>(() => Heading("Nope").Of(Lines()).Map(Sheet()));
      var sectionMiss = Assert.Throws<ProjectionException>(() =>
        Heading("Detail").Of(IntCell()).Map(Sheet()));

      Assert.DoesNotContain("'caption'", captionMiss.Path);
      Assert.DoesNotContain("'projection'", sectionMiss.Path);

      // ...and what they render as instead is rung 3, the description and the ordinal.
      Assert.Contains("Caption(\"Nope\")#1", captionMiss.Path);
      Assert.Contains("Integer#2", sectionMiss.Path);
    }

    // --- Composition ---------------------------------------------------------------------------------------

    [Fact]
    public void MovementsApplyToTheFlowAndTheFirstCaptionSeeksFromThere()
    {
      var space = Mixed(new object?[,] { { "Detail" }, { "x" }, { "Detail" }, { "a" }, { "b" } });

      // Skipping the first two rows puts the second caption in range and the first out of it.
      Assert.Equal(2, Down(2).Heading("Detail").Of(Lines()).Map(space));
    }

    [Fact]
    public void UntilBoundsTheWholeSectionWithTheLandmarkSearchedBeforeTheCaptions()
    {
      // The investor-irr composition in miniature: the bound is measured in the extent the wrapper
      // is handed, so it ends the section where the next section's caption begins.
      var space = Mixed(new object?[,]
      {
        { "Detail" },
        { "a" },
        { "b" },
        { "Next Section" },
        { "c" },
      });

      var section = Until(RowContaining("Next Section")).Heading("Detail").Of(Lines());

      Assert.Equal(2, section.Map(space));
    }

    [Fact]
    public void OptionalAbsorbsAMissingCaptionAsAnAbsentSection()
    {
      var result = Heading("Nope").Of(Lines()).Optional().MapWithDiagnostics(Sheet());

      Assert.Equal(0, result.Value);

      var warning = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);

      Assert.Equal("Caption(\"Nope\")#1", warning.Subject);
      Assert.Equal("Heading -> Caption(\"Nope\")#1", warning.Path);
      Assert.Equal("A1", warning.Location.A1);
    }

    [Fact]
    public void AnAbsorbedSectionConsumesNothing()
    {
      var applied = Heading("Nope").Of(Lines()).Optional().Apply(Sheet());

      Assert.Equal(0, applied.Consumed.Width);
      Assert.Equal(0, applied.Consumed.Height);
    }

    [Fact]
    public void HeadingNestsWithTheOuterCaptionAbove()
    {
      // Like Padded: no merge is attempted, and reading order is preserved. Two ends are two
      // sections, so two headings above one section is spelled by nesting rather than chaining.
      var space = Mixed(new object?[,] { { "Outer" }, { "Inner" }, { "a" } });

      Assert.Equal(1, Heading("Outer").Of(Heading("Inner").Of(Lines())).Map(space));
    }

    // --- Guards ---------------------------------------------------------------------------------------------

    [Fact]
    public void HeadingRejectsABlankOrNullString()
    {
      // A heading takes the text a section announces itself by, so blank or null is not a heading.
      // (The old .Under guards — a null projection, a null or empty caption array — retired with the
      // postfix form; a heading is a string, and this is that string's whole contract. Whitespace is
      // blank too, so all three forms report the same ArgumentException on the text parameter.)
      Assert.Equal("text", Assert.Throws<ArgumentException>(() => Heading(null!)).ParamName);

      var empty = Assert.Throws<ArgumentException>(() => Heading(""));
      Assert.Equal("text", empty.ParamName);
      Assert.Contains("cannot be blank", empty.Message);

      Assert.Equal("text", Assert.Throws<ArgumentException>(() => Heading("   ")).ParamName);
    }
  }
}
