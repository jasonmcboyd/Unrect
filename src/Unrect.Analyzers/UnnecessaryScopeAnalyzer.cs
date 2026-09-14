using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Unrect.Analyzers
{
  /// <summary>
  /// <c>UNR001</c>: a scope, or a witness, closing a declaration over a space that nothing in the
  /// declaration asks for.
  /// <para>
  /// The rule is one question asked of a whole declaration: <em>does any leaf, matcher or witness
  /// underneath this factory demand more than <c>ICellValues</c>?</em> A demand a scope manufactures does
  /// not count — that is the thing being questioned — so a nested scoped factory is looked
  /// <em>through</em> rather than at, and only the outermost scoped construction is reported. Fix
  /// that one and the next surfaces, the way an unused using does.
  /// </para>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public sealed class UnnecessaryScopeAnalyzer : DiagnosticAnalyzer
  {
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
      = ImmutableArray.Create(UnrectDiagnostics.UnnecessaryScope);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.EnableConcurrentExecution();

      context.RegisterCompilationStartAction(start =>
      {
        var symbols = UnrectSymbols.TryLoad(start.Compilation);

        if (symbols is null)
          return;

        start.RegisterSyntaxNodeAction(node => Analyze(node, symbols), SyntaxKind.InvocationExpression);
      });
    }

    private static void Analyze(SyntaxNodeAnalysisContext context, UnrectSymbols symbols)
    {
      var invocation = (InvocationExpressionSyntax)context.Node;
      var model = context.SemanticModel;
      var cancellationToken = context.CancellationToken;

      if (ScopedFactories.ClosedOver(invocation, model, symbols, cancellationToken) is not ITypeSymbol space)
        return;

      if (!ScopedFactories.IsChainEntry(invocation, model, symbols, cancellationToken))
        return;

      if (IsNested(invocation, model, symbols, cancellationToken) || Demands(invocation, model, symbols, cancellationToken))
        return;

      context.ReportDiagnostic(Diagnostic.Create(
        UnrectDiagnostics.UnnecessaryScope,
        invocation.Expression.GetLocation(),
        space.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    /// <summary>
    /// True when this declaration is built inside another scoped one. The scope is the enclosing
    /// declaration's, said once and inherited on purpose; questioning it here would ask a reader to
    /// qualify one branch of a file whose whole point is that it does not have to.
    /// </summary>
    private static bool IsNested(
      InvocationExpressionSyntax entry,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken)
      => ScopedFactories
        .Outermost(entry, model, symbols, cancellationToken)
        .Ancestors()
        .OfType<InvocationExpressionSyntax>()
        .Any(ancestor => ScopedFactories.ClosedOver(ancestor, model, symbols, cancellationToken) is object);

    /// <summary>
    /// True when something under the chain genuinely demands a capability: a demanding leaf, a
    /// demanding matcher, an ascription, or anything else whose own type names a space beyond
    /// <c>ICellValues</c> without a scope having put it there.
    /// </summary>
    private static bool Demands(
      InvocationExpressionSyntax entry,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken)
    {
      foreach (var link in ScopedFactories.Chain(entry, model, symbols, cancellationToken))
      {
        foreach (var argument in link.ArgumentList.Arguments)
        {
          // The witness argument is the scope, spelled as a value — the claim under question, not
          // evidence for it.
          if (IsWitness(argument.Expression, model, symbols, cancellationToken))
            continue;

          if (DemandsWithin(argument.Expression, model, symbols, cancellationToken))
            return true;
        }
      }

      return false;
    }

    private static bool DemandsWithin(
      SyntaxNode node,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken)
    {
      foreach (var expression in node.DescendantNodesAndSelf().OfType<ExpressionSyntax>())
      {
        cancellationToken.ThrowIfCancellationRequested();

        if (IsManufactured(expression, model, symbols, cancellationToken))
          continue;

        if (symbols.DemandsBeyondSpace(model.GetTypeInfo(expression, cancellationToken).Type))
          return true;
      }

      return false;
    }

    /// <summary>
    /// True when the demand this expression carries was put there by a scoped factory rather than by
    /// anything that reads a capability — the whole subtree of such a call is still walked, since a
    /// genuine demand can be nested inside one.
    /// </summary>
    private static bool IsManufactured(
      ExpressionSyntax expression,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken)
    {
      for (var current = expression; current is object;)
      {
        switch (current)
        {
          case InvocationExpressionSyntax invocation:
            if (ScopedFactories.ClosedOver(invocation, model, symbols, cancellationToken) is object)
              return true;

            current = invocation.Expression;
            break;

          case MemberAccessExpressionSyntax access:
            current = access.Expression;
            break;

          default:
            return false;
        }
      }

      return false;
    }

    private static bool IsWitness(
      ExpressionSyntax expression,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken)
      => model.GetTypeInfo(expression, cancellationToken).Type is INamedTypeSymbol type
        && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, symbols.Demand);
  }
}
