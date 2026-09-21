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
  /// The message reads <c>cannot convert from 'IProjectionDefinition&lt;IFormulaSpace, string?&gt;' to
  /// 'IProjectionDefinition&lt;ICellSpace, string?&gt;'</c>, which names both types and still leaves the
  /// reader to work out that the fix belongs two lines up, on the factory whose lambda this child
  /// sits in. A factory is where a space enters a declaration, and the vocabulary is generic in it,
  /// so this offers to build that one factory through
  /// <c>ProjectionBuilders&lt;TDemanded&gt;</c> — the same member, named over the space the child
  /// asks for.
  /// </para>
  /// <para>
  /// It fixes one factory, which is the honest scope of a mechanical edit: if the result then does
  /// not fit ITS parent, the compiler says so at the next site out, and the reader decides whether
  /// the file's own <c>using static</c> is what should have named the wider space all along.
  /// </para>
  /// </summary>
  [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DemandDoorCodeFixProvider))]
  [Shared]
  public sealed class DemandDoorCodeFixProvider : CodeFixProvider
  {
    /// <summary>The compiler's "cannot convert from X to Y" — an argument the receiver's type refuses.</summary>
    private const string ArgumentConversion = "CS1503";

    /// <summary>
    /// The factories that take a child declaration, and so the only ones a child of another space
    /// can be refused by. Everything else in the vocabulary builds a leaf out of nothing, where
    /// there is no child to disagree with the space and nothing for this fix to move.
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

      var vocabulary = symbols.Builders
        .Construct(demanded)
        .ToMinimalDisplayString(model, factory.SpanStart, SymbolDisplayFormat.MinimallyQualifiedFormat);

      var name = member.Identifier.ValueText;

      var title =
        $"Use '{vocabulary}.{name}' here — this child demands "
        + $"'{demanded.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}', "
        + $"which a '{name}' declared over "
        + $"'{Declared(factory, model, symbols, cancellationToken)}' cannot carry";

      var through = factory.WithExpression(SyntaxFactory.MemberAccessExpression(
        SyntaxKind.SimpleMemberAccessExpression,
        SyntaxFactory.ParseTypeName(vocabulary),
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

    private static SimpleNameSyntax? MemberName(InvocationExpressionSyntax invocation)
      => invocation.Expression switch
      {
        MemberAccessExpressionSyntax access => access.Name,
        SimpleNameSyntax name => name,
        _ => null,
      };
  }
}
