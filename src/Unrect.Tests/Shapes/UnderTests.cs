using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.Shapes.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// <c>shape.Under(a, b)</c> is sugar for a vertical flow that reads the captions and then the
  /// shape, and it is sugar in the strongest sense: it desugars to a real flow with real children,
  /// so every caption is a node the engine places, the naming ladder labels, <c>Until</c> bounds,
  /// <c>Optional</c> tolerates, and the consumed-space meter counts.
  /// <para>
  /// The tests below are mostly about that claim. Nothing here is new machinery — what is worth
  /// pinning is that nothing had to be.
  /// </para>
  /// </summary>
  public class UnderTests
  {
    // A junk row, a caption, two data rows.
    private static ISpace Sheet() => Mixed(new object?[,]
    {
      { "junk", null },
      { "Detail", null },
      { "a", 1 },
      { "b", 2 },
    });

    private static IShape<int> Lines() => Range(b => b.Height);

    // --- The desugared tree ---------------------------------------------------------------------------

    [Fact]
    public void UnderIsAFlowThatDescribesItselfByWhatTheUserTyped()
    {
      // Not "VerticalFlow": a path segment should be greppable back to the line that produced it,
      // and the line says .Under.
      var section = Lines().Under(Caption("Detail"));

      Assert.Equal("Under", section.Description);
      Assert.False(section.IsTransparent);
      Assert.Null(section.Placement.Area);
    }

    [Fact]
    public void UnderIsOpaqueLikeEveryOtherCursorComposite()
    {
      var section = Lines().Under(Caption("Detail"));

      var marker = Assert.IsAssignableFrom<IOpaqueComposite>(section);

      Assert.Empty(section.Children);
      Assert.Equal("declared by a cursor lambda; children are known only while it runs", marker.Reason);
    }

    [Fact]
    public void EveryCaptionIsARealChildAndRendersAsItsOwnPathSegment()
    {
      // The whole point of desugaring rather than carrying the caption as an attribute: a caption
      // that cannot be found is reported as the child it is, at its own ordinal.
      var failure = Assert.Throws<ShapeException>(() => Lines().Under(Caption("Nope")).Map(Sheet()));

      Assert.Equal("Under -> Caption(\"Nope\")#1", failure.Path);
      Assert.Equal("Caption(\"Nope\")#1", failure.Subject);
    }

    [Fact]
    public void TheSectionIsTheChildAfterTheCaptions()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Cell(c => c.GetInt()).Under(Caption("Detail")).Map(Sheet()));

      Assert.Equal("Under -> Cell#2", failure.Path);
    }

    // --- Value and extent ------------------------------------------------------------------------------

    [Fact]
    public void TheValueIsTheInnerShapes_AndTheCaptionsAreDiscarded()
    {
      // The captions are read — they must be, or nothing would be verified — but their text is not
      // what the section is for.
      Assert.Equal(2, Lines().Under(Caption("Detail")).Map(Sheet()));
    }

    [Fact]
    public void ConsumedIncludesTheCaptionRowsAndTheSeekThatFoundThem()
    {
      // Flow arithmetic, unchanged: along the axis the sum of the children's advances, and a
      // caption's advance includes the offset it seeked over. The junk row, the caption row and the
      // two data rows are all described.
      var applied = Lines().Under(Caption("Detail")).Apply(Sheet());

      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(4, applied.Consumed.Height);
    }

    [Fact]
    public void CaptionsAreReadInDeclarationOrder()
    {
      var space = Mixed(new object?[,] { { "Cap1" }, { "Cap2" }, { "a" }, { "b" } });

      Assert.Equal(2, Lines().Under(Caption("Cap1"), Caption("Cap2")).Map(space));
    }

    [Fact]
    public void EachCaptionSeeksFromWhereThePreviousChildLeftOff()
    {
      // So a stacked pair reads adjacent rows, and a gap between two captions is absorbed by the
      // second one's own seek rather than needing a modifier.
      var space = Mixed(new object?[,] { { "Cap1" }, { null }, { "Cap2" }, { "a" } });

      Assert.Equal(1, Lines().Under(Caption("Cap1"), Caption("Cap2")).Map(space));
    }

    // --- Labels ------------------------------------------------------------------------------------------

    [Fact]
    public void TheUseSiteLabelLandsOnTheFlow()
    {
      var section = Lines().Under(Caption("Nope"));

      var failure = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v => v.Next(section)).Map(Sheet()));

      Assert.Equal("VerticalFlow -> 'section' -> Caption(\"Nope\")#1", failure.Path);
    }

    [Fact]
    public void NamingTheResultNamesTheFlow()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Lines().Under(Caption("Nope")).Named("details").Map(Sheet()));

      Assert.Equal("'details' -> Caption(\"Nope\")#1", failure.Path);
    }

    [Fact]
    public void NoChildEverCarriesAnIdentifierFromInsideTheHelper()
    {
      // The helper-leak pin. .Under builds its flow from a loop over `caption` and a parameter
      // called `shape`; without an explicit opt-out the naming ladder would capture those and label
      // every section in every declaration 'caption' and 'shape' — identifiers no user ever wrote.
      var captionMiss = Assert.Throws<ShapeException>(() => Lines().Under(Caption("Nope")).Map(Sheet()));
      var sectionMiss = Assert.Throws<ShapeException>(() =>
        Cell(c => c.GetInt()).Under(Caption("Detail")).Map(Sheet()));

      Assert.DoesNotContain("'caption'", captionMiss.Path);
      Assert.DoesNotContain("'shape'", sectionMiss.Path);

      // ...and what they render as instead is rung 3, the description and the ordinal.
      Assert.Contains("Caption(\"Nope\")#1", captionMiss.Path);
      Assert.Contains("Cell#2", sectionMiss.Path);
    }

    // --- Composition ---------------------------------------------------------------------------------------

    [Fact]
    public void MovementsApplyToTheFlowAndTheFirstCaptionSeeksFromThere()
    {
      var space = Mixed(new object?[,] { { "Detail" }, { "x" }, { "Detail" }, { "a" }, { "b" } });

      // Skipping the first two rows puts the second caption in range and the first out of it.
      Assert.Equal(2, Lines().Under(Caption("Detail")).Down(2).Map(space));
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

      var section = Lines().Under(Caption("Detail")).Until(RowContaining("Next Section"));

      Assert.Equal(2, section.Map(space));
    }

    [Fact]
    public void OptionalAbsorbsAMissingCaptionAsAnAbsentSection()
    {
      var result = Lines().Under(Caption("Nope")).Optional().MapWithDiagnostics(Sheet());

      Assert.Equal(0, result.Value);

      var warning = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);

      Assert.Equal("Caption(\"Nope\")#1", warning.Subject);
      Assert.Equal("Under -> Caption(\"Nope\")#1", warning.Path);
      Assert.Equal("A1", warning.Location.A1);
    }

    [Fact]
    public void AnAbsorbedSectionConsumesNothing()
    {
      var applied = Lines().Under(Caption("Nope")).Optional().Apply(Sheet());

      Assert.Equal(0, applied.Consumed.Width);
      Assert.Equal(0, applied.Consumed.Height);
    }

    [Fact]
    public void UnderNestsWithTheOuterCaptionAbove()
    {
      // Like Padded: no merge is attempted, and reading order is preserved.
      var space = Mixed(new object?[,] { { "Outer" }, { "Inner" }, { "a" } });

      Assert.Equal(1, Lines().Under(Caption("Inner")).Under(Caption("Outer")).Map(space));
    }

    // --- Guards ---------------------------------------------------------------------------------------------

    [Fact]
    public void UnderRejectsADeclarationItCannotHonour()
    {
      var captions = new IShape<string>[] { Caption("a") };

      Assert.Equal("shape", Assert.Throws<ArgumentNullException>(() => ((IShape<int>)null!).Under(captions)).ParamName);
      Assert.Equal("captions", Assert.Throws<ArgumentNullException>(() => Lines().Under(null!)).ParamName);

      var empty = Assert.Throws<ArgumentException>(() => Lines().Under());
      Assert.Equal("captions", empty.ParamName);
      Assert.Contains("at least one caption", empty.Message);

      var missing = Assert.Throws<ArgumentException>(() => Lines().Under(Caption("a"), null!));
      Assert.Equal("captions", missing.ParamName);
      Assert.Contains("Caption 2 is null", missing.Message);
    }

    [Fact]
    public void UnderCopiesTheArrayItWasHanded()
    {
      // params may hand us the caller's own array, and the lambda is captured for every future
      // application. A shape that could change underneath its user is not a declaration.
      var captions = new[] { Caption("Detail") };

      var section = Lines().Under(captions);

      captions[0] = Caption("Nope");

      Assert.Equal(2, section.Map(Sheet()));
    }
  }
}
