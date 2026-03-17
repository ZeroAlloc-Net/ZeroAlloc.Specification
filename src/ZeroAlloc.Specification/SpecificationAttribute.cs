namespace ZeroAlloc.Specification;

/// <summary>
/// Marks a partial struct as a source-generated specification.
/// The generator emits And, Or, and Not composition methods.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class SpecificationAttribute : Attribute { }
