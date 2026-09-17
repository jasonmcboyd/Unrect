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
    /// <summary>
    /// UNR001 was the unnecessary-scope rule and retired with the scopes it reported on: a file names
    /// its space once, in its <c>using static</c>, and there is nothing left to call unnecessary. The
    /// number stays spent so it cannot be handed to a second rule — which is the only thing left to
    /// assert about it.
    /// </summary>
    [Fact]
    public void The_retired_scope_rule_spends_its_identifier_and_nothing_else()
    {
      Assert.Equal("UNR001", UnrectDiagnostics.RetiredUnnecessaryScopeId);
      Assert.DoesNotContain(Descriptors(), descriptor => descriptor.Id == UnrectDiagnostics.RetiredUnnecessaryScopeId);
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

    /// <summary>
    /// The one fact both analyzers are built on, asserted against the assemblies this solution just
    /// built: the types they look up are still there under the names they look them up by.
    /// <para>
    /// This is the dark-analyzer guard. Every rule here exits first on
    /// <c>UnrectSymbols.TryLoad</c> returning null, which is correct for a compilation that does not
    /// reference Unrect and catastrophic for one that does: a renamed Core interface — which is
    /// exactly what phase 2 did to <c>ISpace</c> — turns both rules off and leaves every
    /// silence-asserting test passing for the wrong reason. A name is not a contract the compiler
    /// checks here, because these are strings; so it is checked here.
    /// </para>
    /// </summary>
    [Fact]
    public void The_types_the_rules_look_up_resolve_against_the_assemblies_they_ship_beside()
    {
      Assert.NotNull(typeof(Core.ISpace).FullName);
      Assert.Equal("Unrect.Core.ISpace", typeof(Core.ISpace).FullName);
      Assert.Equal("Unrect.Projections.IProjectionDefinition`2", typeof(Projections.IProjectionDefinition<,>).FullName);
      Assert.Equal("Unrect.Projections.ProjectionBuilders`1", typeof(Projections.ProjectionBuilders<>).FullName);
      Assert.Equal("Unrect.Projections.PlacementStage`1", typeof(Projections.PlacementStage<>).FullName);
      Assert.Equal("Unrect.Projections.IRowLandmark`1", typeof(Projections.IRowLandmark<>).FullName);
      Assert.Equal("Unrect.Projections.IColumnLandmark`1", typeof(Projections.IColumnLandmark<>).FullName);
      Assert.Equal("Unrect.Projections.LayoutCursor`1", typeof(Projections.LayoutCursor<>).FullName);
    }

    private static DiagnosticDescriptor[] Descriptors()
      => new DemandsExceedOfferAnalyzer().SupportedDiagnostics.ToArray();

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
