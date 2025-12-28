using Lanz.MapWeaver.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Lanz.MapWeaver.Generator;

[Generator]
public sealed class MapperGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var mapperCandidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => IsCandidate(node),
                static (ctx, _) => GetMapperInfo(ctx))
            .Where(static m => m is not null)!;

        var mappers = mapperCandidates.Collect();

        context.RegisterSourceOutput(mappers, static (spc, mapperList) =>
        {
            foreach (var mapper in mapperList!)
            {
                GenerateMapperCode(spc, mapper);
            }
        });

        var allMappers = mapperCandidates.Collect();

        context.RegisterSourceOutput(allMappers, static (spc, mappers) =>
        {
            if (mappers.Length > 0)
            {
                GenerateServiceCollectionExtension(spc, mappers!);
            }
        });
    }

    private static bool IsCandidate(SyntaxNode node) =>
        node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0;

    private static MapperInfo? GetMapperInfo(GeneratorSyntaxContext context)
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

        if (methods.Count == 0)
            return null;

        return new MapperInfo
        {
            Type = classSymbol,
            Methods = methods
        };
    }

    private static void GenerateMapperCode(SourceProductionContext context, MapperInfo mapper)
    {
        var className = mapper.Type.Name;
        var symbolDisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat;

        // Risoluzione namespace
        var ns = mapper.Type.ContainingNamespace.IsGlobalNamespace
            ? null
            : mapper.Type.ContainingNamespace.ToDisplayString();

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");

        if (ns is not null)
        {
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using Lanz.MapWeaver.Abstraction;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
        }

        // AGGIUNTA: Implementazione dell'interfaccia IMapper
        // Nota: Uso il Full Name dell'interfaccia per evitare conflitti o missing usings
        sb.AppendLine($"    public partial class {className} : global::Lanz.MapWeaver.Abstraction.IMapper");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly IServiceProvider _sp;");
        sb.AppendLine();
        sb.AppendLine($"        public {className}(IServiceProvider sp)"); // Constructor injection
        sb.AppendLine("        {");
        sb.AppendLine("            _sp = sp;");
        sb.AppendLine("        }");

        // 1. Generazione dei metodi specifici (Typed Methods)
        foreach (var method in mapper.Methods)
        {
            var srcName = method.SourceType.ToDisplayString(symbolDisplayFormat);
            var dstName = method.TargetType.ToDisplayString(symbolDisplayFormat);
            method.MethodName = method.IsReverse ? method.MethodName.Replace("Reverse", string.Empty) : method.MethodName;
            if (method.IsReverse)
            {
                sb.AppendLine($"        public {dstName} {method.MethodName}({srcName} source)");
            }
            else
            {
                sb.AppendLine($"        public partial {dstName} {method.MethodName}({srcName} source)");
            }
            sb.AppendLine("        {");
            sb.AppendLine("            if (source is null) return default!;");
            sb.AppendLine($"            var dest = new {dstName}();");

            var srcProps = method.SourceType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic);

            var dstProps = method.TargetType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic);

            foreach (var dp in dstProps)
            {
                if (HasAttribute(dp, "Lanz.MapWeaver.Abstraction.Attributes.MapIgnoreAttribute"))
                {
                    continue;
                }

                string srcPropName = dp.Name;

                var customSource = GetMapPropertySource(dp, "Lanz.MapWeaver.Abstraction.Attributes.MapPropertyAttribute");

                if (!string.IsNullOrEmpty(customSource))
                {
                    srcPropName = customSource!;
                }

                var sp = TryResolvePropertyPath(method.SourceType, srcPropName);
                if (sp is not null)
                {
                    string accessExpression = BuildSafeAccessExpression("source", srcPropName);
                    if (SymbolEqualityComparer.Default.Equals(sp.Type, dp.Type))
                    {
                        sb.AppendLine($"            dest.{dp.Name} = {accessExpression};");
                    }
                    else
                    {
                        if (TryGetCollectionElementType(sp.Type, out var srcItemType) &&
                            TryGetCollectionElementType(dp.Type, out var dstItemType))
                        {
                            var dstItemTypeName = dstItemType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            sb.AppendLine("var imapper = _sp.GetRequiredService<IMapper>();");
                            // Genera: source.Prop?.Select(item => this.Map<DstItem>(item))
                            sb.Append($"            dest.{dp.Name} = source.{sp.Name}?");
                            sb.Append($".Select(item => imapper.Map<{dstItemTypeName}>(item))");

                            if (dp.Type is IArrayTypeSymbol)
                            {
                                sb.AppendLine(".ToArray();");
                            }
                            else if (dp.Type.Name == "List" || dp.Type.Name == "IList" || dp.Type.Name == "ICollection")
                            {
                                sb.AppendLine(".ToList();");
                            }
                            else if (dp.Type.Name == "IEnumerable")
                            {
                                // IEnumerable basta il Select (che ritorna IEnumerable)
                                sb.AppendLine(";");
                            }
                            else
                            {
                                // Fallback (o errore se tipo collezione non supportato es. Queue)
                                sb.AppendLine(".ToList(); // Warning: Defaulting to List");
                            }
                        }
                        else if (!IsPrimitiveOrString(sp.Type))
                        {
                            var destType = dp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                            sb.AppendLine($"            if (source.{sp.Name} is not null)");
                            sb.AppendLine($"            {{");

                            sb.AppendLine($"                dest.{dp.Name} = _sp.GetRequiredService<IMapper>().Map<{destType}>(source.{sp.Name});");
                            sb.AppendLine($"            }}");
                        }
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("            return dest;");
            sb.AppendLine("        }");

        }

        // 2. Generazione dei metodi generici di IMapper (Dispatch logic)
        GenerateGenericMapMethods(sb, mapper);

        sb.AppendLine("    }"); // Chiudo classe

        if (ns is not null)
        {
            sb.AppendLine("}"); // Chiudo namespace
        }

        context.AddSource($"{className}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    // NUOVO METODO: Genera il dispatching per IMapper
    private static void GenerateGenericMapMethods(StringBuilder sb, MapperInfo mapper)
    {
        var format = SymbolDisplayFormat.FullyQualifiedFormat;

        sb.AppendLine();
        sb.AppendLine("        // --- IMapper Implementation ---");

        // Implementazione: TDestination Map<TDestination>(object source)
        sb.AppendLine("        public TDestination Map<TDestination>(object source)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (source is null) return default!;");
        sb.AppendLine("            var srcType = source.GetType();");
        sb.AppendLine("            var dstType = typeof(TDestination);");
        sb.AppendLine();

        // Genera un blocco IF per ogni coppia Source-Dest conosciuta
        foreach (var method in mapper.Methods)
        {
            var srcFull = method.SourceType.ToDisplayString(format);
            var dstFull = method.TargetType.ToDisplayString(format);

            // Check: if (srcType == typeof(User) && dstType == typeof(UserDto))
            sb.AppendLine($"            if (srcType == typeof({srcFull}) && dstType == typeof({dstFull}))");
            sb.AppendLine("            {");
            // Cast e chiamata al metodo specifico generato prima
            sb.AppendLine($"                var result = {method.MethodName}(({srcFull})source);");
            sb.AppendLine("                return (TDestination)(object)result;");
            sb.AppendLine("            }");
        }

        sb.AppendLine();
        sb.AppendLine("            throw new global::System.InvalidOperationException($\"Mapping not registered for {srcType.Name} to {dstType.Name}\");");
        sb.AppendLine("        }");

        // Implementazione: TDestination Map<TSource, TDestination>(TSource source)
        // Delega semplicemente al metodo precedente
        sb.AppendLine();
        sb.AppendLine("        public TDestination Map<TSource, TDestination>(TSource source)");
        sb.AppendLine("        {");
        sb.AppendLine("            return Map<TDestination>((object)source!);");
        sb.AppendLine("        }");

        // Implementazione: Map su istanza esistente
        // (Attualmente non supportato dalla logica base 'new()', quindi lanciamo eccezione o deleghiamo)
        sb.AppendLine();
        sb.AppendLine("        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Current generator implementation only supports creating new instances.");
        sb.AppendLine("            // Fallback to creating a new instance.");
        sb.AppendLine("            return Map<TSource, TDestination>(source);");
        sb.AppendLine("        }");
    }

    private static void GenerateServiceCollectionExtension(SourceProductionContext context, ImmutableArray<MapperInfo> mappers)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using Lanz.MapWeaver.Abstraction;");
        sb.AppendLine("using System;");
        sb.AppendLine();

        sb.AppendLine("namespace Lanz.MapWeaver.Extensions");
        sb.AppendLine("{");

        // --- 1. Generazione del Master Mapper (Orchestrator) ---
        // Questa classe implementa IMapper e conosce tutti i profili generati
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Orchestrates mapping calls across all generated profiles.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    internal sealed class MapWeaverOrchestrator : IMapper");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly IServiceProvider _serviceProvider;");
        sb.AppendLine();
        sb.AppendLine("        public MapWeaverOrchestrator(IServiceProvider serviceProvider)");
        sb.AppendLine("        {");
        sb.AppendLine("            _serviceProvider = serviceProvider;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Metodo Map<Dest>(object source)
        sb.AppendLine("        public TDestination Map<TDestination>(object source)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (source is null) return default!;");
        sb.AppendLine("            var srcType = source.GetType();");
        sb.AppendLine("            var dstType = typeof(TDestination);");
        sb.AppendLine();

        foreach (var mapper in mappers)
        {
            var mapperType = mapper.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            foreach (var method in mapper.Methods)
            {
                var srcFull = method.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var dstFull = method.TargetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                sb.AppendLine($"            if (srcType == typeof({srcFull}) && dstType == typeof({dstFull}))");
                sb.AppendLine("            {");
                sb.AppendLine($"                var profile = _serviceProvider.GetRequiredService<{mapperType}>();");
                sb.AppendLine($"                return ((IMapper)profile).Map<TDestination>(source);");
                sb.AppendLine("            }");
            }
        }

        sb.AppendLine();
        sb.AppendLine("            throw new InvalidOperationException($\"No mapping configuration found for {srcType.Name} to {dstType.Name} in any profile.\");");
        sb.AppendLine("        }");

        sb.AppendLine();
        sb.AppendLine("        public TDestination Map<TSource, TDestination>(TSource source)");
        sb.AppendLine("            => Map<TDestination>((object)source!);");
        sb.AppendLine();
        sb.AppendLine("        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)");
        sb.AppendLine("            => Map<TDestination>((object)source!); // Fallback to new instance creation");

        sb.AppendLine("    }"); // Fine classe Orchestrator
        sb.AppendLine();

        // --- 2. Generazione dell'Extension Method DI ---
        sb.AppendLine("    public static class MapWeaverServiceCollectionExtensions");
        sb.AppendLine("    {");
        sb.AppendLine("        public static IServiceCollection AddMapWeaver(this IServiceCollection services)");
        sb.AppendLine("        {");

        // Registra ogni singolo profilo concreto
        foreach (var mapper in mappers)
        {
            var fullClassName = mapper.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            sb.AppendLine($"            services.AddSingleton<{fullClassName}>();");
        }

        // Registra l'Orchestrator come implementazione unica di IMapper
        sb.AppendLine("            services.AddSingleton<IMapper, MapWeaverOrchestrator>();");

        sb.AppendLine("            return services;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}"); // Fine Namespace

        context.AddSource("MapWeaverOrchestrator.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static bool IsPrimitiveOrString(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.SpecialType == SpecialType.System_String) return true;

        // Controlla se è un tipo primitivo (int, bool, double, etc.) o enum
        return typeSymbol.IsValueType && (typeSymbol.SpecialType != SpecialType.None || typeSymbol.TypeKind == TypeKind.Enum);
    }

    private static bool IsCollection(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String) return false;

        return type.AllInterfaces.Any(i => i.Name == "IEnumerable") || type is IArrayTypeSymbol;
    }

    private static bool TryGetCollectionElementType(ITypeSymbol type, out ITypeSymbol? elementType)
    {
        elementType = null;

        if (type.SpecialType == SpecialType.System_String) return false;

        if (type is IArrayTypeSymbol arraySymbol)
        {
            elementType = arraySymbol.ElementType;
            return true;
        }

        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            // Controlla se implementa IEnumerable (o è IEnumerable stesso)
            // Nota: Questo è un check semplificato. Per robustezza dovresti scorrere le interfacce.
            if (namedType.Name == "IEnumerable" || namedType.AllInterfaces.Any(i => i.Name == "IEnumerable"))
            {
                // Prendiamo il primo argomento generico come tipo elemento
                if (namedType.TypeArguments.Length > 0)
                {
                    elementType = namedType.TypeArguments[0];
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasAttribute(ISymbol symbol, string attributeFullName)
    {
        return symbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString() == attributeFullName);
    }

    private static string? GetMapPropertySource(ISymbol symbol, string attributeFullName)
    {
        var attr = symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeFullName);

        if (attr is null) return null;

        if (attr.ConstructorArguments.Length > 0)
        {
            return attr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
        }
        return null;
    }

    private static IPropertySymbol? TryResolvePropertyPath(ITypeSymbol rootType, string path)
    {
        var parts = path.Split('.');
        ITypeSymbol currentType = rootType;
        IPropertySymbol? currentProp = null;

        foreach (var part in parts)
        {
            // Cerca la proprietà nel tipo corrente
            currentProp = currentType.GetMembers()
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.Name == part && p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic);

            if (currentProp == null) return null; // Path interrotto

            currentType = currentProp.Type;
        }

        return currentProp; // Ritorna l'ultima proprietà della catena (es. City)
    }

    private static string BuildSafeAccessExpression(string rootParamName, string path)
    {
        var parts = path.Split('.');
        if (parts.Length == 1) return $"{rootParamName}.{parts[0]}";

        var sb = new StringBuilder(rootParamName);
        for (int i = 0; i < parts.Length; i++)
        {
            sb.Append($".{parts[i]}");
            // Aggiungi null conditional operator '?' per tutti tranne l'ultimo
            // (o anche per l'ultimo se la proprietà finale è nullable e serve)
            // Per sicurezza, lo mettiamo su tutti i nodi intermedi.
            if (i < parts.Length - 1)
            {
                sb.Append("?");
            }
        }
        return sb.ToString();
    }


}
