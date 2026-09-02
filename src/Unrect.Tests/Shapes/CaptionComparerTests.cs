using System;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// The comparer that lets a caption in a file bind to a member in code: case and whitespace are
  /// noise between the two, so <c>"Contribution ITD"</c> and <c>ContributionItd</c> are the same
  /// name written for two different audiences.
  /// <para>
  /// It is deliberately narrow. It is a <em>binding</em> rule — captions to members — and the last
  /// test here is the one that matters most: it must never become the rule for matching content,
  /// where a user writing a literal means that literal.
  /// </para>
  /// </summary>
  public class CaptionComparerTests
  {
    private static bool Same(string caption, string member) => CaptionComparer.Default.Equals(caption, member);

    [Theory]
    [InlineData("Contribution ITD", "ContributionItd")]
    [InlineData("IRR", "Irr")]
    [InlineData("End Balance", "EndBalance")]
    [InlineData("Investor Name", "InvestorName")]
    [InlineData("  Amount  ", "Amount")]
    [InlineData("Transaction  Date", "TransactionDate")]
    [InlineData("transaction date", "TransactionDate")]
    [InlineData("NET", "net")]
    public void CaseAndWhitespaceAreNoiseBetweenACaptionAndAMember(string caption, string member)
    {
      Assert.True(Same(caption, member));
    }

    [Theory]
    [InlineData("Net (USD)", "NetUsd")]
    [InlineData("Net Amount", "NetIncome")]
    [InlineData("Amount", "Amounts")]
    [InlineData("Date", "TransactionDate")]
    public void AnythingElseIsADifferentName(string caption, string member)
    {
      // Punctuation is not noise: "Net (USD)" and NetUsd are a judgement call the user should make
      // explicitly, with Column(...), rather than one the comparer makes for them.
      Assert.False(Same(caption, member));
    }

    [Theory]
    [InlineData("Contribution ITD", "ContributionItd")]
    [InlineData("IRR", "Irr")]
    [InlineData("End Balance", "EndBalance")]
    [InlineData("  Amount  ", "Amount")]
    public void EqualCaptionsHashAlike(string caption, string member)
    {
      // Required of any comparer used as a dictionary's, which is exactly what this one is.
      Assert.Equal(
        CaptionComparer.Default.GetHashCode(caption),
        CaptionComparer.Default.GetHashCode(member));
    }

    // --- Edges ------------------------------------------------------------------------------------------

    [Fact]
    public void NullIsHandledRatherThanThrown()
    {
      // A header cell can be absent, so the comparer is asked about null in normal use.
      Assert.False(Same(null!, "Amount"));
      Assert.False(Same("Amount", null!));
      Assert.True(CaptionComparer.Default.Equals(null, null));
    }

    [Fact]
    public void AnEmptyCaptionEqualsAnAllWhitespaceOne_WhichIsWhyNeitherMayBindAColumn()
    {
      // The non-obvious consequence of "whitespace is noise": with every space removed, "" and
      // "   " are the same string. That is consistent, but it means the comparer cannot be what
      // rejects an uncaptioned column — a blank header and a spaces-only header are one name to it,
      // and both are nameless. The table's own D2 guard is what refuses them, before the comparer
      // is ever consulted.
      Assert.True(Same("", "   "));
      Assert.True(Same("  ", "\t"));

      var space = Mixed(new object?[,] { { "Amount", "   " }, { 1, 2 } });

      Assert.Throws<ShapeException>(() => TableRows().Map(space));
    }

    [Fact]
    public void GetHashCodeRejectsNullTheWayTheFrameworksComparersDo()
    {
      Assert.Throws<ArgumentNullException>(() => CaptionComparer.Default.GetHashCode(null!));
    }

    [Fact]
    public void TheEmptyAndWhitespaceCaptionsHashAlikeToo()
    {
      // Equality and hashing must agree even on the pair nobody expects to be equal.
      Assert.True(Same("", "   "));
      Assert.Equal(CaptionComparer.Default.GetHashCode(""), CaptionComparer.Default.GetHashCode("   "));
    }

    // --- The pin that keeps the comparer where it belongs ---------------------------------------------

    [Fact]
    public void TheBindingComparerDoesNotLeakIntoContentMatching()
    {
      // A user who writes RowContaining("Net Income") means those two words with that space. If the
      // binding comparer reached content matching, a sheet reading "NetIncome" would silently
      // satisfy it — and so would a dozen other things nobody asked for.
      var space = Mixed(new object?[,] { { "NetIncome" } });

      Assert.Throws<ShapeException>(() => Cell(c => c.GetString()).After(To(RowContaining("Net Income"))).Map(space));
      Assert.Throws<ShapeException>(() => Caption("Net Income").Map(space));

      // ...while the comparer itself would have said yes, which is the whole point of the pin.
      Assert.True(Same("Net Income", "NetIncome"));
    }

    [Fact]
    public void ACaptionDoesNotAbsorbATrailingColonTheWayAFieldLabelDoes()
    {
      // Colon-tolerance is LabelEquals, and LabelEquals is Fields-only: a caption is a whole cell
      // value and a label is half of a labelled pair, so they match by different rules.
      var space = Mixed(new object?[,] { { "EIN:" } });

      Assert.Throws<ShapeException>(() => Caption("EIN").Map(space));
      Assert.Equal("EIN:", Caption("EIN:").Map(space));
    }
  }
}
