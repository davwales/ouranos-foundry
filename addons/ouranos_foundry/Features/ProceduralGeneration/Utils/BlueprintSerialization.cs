#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Utils;

/// <summary>
/// Converts blueprint records to and from save data: dictionaries of Variant-compatible
/// values, with IDs as strings and road paths as flat <c>[x0, y0, x1, y1, …]</c> arrays.
/// Readers throw when a key is missing or a value is malformed.
/// </summary>
public static class BlueprintSerialization
{
    public static Godot.Collections.Dictionary<string, Variant> ToData(Region region)
    {
        return new()
        {
            ["id"] = region.Id.ToString(),
            ["name"] = region.Name,
            ["center"] = region.Center,
            ["biome_id"] = region.BiomeId,
            ["city_ids"] = ToData(region.CityIds),
            ["road_ids"] = ToData(region.RoadIds),
        };
    }

    public static Region ToRegion(Variant data)
    {
        var d = data.AsGodotDictionary();
        return new Region(
            ToGuid(d["id"]),
            d["name"].AsString(),
            d["center"].AsVector2I(),
            d["biome_id"].AsString(),
            ToGuids(d["city_ids"]),
            ToGuids(d["road_ids"])
        );
    }

    /// <summary>A city with its points of interest, which take the city's ID on load.</summary>
    public static Godot.Collections.Dictionary<string, Variant> ToData(City city)
    {
        var pois = new Godot.Collections.Array();
        foreach (var poi in city.PointsOfInterest)
        {
            pois.Add(
                new Godot.Collections.Dictionary<string, Variant>
                {
                    ["id"] = poi.Id.ToString(),
                    ["name"] = poi.Name,
                    ["poi_type_id"] = poi.PoiTypeId,
                    ["position"] = poi.Position,
                }
            );
        }

        return new()
        {
            ["id"] = city.Id.ToString(),
            ["name"] = city.Name,
            ["position"] = city.Position,
            ["size_id"] = city.SizeId,
            ["region_id"] = city.RegionId.ToString(),
            ["points_of_interest"] = pois,
            ["road_ids"] = ToData(city.RoadIds),
            ["radius"] = city.Radius,
        };
    }

    public static City ToCity(Variant data)
    {
        var d = data.AsGodotDictionary();
        var id = ToGuid(d["id"]);
        var pois = d["points_of_interest"]
            .AsGodotArray()
            .Select(poi => poi.AsGodotDictionary())
            .Select(poi => new PointOfInterest(
                ToGuid(poi["id"]),
                poi["name"].AsString(),
                poi["poi_type_id"].AsString(),
                poi["position"].AsVector2I(),
                id
            ));

        return new City(
            id,
            d["name"].AsString(),
            d["position"].AsVector2I(),
            d["size_id"].AsString(),
            ToGuid(d["region_id"]),
            [.. pois],
            ToGuids(d["road_ids"]),
            d["radius"].AsInt32()
        );
    }

    public static Godot.Collections.Dictionary<string, Variant> ToData(Road road)
    {
        return new()
        {
            ["id"] = road.Id.ToString(),
            ["source_city_id"] = road.SourceCityId.ToString(),
            ["target_city_id"] = road.TargetCityId.ToString(),
            ["road_type_id"] = road.RoadTypeId,
            ["path"] = road.PathPoints.SelectMany(p => new[] { p.X, p.Y }).ToArray(),
        };
    }

    public static Road ToRoad(Variant data)
    {
        var d = data.AsGodotDictionary();
        var path = d["path"].AsInt32Array();
        if (path.Length % 2 != 0)
        {
            throw new FormatException("A road path has an odd number of coordinates.");
        }

        return new Road(
            ToGuid(d["id"]),
            ToGuid(d["source_city_id"]),
            ToGuid(d["target_city_id"]),
            d["road_type_id"].AsString(),
            [.. path.Chunk(2).Select(xy => new Vector2I(xy[0], xy[1]))]
        );
    }

    private static string[] ToData(IEnumerable<Guid> ids)
    {
        return [.. ids.Select(id => id.ToString())];
    }

    private static Guid ToGuid(Variant id)
    {
        return Guid.Parse(id.AsString());
    }

    private static List<Guid> ToGuids(Variant ids)
    {
        return [.. ids.AsStringArray().Select(Guid.Parse)];
    }
}
