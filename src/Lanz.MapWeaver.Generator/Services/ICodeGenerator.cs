using Lanz.MapWeaver.Generator.Models;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Lanz.MapWeaver.Generator.Services;

/// <summary>
/// Defines the contract for generating mapper source code.
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// Generates the mapper class implementation code.
    /// </summary>
    /// <param name="mapper">The mapper information containing types and methods.</param>
    /// <returns>The generated source code as a string.</returns>
    string GenerateMapperClass(MapperInfo mapper);

    /// <summary>
    /// Generates the service collection extension code for DI registration.
    /// </summary>
    /// <param name="mappers">Collection of all mappers to register.</param>
    /// <returns>The generated source code as a string.</returns>
    string GenerateServiceCollectionExtension(ImmutableArray<MapperInfo> mappers);
}
