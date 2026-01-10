using Lanz.MapWeaver.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lanz.MapWeaver.Generator.Services;

/// <summary>
/// Analyzes syntax nodes to identify and extract mapper information.
/// </summary>
public sealed class MapperAnalyzer : IMapperAnalyzer
{
    /// <inheritdoc/>
    public bool IsMapperCandidate(SyntaxNode node) =>
        node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0;

    /// <inheritdoc/>
    public MapperInfo? ExtractMapperInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        
        if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol)
            return null;

        var generateMapperAttr = semanticModel.Compilation
            .GetTypeByMetadataName("Lanz.MapWeaver.Abstraction.Attributes.GenerateMapperAttribute");
        
        if (generateMapperAttr is null)
            return null;

        var hasAttr = classSymbol.GetAttributes()
            .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, generateMapperAttr));

        if (!hasAttr)
            return null;

        var mapTypesAttrSymbol = semanticModel.Compilation
            .GetTypeByMetadataName("Lanz.MapWeaver.Abstraction.Attributes.MapTypesAttribute");
        
        if (mapTypesAttrSymbol is null)
            return null;

        var methods = ExtractMapMethods(classSymbol, mapTypesAttrSymbol);

        if (methods.Count == 0)
            return null;

        return new MapperInfo
        {
            Type = classSymbol,
            Methods = methods
        };
    }

    private static List<MapMethodInfo> ExtractMapMethods(INamedTypeSymbol classSymbol, INamedTypeSymbol mapTypesAttrSymbol)
    {
        var methods = new List<MapMethodInfo>();

        foreach (var member in classSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (!member.IsPartialDefinition)
                continue;

            foreach (var attr in member.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, mapTypesAttrSymbol))
                    continue;

                if (attr.ConstructorArguments.Length < 2)
                    continue;

                if (attr.ConstructorArguments[0].Value is not INamedTypeSymbol src)
                    continue;
                if (attr.ConstructorArguments[1].Value is not INamedTypeSymbol dst)
                    continue;

                bool reverse = ExtractReverseFlag(attr);

                methods.Add(new MapMethodInfo
                {
                    SourceType = src,
                    TargetType = dst,
                    MethodName = member.Name,
                });

                if (reverse)
                {
                    methods.Add(new MapMethodInfo
                    {
                        SourceType = dst,
                        TargetType = src,
                        MethodName = member.Name + "Reverse",
                        IsReverse = reverse
                    });
                }
            }
        }

        return methods;
    }

    private static bool ExtractReverseFlag(AttributeData attr)
    {
        bool reverse = false;
        
        if (attr.ConstructorArguments.Length >= 3 &&
            attr.ConstructorArguments[2].Value is bool reverseCtorValue)
        {
            reverse = reverseCtorValue;
        }

        if (!reverse && attr.NamedArguments.Length > 0)
        {
            foreach (var namedArg in attr.NamedArguments)
            {
                if (namedArg.Key == "Reverse" && namedArg.Value.Value is bool reverseNamedValue)
                {
                    reverse = reverseNamedValue;
                    break;
                }
            }
        }

        return reverse;
    }
}
