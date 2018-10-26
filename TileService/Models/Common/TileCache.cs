using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TileService.Models.Common
{
    public class TileCache<T> where T : GenericTile<T>
    {
        static readonly Dictionary<string, Task<T>> Tiles = new Dictionary<string, Task<T>>();
        static readonly List<string> Order = new List<string>();

        public int LoadZoom { get; }
        public int MinZoom { get; }
        public int MaxZoom { get; }
        public int CacheSize { get; }
        public Func<int, int, int, T> Loader { get; }
        public Func<int, int, int, T, T> Copier { get; }

        public TileCache(int loadZoom, int minZoom, int maxZoom, int cacheSize, Func<int, int, int, T> loader, Func<int, int, int, T, T> copier)
        {
            Debug.Assert(loadZoom <= minZoom, $"TileCache must have loadZoom {loadZoom} <= minZoom {minZoom}");
            Debug.Assert(minZoom <= maxZoom, $"TileCache must have minZoom {minZoom} <= maxZoom {maxZoom}");

            MinZoom = minZoom;
            MaxZoom = maxZoom;
            LoadZoom = loadZoom;
            CacheSize = cacheSize;
            Loader = loader;
            Copier = copier;
        }

        public async Task<T> Get(int zoom, int x, int y)
        {
            Debug.Assert(zoom >= MinZoom, $"TileCache.Get must have zoom {zoom} >= MinZoom {MinZoom}");
            Debug.Assert(zoom <= MaxZoom, $"TileCache.Get must have zoom {zoom} <= MaxZoom {MaxZoom}");

            var zoomDiff = zoom - LoadZoom;
            var cachedTile = await GetCached(LoadZoom, (int)(x / Math.Pow(2, zoomDiff)), (int)(y / Math.Pow(2, zoomDiff)));

            return Copier(zoom, x, y, cachedTile);
        }

        Task<T> GetCached(int zoom, int x, int y)
        {
            var key = $"{zoom}/{x}/{y}";
            lock (Tiles) {
                if (Tiles.TryGetValue(key, out var task)) {
                    Order.Remove(key);
                    Order.Add(key);
                    return task;
                }

                var tile = Loader(zoom, x, y);
                task = tile.Load();
                Tiles.Add(key, task);
                Order.Add(key);

                while (Order.Count > CacheSize) {
                    Tiles.Remove(Order[0]);
                    Order.RemoveAt(0);
                }

                Console.WriteLine($"Caching {tile} ({Order.Count} / {CacheSize})");

                return task;
            }
        }
    }
}
