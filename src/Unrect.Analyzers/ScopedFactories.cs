using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unrect.Analyzers
{
  /// <summary>
  /// What counts as a factory closed over a space, and which space it is closed over.
  /// <para>
  /// Two spellings say the same thing — a member of the file-scoped vocabulary
  /// (<c>ProjectionBuilders&lt;TSpace&gt;</c>, imported or qualified) and a member of a pipeline
  /// stage built from one — so both answer here.
  /// </para>
  /// </summary>
  internal static class ScopedFactories
  {
    /// <summary>
    /// The space <paramref name="invocation"/> closes its result over, or null when it closes over
    /// nothing, over <c>ISpace</c>, or over a type parameter — a generic helper is parameterized by
    /// its space, not scoped to one.
    /// </summary>
    public static ITypeSymbol? ClosedOver(
      InvocationExpressionSyntax invocation,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken = default)
    {
      var info = model.GetSymbolInfo(invocation, cancellationToken);

      // The candidate matters as much as the symbol: this is asked about calls the compiler REFUSED,
      // where nothing bound and the member the reader wrote is the candidate it turned away. Taking
      // the first is safe because the answer does not depend on which: the receiver fixes the
      // constructed ProjectionBuilders<TSpace> or stage type, so every candidate of one refused call
      // is a member of that same type and names that same space.
      var method = (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) as IMethodSymbol;

      return method is null ? null : ClosedOver(method, symbols);
    }

    /// <inheritdoc cref="ClosedOver(InvocationExpressionSyntax, SemanticModel, UnrectSymbols, CancellationToken)"/>
    public static ITypeSymbol? ClosedOver(IMethodSymbol method, UnrectSymbols symbols)
      => Raised(ScopedReceiver(method, symbols), symbols);

    private static ITypeSymbol? ScopedReceiver(IMethodSymbol method, UnrectSymbols symbols)
    {
      var containing = method.ContainingType;

      if (containing is null || containing.TypeArguments.Length != 1)
        return null;

      return IsScopedVocabulary(containing.OriginalDefinition, symbols) ? containing.TypeArguments[0] : null;
    }

    private static bool IsScopedVocabulary(INamedTypeSymbol definition, UnrectSymbols symbols)
    {
      if (SymbolEqualityComparer.Default.Equals(definition, symbols.Builders))
        return true;

      for (var current = definition; current is object; current = current.BaseType?.OriginalDefinition)
      {
        if (SymbolEqualityComparer.Default.Equals(current, symbols.Stage))
          return true;
      }

      return false;
    }

    /// <summary>
    /// The space, once the three non-answers are struck out: <c>ISpace</c> raises nothing, a type
    /// parameter is a helper's own space rather than a scope, and an error type is a compilation
    /// already broken elsewhere.
    /// </summary>
    private static ITypeSymbol? Raised(ITypeSymbol? space, UnrectSymbols symbols)
      => space is null || space is ITypeParameterSymbol || space.TypeKind == TypeKind.Error || symbols.IsSpace(space)
        ? null
        : space;
  }
}
