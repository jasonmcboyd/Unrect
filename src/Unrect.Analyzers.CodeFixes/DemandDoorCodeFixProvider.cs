using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unrect.Analyzers
{
  /// <summary>
  /// <c>UNR002</c>, the demand door — a fix on the compiler's CS1503 rather than a diagnostic of its
  /// own, because the compiler has already said something true and the only thing missing is what to
  /// do about it.
  /// <para>
  /// The message reads <c>cannot convert from 'IProjection&lt;IFormulaSpace, string?&gt;' to
  /// 'IProjection&lt;ISpace, string?&gt;'</c>, which names both types and still leaves the reader to
  /// work out that the fix belongs two lines up, on the factory whose lambda this child sits in. A
  /// factory is where a demand enters a declaration; if a scope over a space that answers the demand
  /// is already in hand, this offers to build the factory through it.
  /// </para>
  /// </summary>
  [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DemandDoorCodeFixProvider))]
  [Shared]
  public sealed class DemandDoorCodeFixProvider : CodeFixProvider
  {
    /// <summary>The compiler's "cannot convert from X to Y" — an argument the receiver's type refuses.</summary>
    private const string ArgumentConversion = "CS1503";

    /// <summary>
    /// The factories a demand travels through. Everything else in the vocabulary is indifferent to
    /// the space and composes into a scoped declaration by variance, so no other member has a scoped
    /// spelling for this fix to reach for.
    /// </summary>
    private static readonly ImmutableHashSet<string> Composing = ImmutableHashSet.Create(
      "VerticalFlow",
      "HorizontalFlow",
      "Overlay",
      "Table",
      "VerticalRepeat",
      "HorizontalRepeat",
      "Choice");

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(ArgumentConversion);

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
        Offer(context, root, model, symbols, diagnostic);
      }
    }

    private static void Offer(
      CodeFixContext context,
      SyntaxNode root,
      SemanticModel model,
      UnrectSymbols symbols,
      Diagnostic diagnostic)
    {
      var cancellationToken = context.CancellationToken;

      if (root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<ArgumentSyntax>() is not ArgumentSyntax child
        || child.Parent?.Parent is not InvocationExpressionSyntax refused)
      {
        return;
      }

      if (symbols.DemandOf(model.GetTypeInfo(child.Expression, cancellationToken).Type) is not ITypeSymbol demanded
        || symbols.IsSpace(demanded))
      {
        return;
      }

      if (Factory(refused, model, symbols, cancellationToken) is not InvocationExpressionSyntax factory
        || MemberName(factory) is not SimpleNameSyntax member
        || !Composing.Contains(member.Identifier.ValueText))
      {
        return;
      }

      if (InScope(model, factory.SpanStart, demanded, symbols) is not string scope)
        return;

      var name = member.Identifier.ValueText;

      var title =
        $"Use '{scope}.{name}' here — this child demands "
        + $"'{demanded.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}', "
        + $"which a '{name}' declared over "
        + $"'{Declared(factory, model, symbols, cancellationToken)}' cannot carry";

      var through = factory.WithExpression(SyntaxFactory.MemberAccessExpression(
        SyntaxKind.SimpleMemberAccessExpression,
        SyntaxFactory.IdentifierName(scope),
        member.WithoutTrivia()).WithTriviaFrom(factory.Expression));

      context.RegisterCodeFix(
        CodeAction.Create(
          title,
          _ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(factory, through))),
          equivalenceKey: nameof(DemandDoorCodeFixProvider)),
        diagnostic);
    }

    /// <summary>
    /// The invocation the demand should have entered through: the refused call itself, or — when the
    /// refusal is a child handed to a layout's cursor — the factory whose lambda that cursor belongs
    /// to.
    /// </summary>
    private static InvocationExpressionSyntax? Factory(
      InvocationExpressionSyntax refused,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken)
    {
      if (!IsCursorNext(refused, model, symbols, cancellationToken))
        return refused;

      return refused.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>() is AnonymousFunctionExpressionSyntax layout
        && layout.Parent?.Parent?.Parent is InvocationExpressionSyntax factory
          ? factory
          : null;
    }

    private static bool IsCursorNext(
      InvocationExpressionSyntax invocation,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken)
      => invocation.Expression is MemberAccessExpressionSyntax access
        && access.Name.Identifier.ValueText == "Next"
        && model.GetTypeInfo(access.Expression, cancellationToken).Type is INamedTypeSymbol cursor
        && symbols.IsCursor(cursor);

    /// <summary>The space the refused factory is declared over, for the fix's sentence.</summary>
    private static string Declared(
      InvocationExpressionSyntax factory,
      SemanticModel model,
      UnrectSymbols symbols,
      CancellationToken cancellationToken)
    {
      var space = ScopedFactories.ClosedOver(factory, model, symbols, cancellationToken) ?? symbols.Space;

      return space.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
    }

    /// <summary>
    /// A scope in hand at <paramref name="position"/> over a space that answers
    /// <paramref name="demanded"/>. Nothing is invented: a scope is only ever suggested where the
    /// reader already opened one.
    /// </summary>
    private static string? InScope(
      SemanticModel model,
      int position,
      ITypeSymbol demanded,
      UnrectSymbols symbols)
    {
      foreach (var symbol in model.LookupSymbols(position))
      {
        if (TypeOf(symbol) is INamedTypeSymbol scope
          && SymbolEqualityComparer.Default.Equals(scope.OriginalDefinition, symbols.Scope)
          && UnrectSymbols.Satisfies(scope.TypeArguments[0], demanded))
        {
          return symbol.Name;
        }
      }

      return null;
    }

    /// <summary>What a symbol holds, for the four kinds of thing a scope is ever held in.</summary>
    private static ITypeSymbol? TypeOf(ISymbol symbol)
      => symbol switch
      {
        ILocalSymbol local => local.Type,
        IParameterSymbol parameter => parameter.Type,
        IFieldSymbol field => field.Type,
        IPropertySymbol property => property.Type,
        _ => null,
      };

    private static SimpleNameSyntax? MemberName(InvocationExpressionSyntax invocation)
      => invocation.Expression switch
      {
        MemberAccessExpressionSyntax access => access.Name,
        SimpleNameSyntax name => name,
        _ => null,
      };
  }
}
