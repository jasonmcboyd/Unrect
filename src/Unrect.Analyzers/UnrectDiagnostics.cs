using Microsoft.CodeAnalysis;

namespace Unrect.Analyzers
{
  /// <summary>
  /// The three things the compiler cannot say about a demand, and the identifiers they say them
  /// under.
  /// <para>
  /// A demand — the <c>TSpace</c> in <c>IProjection&lt;TSpace, T&gt;</c> — is checked by the type
  /// system and reported by messages written for type inference, which name either the wrong thing
  /// or nothing at all. These diagnostics fill exactly those gaps and add no rule of their own:
  /// <c>UNR001</c> says a scope is carrying a demand nobody makes, <c>UNR002</c> is a fix on the
  /// compiler's own conversion error rather than a diagnostic, and <c>UNR003</c> speaks the sentence
  /// behind an inference failure at <c>Map</c>.
  /// </para>
  /// </summary>
  internal static class UnrectDiagnostics
  {
    /// <summary>The category every diagnostic here reports under.</summary>
    public const string Category = "Unrect.Usage";

    /// <summary>The identifier of <see cref="UnnecessaryScope"/>.</summary>
    public const string UnnecessaryScopeId = "UNR001";

    /// <summary>
    /// The identifier reserved for the demand door — the code fix on the compiler's CS1503, which
    /// reports no diagnostic of its own and therefore has no descriptor and no release-tracking
    /// entry. It is named here so the number is spent and cannot be handed to a second rule.
    /// </summary>
    public const string DemandDoorId = "UNR002";

    /// <summary>The identifier of <see cref="DemandsExceedOffer"/>.</summary>
    public const string DemandsExceedOfferId = "UNR003";

    /// <summary>
    /// A scope, or a witness, closing a factory over a space that nothing underneath it asks for.
    /// The IDE0005 of scopes: the declaration compiles and runs, and the only cost is that it now
    /// refuses spaces it never needed — which is a cost a hoisted helper passes to every caller.
    /// </summary>
    public static readonly DiagnosticDescriptor UnnecessaryScope = new DiagnosticDescriptor(
      UnnecessaryScopeId,
      "Unnecessary scope",
      "nothing here demands '{0}' — the plain spelling serves, and a scope should mark the doors a demand passes through",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description:
        "A scope raises everything built through it to its space whether the children needed it or not. "
        + "Where no leaf, matcher or witness underneath the declaration demands anything beyond ICellValues, "
        + "the scoped spelling states a requirement the declaration does not have — and a helper written "
        + "that way quietly demands more than it reads.");

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
        + "than at the two types that disagree. Either open the door that offers the capability, or drop "
        + "the demand from the declaration.");
  }
}
