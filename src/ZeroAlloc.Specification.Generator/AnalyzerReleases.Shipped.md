; Shipped analyzer releases.
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.1.0

### New Rules

Rule ID | Category                | Severity | Notes
--------|-------------------------|----------|-------------------------------------------------------
ZA001   | ZeroAlloc.Specification | Error    | [Specification] must be applied to a struct
ZA002   | ZeroAlloc.Specification | Error    | Specification struct must implement ISpecification<T>
ZA003   | ZeroAlloc.Specification | Error    | Specification struct must be partial
ZA004   | ZeroAlloc.Specification | Warning  | Specification struct should be readonly

## Release 1.2.5

### New Rules

Rule ID | Category                | Severity | Notes
--------|-------------------------|----------|------------------------------------------------------------------
ZA005   | ZeroAlloc.Specification | Warning  | Nested specification inside a containing type that is not partial
