using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TileService.Models.Common;

namespace TileService.Models.Geometry
{
    public class RailTile : GenericTile<RailTile>
    {
        public static readonly TileCache<RailTile> Cache = new TileCache<RailTile>(14, 16, 22, 16, (zoom, x, y) => new RailTile(zoom, x, y), (zoom, x, y, copy) => new RailTile(zoom, x, y, copy));

        public ImmutableList<string> Layers { get; private set; }
        public ImmutableList<Way> Rails { get; private set; }

        RailTile(int zoom, int x, int y)
            : base(zoom, x, y)
        {
        }

        RailTile(int zoom, int x, int y, RailTile copy)
            : base(zoom, x, y)
        {
            Debug.Assert(copy.Layers != null, "Cannot copy data from Tile without any data");

            Layers = copy.Layers;
            Rails = copy.Rails;
        }

        async public override Task<RailTile> Load()
        {
            Debug.Assert(Rails == null, "Cannot load data for Tile more than once");

            var overpass = await Overpass.Query.GetRailways(this);
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

            var overpassRails = overpassWays.Where(way => {
                if (way.tags.GetValueOrDefault("area", "no") == "yes") {
                    return false;
                }
                switch (way.tags.GetValueOrDefault("railway", "no")) {
                    case "abandoned":
                    case "construction":
                    case "disused":
                    case "funicular":
                    case "light_rail":
                    case "monorail":
                    case "narrow_gauge":
                    case "preserved":
                    case "rail":
                    case "subway":
                    case "tram":
                        return true;
                }
                return false;
            }).ToList();
            var overpassRailJunctions = overpassNodes.Where(node => {
                return 1 < overpassRails.Where(road => road.nodes.Contains(node.id)).Count();
            });

            Layers = ImmutableList.ToImmutableList(overpassWays.Select(way => {
                return way.tags.GetValueOrDefault("layer", "0");
            }).Distinct().Where(layer => int.TryParse(layer, out var result))).Sort((a, b) => {
                return int.Parse(a) - int.Parse(b);
            });

            Rails = ImmutableList.ToImmutableList(overpassRails.Select(way => {
                return new Way(
                    this,
                    way.tags,
                    way.nodes.Select(nodeId => {
                        var node = overpassNodesById[nodeId];
                        return new Point(node.lat, node.lon);
                    })
                );
            }));

            return this;
        }
    }
}
