using Microsoft.CodeAnalysis;

namespace Lanz.MapWeaver.Generator.Services;

/// <summary>
/// Provides utility methods for analyzing and resolving type information.
/// </summary>
public interface ITypeResolver
{
    /// <summary>
    /// Determines if a type is a primitive type or string.
    /// </summary>
    bool IsPrimitiveOrString(ITypeSymbol typeSymbol);

    /// <summary>
    /// Determines if a type is a collection type.
    /// </summary>
    bool IsCollection(ITypeSymbol type);

    /// <summary>
    /// Attempts to extract the element type from a collection type.
    /// </summary>
    bool TryGetCollectionElementType(ITypeSymbol type, out ITypeSymbol? elementType);

    /// <summary>
    /// Checks if a symbol has a specific attribute.
    /// </summary>
    bool HasAttribute(ISymbol symbol, string attributeFullName);

    /// <summary>
    /// Gets the source property name from MapProperty attribute.
    /// </summary>
    string? GetMapPropertySource(ISymbol symbol, string attributeFullName);

    /// <summary>
    /// Resolves a property path (e.g., "Address.City") from a root type.
    /// </summary>
    IPropertySymbol? TryResolvePropertyPath(ITypeSymbol rootType, string path);

    /// <summary>
    /// Builds a safe null-conditional access expression for a property path.
    /// </summary>
    string BuildSafeAccessExpression(string rootParamName, string path);
}
