; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
UNR001 | Unrect.Usage | Warning | Unnecessary scope. Nothing under this declaration demands the space it is closed over, so the plain spelling serves. See UnnecessaryScopeAnalyzer.
UNR003 | Unrect.Usage | Warning | Demands exceed offer. This projection demands a capability the space it is applied to does not offer. Reported beside the compiler's own inference failure, which names neither type. See DemandsExceedOfferAnalyzer.
