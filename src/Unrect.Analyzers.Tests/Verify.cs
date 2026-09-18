using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Unrect.Analyzers.Tests
{
  /// <summary>
  /// Runs a diagnostic, or a diagnostic and its fix, over a source file compiled against the REAL
  /// Unrect assemblies — the ones this solution just built, not a stub of them. A rule about what
  /// <c>IProjectionDefinition</c>'s demand means is only worth as much as the <c>IProjectionDefinition</c> it was tested
  /// against.
  /// </summary>
  internal static class Verify
  {
    /// <summary>
    /// What every test source may use, so that no test spends lines on imports — and, since phase 6,
    /// the file scope itself: there is no <c>Projection.Over&lt;T&gt;()</c> to open a scope with any
    /// more, so a source's space is named once here, in the <c>using static</c> every fixture shares.
    /// <c>ISheetCells</c> is the narrow one on purpose: it is what the streaming door vends and
    /// what a formula-reading child out-demands.
    /// </summary>
    public const string Usings = """
      using System.Collections.Generic;

      using Unrect.Core;
      using Unrect.Projections;
      using Unrect.Spreadsheets;

      using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
      using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;

      """;

    private static readonly MetadataReference[] Unrect =
    {
      MetadataReference.CreateFromFile(typeof(Core.ISpace).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Projections.ProjectionEngine).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Projections.ProjectionMapping).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Strategies.SizeStrategies).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Spreadsheets.SpreadsheetProjections).Assembly.Location),
    };

    /// <summary>Asserts that <paramref name="source"/> reports exactly <paramref name="expected"/>.</summary>
    public static Task Reports<TAnalyzer>(string source, params DiagnosticResult[] expected)
      where TAnalyzer : DiagnosticAnalyzer, new()
    {
      var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> { TestCode = Usings + source };

      test.ExpectedDiagnostics.AddRange(expected);
      Configure(test);

      return test.RunAsync();
    }

    /// <summary>Asserts that <paramref name="source"/> reports nothing at all.</summary>
    public static Task Silent<TAnalyzer>(string source)
      where TAnalyzer : DiagnosticAnalyzer, new()
      => Reports<TAnalyzer>(source);

    /// <summary>
    /// Asserts the diagnostics AND that fixing them yields <paramref name="fixedSource"/> — which,
    /// when it is the source unchanged, is the assertion that no fix was offered.
    /// </summary>
    public static Task Fixes<TAnalyzer, TCodeFix>(string source, string fixedSource, params DiagnosticResult[] expected)
      where TAnalyzer : DiagnosticAnalyzer, new()
      where TCodeFix : CodeFixProvider, new()
      => Fixes<TAnalyzer, TCodeFix>(source, fixedSource, titled: null, expected);

    /// <inheritdoc cref="Fixes{TAnalyzer, TCodeFix}(string, string, DiagnosticResult[])"/>
    /// <param name="source">The source, with the expectation written in it as markup.</param>
    /// <param name="fixedSource">What the source must read as once the fix is applied.</param>
    /// <param name="titled">
    /// The exact title the offer must carry, or null to leave it unpinned. Where a fix's whole value
    /// is the sentence it offers, the edit alone is not the assertion.
    /// </param>
    /// <param name="expected">The diagnostics of our own the source must report.</param>
    public static Task Fixes<TAnalyzer, TCodeFix>(
      string source,
      string fixedSource,
      string? titled,
      params DiagnosticResult[] expected)
      where TAnalyzer : DiagnosticAnalyzer, new()
      where TCodeFix : CodeFixProvider, new()
    {
      var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
      {
        TestCode = Usings + source,
        FixedCode = Usings + fixedSource,
      };

      if (titled is object)
        test.CodeActionVerifier = (action, verifier) => verifier.Equal(titled, action.Title);

      test.ExpectedDiagnostics.AddRange(expected);
      Configure(test);

      return test.RunAsync();
    }

    /// <summary>
    /// The same, for a fix on a diagnostic the COMPILER reports: there is no analyzer of ours in
    /// play, and the expectation is written in the source as markup naming the compiler's own error.
    /// </summary>
    public static Task FixesCompilerError<TCodeFix>(string source, string fixedSource, string? titled = null)
      where TCodeFix : CodeFixProvider, new()
      => Fixes<EmptyDiagnosticAnalyzer, TCodeFix>(source, fixedSource, titled);

    /// <summary>
    /// Asserts that the diagnostics stand and nothing is offered for them — the assertion behind
    /// every "not mechanical" in a fix provider's remarks.
    /// </summary>
    public static Task OffersNoFix<TAnalyzer, TCodeFix>(string source, params DiagnosticResult[] expected)
      where TAnalyzer : DiagnosticAnalyzer, new()
      where TCodeFix : CodeFixProvider, new()
    {
      var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
      {
        TestCode = Usings + source,
        FixedCode = Usings + source,
      };

      test.ExpectedDiagnostics.AddRange(expected);
      test.FixedState.ExpectedDiagnostics.AddRange(expected);
      Configure(test);

      return test.RunAsync();
    }

    /// <inheritdoc cref="OffersNoFix{TAnalyzer, TCodeFix}"/>
    public static Task OffersNoFixForCompilerError<TCodeFix>(string source)
      where TCodeFix : CodeFixProvider, new()
      => OffersNoFix<EmptyDiagnosticAnalyzer, TCodeFix>(source);

    private static void Configure(AnalyzerTest<DefaultVerifier> test)
    {
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalReferences.AddRange(Unrect);

      test.SolutionTransforms.Add((solution, projectId) =>
      {
        var project = solution.GetProject(projectId)!;
        var parse = (CSharpParseOptions)project.ParseOptions!;
        var compilation = (CSharpCompilationOptions)project.CompilationOptions!;

        return solution
          .WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.Latest))
          .WithProjectCompilationOptions(projectId, compilation.WithNullableContextOptions(NullableContextOptions.Enable));
      });
    }
  }
}
