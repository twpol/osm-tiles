using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TileService.Models.Common;

namespace TileService.Models.Geometry
{
    public class RoadTile : GenericTile<RoadTile>
    {
        public static readonly TileCache<RoadTile> Cache = new TileCache<RoadTile>(14, 16, 22, 16, (zoom, x, y) => new RoadTile(zoom, x, y), (zoom, x, y, copy) => new RoadTile(zoom, x, y, copy));

        public ImmutableList<string> Layers { get; private set; }
        public ImmutableList<Way> Roads { get; private set; }
        public ImmutableList<Junction> RoadJunctions { get; private set; }

        RoadTile(int zoom, int x, int y)
            : base(zoom, x, y)
        {
        }

        RoadTile(int zoom, int x, int y, RoadTile copy)
            : base(zoom, x, y)
        {
            Debug.Assert(copy.Layers != null, "Cannot copy data from Tile without any data");

            Layers = copy.Layers;
            Roads = copy.Roads;
            RoadJunctions = copy.RoadJunctions;
        }

        async public override Task<RoadTile> Load()
        {
            Debug.Assert(Roads == null, "Cannot load data for Tile more than once");

            var overpass = await Overpass.Query.GetHighways(this);
            var overpassWays = overpass.elements.Where(element => element.type == "way").ToArray();
            var overpassNodes = overpass.elements.Where(element => element.type == "node").ToArray();
            var overpassNodesById = new Dictionary<long, Overpass.Element>(
                overpassNodes.Select(node => {
                    return new KeyValuePair<long, Overpass.Element>(
                        node.id,
                        node
                    );
                })
            );

            var overpassRoads = overpassWays.Where(way => {
                if (way.tags.GetValueOrDefault("area", "no") == "yes") {
                    return false;
                }
                switch (way.tags.GetValueOrDefault("highway", "no")) {
                    case "motorway":
                    case "trunk":
                    case "primary":
                    case "secondary":
                    case "tertiary":
                    case "unclassified":
                    case "residential":
                    case "service":
                    case "motorway_link":
                    case "trunk_link":
                    case "primary_link":
                    case "secondary_link":
                    case "tertiary_link":
                        return true;
                }
                return false;
            }).ToList();
            var overpassRoadJunctions = overpassNodes.Where(node => {
                return 1 < overpassRoads.Where(road => road.nodes.Contains(node.id)).Count();
            });

            Layers = ImmutableList.ToImmutableList(overpassWays.Select(way => {
                return way.tags.GetValueOrDefault("layer", "0");
            }).Distinct().Where(layer => int.TryParse(layer, out var result))).Sort((a, b) => {
                return int.Parse(a) - int.Parse(b);
            });

            Roads = ImmutableList.ToImmutableList(overpassRoads.Select(way => {
                return new Way(
                    this,
                    way.tags,
                    way.nodes.Select(nodeId => {
                        var node = overpassNodesById[nodeId];
                        return new Point(node.lat, node.lon);
                    })
                );
            }));
            RoadJunctions = ImmutableList.ToImmutableList(overpassRoadJunctions.Select(node => {
                return new Junction(
                    overpassRoads.Where(road => road.nodes.Contains(node.id)).Select(road => {
                        var way = Roads[overpassRoads.IndexOf(road)];
                        var point = way.Points[Array.IndexOf(road.nodes, node.id)];
                        return new WayPoint(way, point);
                    })
                );
            }));

            return this;
        }
    }
}
