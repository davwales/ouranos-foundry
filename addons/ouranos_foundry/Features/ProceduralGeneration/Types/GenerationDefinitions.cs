#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Types;

/// <summary>
/// A plain C# snapshot of a <c>ProceduralGenerationConfig</c>'s definition tables and
/// sub-configs. Build it on the main thread; after that it never touches a Godot
/// collection, so passes may read it from worker threads. Lookups are case-insensitive
/// by Id, keep the first definition of a duplicated Id and fall back to the type's
/// <c>Default</c>; <see cref="UnknownIds"/> lists the config's own references that miss.
/// </summary>
public sealed class GenerationDefinitions
{
    private readonly Dictionary<string, BiomeDefinition> _biomes;
    private readonly Dictionary<string, CitySizeDefinition> _citySizes;
    private readonly Dictionary<string, PoiTypeDefinition> _poiTypes;
    private readonly Dictionary<string, RoadTypeDefinition> _roadTypes;

    // Read the Defaults here so they are created on the main thread.
    private readonly BiomeDefinition _defaultBiome = BiomeDefinition.Default;
    private readonly CitySizeDefinition _defaultCitySize = CitySizeDefinition.Default;
    private readonly PoiTypeDefinition _defaultPoiType = PoiTypeDefinition.Default;
    private readonly RoadTypeDefinition _defaultRoadType = RoadTypeDefinition.Default;

    public GenerationDefinitions(ProceduralGenerationConfig config)
    {
        // Empty slots are skipped; Validate() reports them.
        Biomes = new([.. config.BiomeDefinitions.OfType<BiomeDefinition>()]);
        CitySizes = new([.. config.CitySizeDefinitions.OfType<CitySizeDefinition>()]);
        PoiTypes = new([.. config.PoiTypeDefinitions.OfType<PoiTypeDefinition>()]);
        RoadTypes = new([.. config.RoadTypeDefinitions.OfType<RoadTypeDefinition>()]);
        _biomes = ById(Biomes, d => d.Id);
        _citySizes = ById(CitySizes, d => d.Id);
        _poiTypes = ById(PoiTypes, d => d.Id);
        _roadTypes = ById(RoadTypes, d => d.Id);
        Terrain = config.TerrainConfig ?? new();
        Naming = config.NamingConfig ?? new();
        Generation = config.GenerationConfig ?? new();

        var sizeIds = Biomes
            .Select(b => b.CitySizeDistribution)
            .OfType<CitySizeDistribution>()
            .SelectMany(d => d.Entries.OfType<WeightedSizeEntry>().Select(e => e.SizeId));
        string[] roadTypeIds = [config.HubRoadTypeId, config.DefaultRoadTypeId];
        UnknownIds =
        [
            .. roadTypeIds
                .Where(id => !_roadTypes.ContainsKey(id))
                .Select(id => $"road type '{id}'")
                .Concat(
                    sizeIds
                        .Where(id => !_citySizes.ContainsKey(id))
                        .Select(id => $"city size '{id}'")
                )
                .Distinct(),
        ];
    }

    /// <summary>The definitions in config order, for seeded picks.</summary>
    public ReadOnlyCollection<BiomeDefinition> Biomes { get; }

    public ReadOnlyCollection<CitySizeDefinition> CitySizes { get; }

    public ReadOnlyCollection<PoiTypeDefinition> PoiTypes { get; }

    public ReadOnlyCollection<RoadTypeDefinition> RoadTypes { get; }

    /// <summary><c>config.TerrainConfig</c>, or a default one when it is not set.</summary>
    public TerrainConfig Terrain { get; }

    /// <summary><c>config.NamingConfig</c>, or a default one when it is not set.</summary>
    public NamingConfig Naming { get; }

    /// <summary><c>config.GenerationConfig</c>, or a default one when it is not set.</summary>
    public GenerationConfig Generation { get; }

    /// <summary>
    /// Road type and city size Ids the config refers to that no definition has. Lookups of
    /// them return the Default. <c>ProceduralGenerationConfig.GetWarnings</c> lists each one.
    /// </summary>
    public IReadOnlyList<string> UnknownIds { get; }

    public BiomeDefinition GetBiome(string id)
    {
        return _biomes.GetValueOrDefault(id, _defaultBiome);
    }

    public CitySizeDefinition GetCitySize(string id)
    {
        return _citySizes.GetValueOrDefault(id, _defaultCitySize);
    }

    public PoiTypeDefinition GetPoiType(string id)
    {
        return _poiTypes.GetValueOrDefault(id, _defaultPoiType);
    }

    public RoadTypeDefinition GetRoadType(string id)
    {
        return _roadTypes.GetValueOrDefault(id, _defaultRoadType);
    }

    private static Dictionary<string, T> ById<T>(IEnumerable<T> definitions, Func<T, string> id)
    {
        var byId = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in definitions)
        {
            byId.TryAdd(id(definition), definition);
        }

        return byId;
    }
}
