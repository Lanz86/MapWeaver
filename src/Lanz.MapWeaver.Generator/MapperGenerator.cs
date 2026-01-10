using Lanz.MapWeaver.Generator.Models;
using Lanz.MapWeaver.Generator.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Lanz.MapWeaver.Generator;

/// <summary>
/// Source generator for creating mapper implementations.
/// </summary>
[Generator]
public sealed class MapperGenerator : IIncrementalGenerator
{
    private readonly IMapperAnalyzer _analyzer;
    private readonly ICodeGenerator _codeGenerator;

    /// <summary>
    /// Initializes a new instance of the MapperGenerator class with default dependencies.
    /// </summary>
    public MapperGenerator() : this(new MapperAnalyzer(), new CodeGenerator(new TypeResolver()))
    {
    }

    /// <summary>
    /// Initializes a new instance of the MapperGenerator class with specified dependencies.
    /// </summary>
    /// <param name="analyzer">The mapper analyzer instance.</param>
    /// <param name="codeGenerator">The code generator instance.</param>
    internal MapperGenerator(IMapperAnalyzer analyzer, ICodeGenerator codeGenerator)
    {
        _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
    }

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var mapperCandidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                (node, _) => _analyzer.IsMapperCandidate(node),
                (ctx, _) => _analyzer.ExtractMapperInfo(ctx))
            .Where(m => m is not null)!;

        var mappers = mapperCandidates.Collect();

        context.RegisterSourceOutput(mappers, (spc, mapperList) =>
        {
            foreach (var mapper in mapperList!)
            {
                GenerateMapperCode(spc, mapper);
            }
        });

        var allMappers = mapperCandidates.Collect();

        context.RegisterSourceOutput(allMappers, (spc, mappers) =>
        {
            if (mappers.Length > 0)
            {
                GenerateServiceCollectionExtension(spc, mappers!);
            }
        });
    }

    /// <summary>
    /// Generates the mapper class source code and adds it to the compilation.
    /// </summary>
    private void GenerateMapperCode(SourceProductionContext context, MapperInfo mapper)
    {
        var className = mapper.Type.Name;
        var sourceCode = _codeGenerator.GenerateMapperClass(mapper);
        context.AddSource($"{className}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    }

    /// <summary>
    /// Generates the service collection extension source code and adds it to the compilation.
    /// </summary>
    private void GenerateServiceCollectionExtension(SourceProductionContext context, System.Collections.Immutable.ImmutableArray<MapperInfo> mappers)
    {
        var sourceCode = _codeGenerator.GenerateServiceCollectionExtension(mappers);
        context.AddSource("MapWeaverOrchestrator.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    }
}
