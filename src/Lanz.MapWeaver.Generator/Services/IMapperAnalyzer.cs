using Lanz.MapWeaver.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lanz.MapWeaver.Generator.Services;

/// <summary>
/// Defines the contract for analyzing mapper candidates and extracting mapper metadata.
/// </summary>
public interface IMapperAnalyzer
{
    /// <summary>
    /// Determines whether a syntax node is a potential mapper candidate.
    /// </summary>
    /// <param name="node">The syntax node to analyze.</param>
    /// <returns>True if the node is a mapper candidate; otherwise, false.</returns>
    bool IsMapperCandidate(SyntaxNode node);

    /// <summary>
    /// Extracts mapper information from a syntax context.
    /// </summary>
    /// <param name="context">The generator syntax context containing the node and semantic model.</param>
    /// <returns>MapperInfo if valid mapper is found; otherwise, null.</returns>
    MapperInfo? ExtractMapperInfo(GeneratorSyntaxContext context);
}
