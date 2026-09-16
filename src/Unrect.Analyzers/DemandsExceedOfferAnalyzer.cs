using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Unrect.Analyzers
{
  /// <summary>
  /// <c>UNR003</c>: applying a declaration to a space that cannot answer what it demands.
  /// <para>
  /// This is a compile error already, and always was — the point of naming the space is that it is.
  /// What it is not is a legible one: the receiver and the argument each bind perfectly well on
  /// their own, so the compiler reports the call as a failure of type inference — <c>the type
  /// arguments for method 'ProjectionExtensions.Map&lt;TSpace, TResult&gt;' cannot be inferred from
  /// the usage</c> (CS0411), which names no capability and points at the method rather than at
  /// either type. This one is reported <em>alongside</em> the compiler's, and says which space was
  /// demanded and which was offered.
  /// </para>
  /// <para>
  /// <b>Where the same disagreement shows up earlier.</b> <c>IProjection&lt;TSpace, TResult&gt;</c>
  /// is invariant, so a declaration that out-demands the file it is composed into is refused at the
  /// composition site rather than surviving to <c>Map</c> — CS1503 or CS0311 at <c>v.Next(child)</c>,
  /// <c>Choice(…)</c> or <c>.Else(…)</c>. Those the compiler already names both types for, which is
  /// why nothing is added here for them; <c>UNR002</c>'s fix is what speaks there, and says which
  /// vocabulary to declare the factory through.
  /// </para>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public sealed class DemandsExceedOfferAnalyzer : DiagnosticAnalyzer
  {
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
      = ImmutableArray.Create(UnrectDiagnostics.DemandsExceedOffer);

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

      if (invocation.Expression is not MemberAccessExpressionSyntax application
        || !IsApplication(application.Name.Identifier.ValueText)
        || invocation.ArgumentList.Arguments.Count != 1)
      {
        return;
      }

      var model = context.SemanticModel;
      var cancellationToken = context.CancellationToken;

      // A call that binds needs no explaining: whatever it resolved to, the demand was met.
      if (model.GetSymbolInfo(invocation, cancellationToken).Symbol is object)
        return;

      if (symbols.DemandOf(model.GetTypeInfo(application.Expression, cancellationToken).Type) is not ITypeSymbol demanded)
        return;

      var offered = model.GetTypeInfo(invocation.ArgumentList.Arguments[0].Expression, cancellationToken).Type;

      // Something that is not a space at all is a different mistake, and the compiler's own message
      // for it names the types.
      if (offered is null || !UnrectSymbols.Satisfies(offered, symbols.Space))
        return;

      if (UnrectSymbols.Satisfies(offered, demanded))
        return;

      context.ReportDiagnostic(Diagnostic.Create(
        UnrectDiagnostics.DemandsExceedOffer,
        invocation.GetLocation(),
        demanded.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
        offered.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static bool IsApplication(string name)
      => name == "Map" || name == "Apply" || name == "MapWithDiagnostics";
  }
}
