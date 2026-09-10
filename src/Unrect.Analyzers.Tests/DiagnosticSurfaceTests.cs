using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;

using Xunit;

namespace Unrect.Analyzers.Tests
{
  /// <summary>
  /// The sentences themselves. With these diagnostics the message IS the product — each one exists
  /// because a compiler message said the wrong thing or nothing — so the words are pinned here
  /// rather than left to whatever a refactor leaves behind, and so is their presence in the
  /// release-tracking file that holds them to a version.
  /// </summary>
  public class DiagnosticSurfaceTests
  {
    [Fact]
    public void The_unnecessary_scope_message_is_the_librarys_sentence()
    {
      Assert.Equal("UNR001", UnrectDiagnostics.UnnecessaryScope.Id);
      Assert.Equal("Unnecessary scope", UnrectDiagnostics.UnnecessaryScope.Title.ToString());
      Assert.Equal(
        "nothing here demands '{0}' — the plain spelling serves, and a scope should mark the doors a demand passes through",
        UnrectDiagnostics.UnnecessaryScope.MessageFormat.ToString());
      Assert.Equal(DiagnosticSeverity.Warning, UnrectDiagnostics.UnnecessaryScope.DefaultSeverity);
      Assert.Equal("Unrect.Usage", UnrectDiagnostics.UnnecessaryScope.Category);
    }

    [Fact]
    public void The_demands_exceed_offer_message_names_both_spaces()
    {
      Assert.Equal("UNR003", UnrectDiagnostics.DemandsExceedOffer.Id);
      Assert.Equal("Demands exceed offer", UnrectDiagnostics.DemandsExceedOffer.Title.ToString());
      Assert.Equal(
        "this projection demands '{0}'; this space offers '{1}'",
        UnrectDiagnostics.DemandsExceedOffer.MessageFormat.ToString());
      Assert.Equal(DiagnosticSeverity.Warning, UnrectDiagnostics.DemandsExceedOffer.DefaultSeverity);
      Assert.Equal("Unrect.Usage", UnrectDiagnostics.DemandsExceedOffer.Category);
    }

    /// <summary>
    /// UNR002 is a fix on the compiler's own diagnostic and reports nothing of its own, so it has no
    /// descriptor and no release-tracking entry — the identifier is reserved so that the number
    /// cannot be handed to a second rule.
    /// </summary>
    [Fact]
    public void The_demand_door_reserves_its_identifier_without_spending_a_descriptor()
    {
      Assert.Equal("UNR002", UnrectDiagnostics.DemandDoorId);
      Assert.Equal("CS1503", Assert.Single(new DemandDoorCodeFixProvider().FixableDiagnosticIds));
    }

    [Fact]
    public void Every_descriptor_is_tracked_as_an_unshipped_rule()
    {
      var tracked = File.ReadAllText(Path.Combine(Solution(), "Unrect.Analyzers", "AnalyzerReleases.Unshipped.md"));

      foreach (var descriptor in Descriptors())
        Assert.Contains($"{descriptor.Id} | {descriptor.Category} | {descriptor.DefaultSeverity} |", tracked);
    }

    private static DiagnosticDescriptor[] Descriptors()
      => new UnnecessaryScopeAnalyzer().SupportedDiagnostics
        .Concat(new DemandsExceedOfferAnalyzer().SupportedDiagnostics)
        .ToArray();

    /// <summary>The src directory, four levels above the test assembly's bin/Debug/net8.0.</summary>
    private static string Solution()
      => Path.GetFullPath(Path.Combine(
        Path.GetDirectoryName(typeof(DiagnosticSurfaceTests).Assembly.Location)!,
        "..",
        "..",
        "..",
        ".."));
  }
}
