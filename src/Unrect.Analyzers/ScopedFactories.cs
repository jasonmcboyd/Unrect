using System.Collections.Generic;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unrect.Analyzers
{
  /// <summary>
  /// What counts as a factory that closes over a space, and how much of a declaration one of them
  /// speaks for.
  /// <para>
  /// Three spellings say the same thing — a scope member (<c>p.VerticalFlow(…)</c>), a member of the
  /// file-scoped vocabulary (<c>ProjectionBuilders&lt;TSpace&gt;</c>, imported or qualified), and a
  /// witness overload (<c>VerticalFlow(Formulas, v =&gt; …)</c>) — and a pipeline entered through any
  /// of them carries the space through every stage that follows, so the <em>chain</em>, not the
  /// invocation, is the unit a diagnostic reasons about.
  /// </para>
  /// </summary>
  internal static class ScopedFactories
  {
    /// <summary>
    /// The ascription. It is a promise the type system cannot check, so a demand it states is never
    /// evidence of a scope being unnecessary — and never evidence of one being necessary either.
    /// </summary>
    private const string Ascription = "Demanding";

    /// <summary>
    /// The space <paramref name="invocation"/> closes its result over, or null when it closes over
    /// nothing, over <c>ICellValues</c>, or over a type parameter — a generic helper is parameterized by
    /// its space, not scoped to one.
    /// </summary>
    public static ITypeSymbol? ClosedOver(
      InvocationExpressionSyntax invocation,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken = default)
    {
      if (model.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method)
        return null;

      return ClosedOver(method, symbols);
    }

    /// <inheritdoc cref="ClosedOver(InvocationExpressionSyntax, SemanticModel, UnrectSymbols, CancellationToken)"/>
    public static ITypeSymbol? ClosedOver(IMethodSymbol method, UnrectSymbols symbols)
    {
      if (method.Name == Ascription)
        return null;

      return Raised(ScopedReceiver(method, symbols), symbols) ?? Raised(Witness(method, symbols), symbols);
    }

    /// <summary>
    /// True when <paramref name="invocation"/> begins its chain — nothing to its left is a scoped
    /// factory of its own, so this is where the space enters the declaration.
    /// </summary>
    public static bool IsChainEntry(
      InvocationExpressionSyntax invocation,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken = default)
      => !(invocation.Expression is MemberAccessExpressionSyntax access
        && access.Expression is InvocationExpressionSyntax receiver
        && ClosedOver(receiver, model, symbols, cancellationToken) is object);

    /// <summary>
    /// The scoped invocations of the chain <paramref name="entry"/> begins, in order — the entry
    /// itself, then every stage member called on its result. The chain ends where the pipeline does:
    /// a postfix modifier is an extension over the finished projection, not a stage.
    /// </summary>
    public static IEnumerable<InvocationExpressionSyntax> Chain(
      InvocationExpressionSyntax entry,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken = default)
    {
      yield return entry;

      var current = entry;

      while (current.Parent is MemberAccessExpressionSyntax access
        && access.Expression == current
        && access.Parent is InvocationExpressionSyntax next
        && ClosedOver(next, model, symbols, cancellationToken) is object)
      {
        yield return next;
        current = next;
      }
    }

    /// <summary>The last invocation of the chain <paramref name="entry"/> begins.</summary>
    public static InvocationExpressionSyntax Outermost(
      InvocationExpressionSyntax entry,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken = default)
    {
      var last = entry;

      foreach (var link in Chain(entry, model, symbols, cancellationToken))
        last = link;

      return last;
    }

    private static ITypeSymbol? ScopedReceiver(IMethodSymbol method, UnrectSymbols symbols)
    {
      var containing = method.ContainingType;

      if (containing is null || containing.TypeArguments.Length != 1)
        return null;

      return IsScopedVocabulary(containing.OriginalDefinition, symbols) ? containing.TypeArguments[0] : null;
    }

    private static bool IsScopedVocabulary(INamedTypeSymbol definition, UnrectSymbols symbols)
    {
      if (SymbolEqualityComparer.Default.Equals(definition, symbols.Scope)
        || SymbolEqualityComparer.Default.Equals(definition, symbols.Builders))
      {
        return true;
      }

      for (var current = definition; current is object; current = current.BaseType?.OriginalDefinition)
      {
        if (SymbolEqualityComparer.Default.Equals(current, symbols.Stage))
          return true;
      }

      return false;
    }

    private static ITypeSymbol? Witness(IMethodSymbol method, UnrectSymbols symbols)
    {
      foreach (var parameter in method.Parameters)
      {
        if (parameter.Type is INamedTypeSymbol named
          && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, symbols.Demand))
        {
          return named.TypeArguments[0];
        }
      }

      return null;
    }

    /// <summary>
    /// The space, once the three non-answers are struck out: <c>ICellValues</c> raises nothing, a type
    /// parameter is a helper's own space rather than a scope, and an error type is a compilation
    /// already broken elsewhere.
    /// </summary>
    private static ITypeSymbol? Raised(ITypeSymbol? space, UnrectSymbols symbols)
      => space is null || space is ITypeParameterSymbol || space.TypeKind == TypeKind.Error || symbols.IsSpace(space)
        ? null
        : space;
  }
}
