using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace Unrect.Analyzers
{
  /// <summary>
  /// The fix for <c>UNR001</c>: say it plainly.
  /// <para>
  /// Offered only where the swap is mechanical — a scope receiver becomes the
  /// <see cref="T:Unrect.Projections.Projection"/> class, and a witness argument is deleted. It is
  /// deliberately not offered for the two spellings where the fix is a decision rather than an edit:
  /// a member of <c>ProjectionBuilders&lt;TSpace&gt;</c> called through <c>using static</c> cannot be
  /// qualified without an import the file is forbidden to have (the plain vocabulary would be
  /// ambiguous with the closed one), and a scoped pipeline stage held in a variable is fixed where
  /// the variable is declared, not here.
  /// </para>
  /// </summary>
  [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnnecessaryScopeCodeFixProvider))]
  [Shared]
  public sealed class UnnecessaryScopeCodeFixProvider : CodeFixProvider
  {
    private const string Title = "Use the plain spelling";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
      = ImmutableArray.Create(UnrectDiagnostics.UnnecessaryScopeId);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
      var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
      var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

      if (root is null || model is null || UnrectSymbols.TryLoad(model.Compilation) is not UnrectSymbols symbols)
        return;

      foreach (var diagnostic in context.Diagnostics)
      {
        // The diagnostic sits on what is called, so the invocation is that node or its parent —
        // 'p.VerticalFlow' and a bare 'VerticalFlow' land on different sides of it.
        var located = root.FindNode(diagnostic.Location.SourceSpan);
        var invocation = located as InvocationExpressionSyntax ?? located.Parent as InvocationExpressionSyntax;

        if (invocation is null || Rewrite(invocation, model, symbols, context.CancellationToken) is not SyntaxNode replaced)
          continue;

        context.RegisterCodeFix(
          CodeAction.Create(
            Title,
            _ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(invocation, replaced))),
            equivalenceKey: Title),
          diagnostic);
      }
    }

    /// <summary>
    /// The invocation with the scope taken out of it, or null where taking it out is not a matter of
    /// syntax.
    /// </summary>
    private static InvocationExpressionSyntax? Rewrite(
      InvocationExpressionSyntax invocation,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken)
      => WithoutWitness(invocation, model, symbols, cancellationToken)
        ?? WithoutScopeReceiver(invocation, model, symbols, cancellationToken);

    /// <summary>
    /// <c>VerticalFlow(Formulas, v =&gt; …)</c> becomes <c>VerticalFlow(v =&gt; …)</c>: the witness is
    /// the whole of what the demanding overload says, so deleting it is the plain spelling.
    /// </summary>
    private static InvocationExpressionSyntax? WithoutWitness(
      InvocationExpressionSyntax invocation,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken)
    {
      var witness = invocation.ArgumentList.Arguments.FirstOrDefault(argument =>
        model.GetTypeInfo(argument.Expression, cancellationToken).Type is INamedTypeSymbol type
        && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, symbols.Demand));

      if (witness is null)
        return null;

      var remaining = invocation.ArgumentList.Arguments.Remove(witness);

      return invocation.WithArgumentList(invocation.ArgumentList.WithArguments(remaining));
    }

    /// <summary>
    /// <c>p.Overlay(…)</c> becomes <c>Projection.Overlay(…)</c>, and the same for a qualified member
    /// of the file-scoped vocabulary. Everything after the entry follows by type: the plain stages
    /// carry the same members as the scoped ones.
    /// </summary>
    private static InvocationExpressionSyntax? WithoutScopeReceiver(
      InvocationExpressionSyntax invocation,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken)
    {
      if (invocation.Expression is not MemberAccessExpressionSyntax access || !IsScopeReceiver(access.Expression))
        return null;

      return invocation.WithExpression(access.WithExpression(PlainVocabulary(access.Expression)));

      bool IsScopeReceiver(ExpressionSyntax receiver)
      {
        if (model.GetTypeInfo(receiver, cancellationToken).Type is INamedTypeSymbol type
          && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, symbols.Scope))
        {
          return true;
        }

        return model.GetSymbolInfo(receiver, cancellationToken).Symbol is INamedTypeSymbol named
          && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, symbols.Builders);
      }

      ExpressionSyntax PlainVocabulary(ExpressionSyntax receiver)
      {
        var name = model.LookupNamespacesAndTypes(receiver.SpanStart, name: "Projection")
          .Any(candidate => candidate is INamedTypeSymbol { ContainingNamespace.Name: "Projections" })
            ? "Projection"
            : "Unrect.Projections.Projection";

        return SyntaxFactory
          .ParseTypeName(name)
          .WithAdditionalAnnotations(Simplifier.Annotation)
          .WithTriviaFrom(receiver);
      }
    }
  }
}
