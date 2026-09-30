using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ZeroAlloc.Specification.Generator;

namespace ZeroAlloc.Specification.Tests.Generator;

/// <summary>
/// Runs the generator on an in-memory source and compiles the result, so a test can check both
/// what was generated and that the code using it builds.
/// </summary>
internal static class GeneratorRunner
{
    private const string Usings = """
        using System;
        using System.Linq.Expressions;
        using ZeroAlloc.Specification;

        """;

    public sealed record Result(string[] HintNames, string[] Sources, Diagnostic[] Diagnostics);

    /// <summary>
    /// A <c>[Specification]</c> struct over <c>int</c> with the given name, which may carry type
    /// parameters, as in <c>IsActive&lt;T&gt;</c>.
    /// </summary>
    public static string Spec(string name) => $$"""
        [Specification]
        public readonly partial struct {{name}} : ISpecification<int>
        {
            public bool IsSatisfiedBy(int x) => x > 0;
            public Expression<Func<int, bool>> ToExpression() => x => x > 0;
        }
        """;

    /// <summary>
    /// Generated hint names and sources in hint-name order, and every warning or error from the
    /// generator and from compiling its output with the source.
    /// </summary>
    public static Result Run(string source)
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ISpecification<>).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
        };
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(Usings + source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CSharpGeneratorDriver.Create(new SpecificationGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);

        var generated = output.SyntaxTrees
            .Skip(1)
            .OrderBy(t => Path.GetFileName(t.FilePath), StringComparer.Ordinal)
            .ToArray();
        var diagnostics = generatorDiagnostics
            .Concat(output.GetDiagnostics())
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .ToArray();
        return new Result(
            generated.Select(t => Path.GetFileName(t.FilePath)).ToArray(),
            generated.Select(t => t.GetText().ToString()).ToArray(),
            diagnostics);
    }
}
