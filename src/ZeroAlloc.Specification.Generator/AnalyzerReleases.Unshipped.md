; Unshipped analyzer release.
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### Changed Rules

Rule ID | New Category            | New Severity | Old Category            | Old Severity | Notes
--------|-------------------------|--------------|-------------------------|--------------|------------------------------------------------------------------
ZA005   | ZeroAlloc.Specification | Error        | ZeroAlloc.Specification | Warning      | Nested specification inside a containing type that is not partial
