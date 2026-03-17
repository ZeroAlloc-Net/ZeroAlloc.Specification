using Microsoft.CodeAnalysis;

namespace ZeroAlloc.Specification.Generator;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor NotAStruct = new(
        id: "ZA001",
        title: "[Specification] must be applied to a struct",
        messageFormat: "'{0}' must be a struct to use [Specification]",
        category: "ZeroAlloc.Specification",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingInterface = new(
        id: "ZA002",
        title: "Specification struct must implement ISpecification<T>",
        messageFormat: "'{0}' must implement ISpecification<T>",
        category: "ZeroAlloc.Specification",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotPartial = new(
        id: "ZA003",
        title: "Specification struct must be partial",
        messageFormat: "'{0}' must be declared partial",
        category: "ZeroAlloc.Specification",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotReadonly = new(
        id: "ZA004",
        title: "Specification struct should be readonly",
        messageFormat: "'{0}' should be declared readonly for correctness",
        category: "ZeroAlloc.Specification",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
