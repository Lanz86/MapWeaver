using Microsoft.CodeAnalysis;

namespace Lanz.MapWeaver.Generator.Models;

/// <summary>
/// Represents metadata about a mapping method, including its source and target types and method name.
/// </summary>
/// <remarks>Use this class to describe or analyze mapping methods between types, such as when generating or
/// inspecting code that performs object-to-object mapping. Instances of this class are immutable.</remarks>
public sealed class MapMethodInfo
{
    public INamedTypeSymbol SourceType { get; set; }
    public INamedTypeSymbol TargetType { get; set; }
    public string MethodName { get; set; }
}

/// <summary>
/// Represents metadata about a mapping type, including its symbol and associated mapping methods.
/// </summary>
/// <remarks>This class is typically used in code generation or analysis scenarios to describe a type that
/// provides mapping functionality, along with the methods that perform the mappings. Instances of this class are
/// immutable once constructed.</remarks>
public sealed class MapperInfo
{
    /// <summary>
    /// Gets or sets the symbol representing the type associated with this member.
    /// </summary>
    public INamedTypeSymbol Type { get; set; }
    /// <summary>
    /// Gets or sets the collection of method metadata associated with the map.
    /// </summary>
    public List<MapMethodInfo> Methods { get; set; }
}
