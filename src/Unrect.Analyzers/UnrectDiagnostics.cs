using Microsoft.CodeAnalysis;

namespace Unrect.Analyzers
{
  /// <summary>
  /// The two things the compiler cannot say about the space a declaration is written over, and the
  /// identifiers they say them under.
  /// <para>
  /// The space — the <c>TSpace</c> in <c>IProjection&lt;TSpace, T&gt;</c> — is checked by the type
  /// system and reported by messages written for type inference and conversion, which name either
  /// the wrong thing or nothing at all. These fill exactly those gaps and add no rule of their own:
  /// <c>UNR002</c> is a fix on the compiler's own conversion error rather than a diagnostic, and
  /// <c>UNR003</c> speaks the sentence behind a composition or a <c>Map</c> the compiler refuses.
  /// </para>
  /// </summary>
  internal static class UnrectDiagnostics
  {
    /// <summary>The category every diagnostic here reports under.</summary>
    public const string Category = "Unrect.Usage";

    /// <summary>
    /// Retired, and named here so the number stays spent. <c>UNR001</c> was the unnecessary-scope
    /// rule, which reported a scope closed over a space nothing underneath it demanded. There are no
    /// scopes to close: a file names its space once, in its <c>using static</c>, and a declaration
    /// that demands less of it is exactly what contravariance in a helper's constraint is for.
    /// </summary>
    public const string RetiredUnnecessaryScopeId = "UNR001";

    /// <summary>
    /// The identifier reserved for the demand door — the code fix on the compiler's CS1503, which
    /// reports no diagnostic of its own and therefore has no descriptor and no release-tracking
    /// entry. It is named here so the number is spent and cannot be handed to a second rule.
    /// </summary>
    public const string DemandDoorId = "UNR002";

    /// <summary>The identifier of <see cref="DemandsExceedOffer"/>.</summary>
    public const string DemandsExceedOfferId = "UNR003";

    /// <summary>
    /// A <c>Map</c>/<c>Apply</c>/<c>MapWithDiagnostics</c> whose projection demands a space the
    /// argument does not offer. The compiler refuses the call as a failure of type inference and
    /// names neither side; this names both.
    /// </summary>
    public static readonly DiagnosticDescriptor DemandsExceedOffer = new DiagnosticDescriptor(
      DemandsExceedOfferId,
      "Demands exceed offer",
      "this projection demands '{0}'; this space offers '{1}'",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description:
        "Applying a declaration to a space it out-demands is a compile error already — but the compiler "
        + "reports it as an inference failure, which names no capability and points at the method rather "
        + "than at the two types that disagree. Either read the file through a door that offers the "
        + "capability, or write the declaration over the space that door hands back.");
  }
}
