using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Reflection;
using Lanz.MapWeaver.Generator;

namespace Lanz.MapWeaver.Tests;

public static class TestHelper
{
    public static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) GetGeneratedOutput(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>();
        
        // Add basic references
        var assemblies = new[]
        {
            typeof(object).Assembly,
            typeof(Console).Assembly,
            typeof(Lanz.MapWeaver.Abstraction.Attributes.GenerateMapperAttribute).Assembly,
            Assembly.Load("System.Runtime"),
            Assembly.Load("netstandard")
        };

        foreach (var assembly in assemblies.Distinct())
        {
            references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        var compilation = CSharpCompilation.Create(
            "Tests",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new MapperGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        
        var generatedSources = runResult.GeneratedTrees
            .Select(t => t.ToString())
            .ToArray();

        return (runResult.Diagnostics, generatedSources);
    }
}
