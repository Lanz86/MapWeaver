using Microsoft.CodeAnalysis;
using System.Text;

namespace Lanz.MapWeaver.Generator.Services;

/// <summary>
/// Provides type analysis and resolution utilities.
/// </summary>
public sealed class TypeResolver : ITypeResolver
{
    /// <inheritdoc/>
    public bool IsPrimitiveOrString(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.SpecialType == SpecialType.System_String) 
            return true;

        return typeSymbol.IsValueType && 
               (typeSymbol.SpecialType != SpecialType.None || typeSymbol.TypeKind == TypeKind.Enum);
    }

    /// <inheritdoc/>
    public bool IsCollection(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String) 
            return false;

        return type.AllInterfaces.Any(i => i.Name == "IEnumerable") || type is IArrayTypeSymbol;
    }

    /// <inheritdoc/>
    public bool TryGetCollectionElementType(ITypeSymbol type, out ITypeSymbol? elementType)
    {
        elementType = null;

        if (type.SpecialType == SpecialType.System_String) 
            return false;

        if (type is IArrayTypeSymbol arraySymbol)
        {
            elementType = arraySymbol.ElementType;
            return true;
        }

        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            if (namedType.Name == "IEnumerable" || 
                namedType.AllInterfaces.Any(i => i.Name == "IEnumerable"))
            {
                if (namedType.TypeArguments.Length > 0)
                {
                    elementType = namedType.TypeArguments[0];
                    return true;
                }
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public bool HasAttribute(ISymbol symbol, string attributeFullName)
    {
        return symbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString() == attributeFullName);
    }

    /// <inheritdoc/>
    public string? GetMapPropertySource(ISymbol symbol, string attributeFullName)
    {
        var attr = symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeFullName);
        
        if (attr is null) 
            return null;

        if (attr.ConstructorArguments.Length > 0)
        {
            return attr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
        }
        
        return null;
    }

    /// <inheritdoc/>
    public IPropertySymbol? TryResolvePropertyPath(ITypeSymbol rootType, string path)
    {
        var parts = path.Split('.');
        ITypeSymbol currentType = rootType;
        IPropertySymbol? currentProp = null;

        foreach (var part in parts)
        {
            currentProp = currentType.GetMembers()
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.Name == part && 
                                   p.DeclaredAccessibility == Accessibility.Public && 
                                   !p.IsStatic);

            if (currentProp == null) 
                return null;

            currentType = currentProp.Type;
        }

        return currentProp;
    }

    /// <inheritdoc/>
    public string BuildSafeAccessExpression(string rootParamName, string path)
    {
        var parts = path.Split('.');
        
        if (parts.Length == 1) 
            return $"{rootParamName}.{parts[0]}";

        var sb = new StringBuilder(rootParamName);
        
        for (int i = 0; i < parts.Length; i++)
        {
            sb.Append($".{parts[i]}");
            
            if (i < parts.Length - 1)
            {
                sb.Append("?");
            }
        }
        
        return sb.ToString();
    }
}
