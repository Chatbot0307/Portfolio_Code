
using GameSync;
using Island;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public static class SaveManager
{
    private static readonly string SaveDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves");
    private static readonly string StateFile = Path.Combine(SaveDirectory, "server_state.sur");
    private static readonly string LegacyMapFile = Path.Combine(SaveDirectory, "world_map.sur");
    private static readonly string RegionsDirectory = Path.Combine(SaveDirectory, "regions");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    static SaveManager()
    {
        Directory.CreateDirectory(SaveDirectory);
        Directory.CreateDirectory(RegionsDirectory);
    }

    public class WorldSaveData
    {
        public List<NetworkInstantiate> NetworkObjects { get; set; } = new();
        public float ElapsedTicks { get; set; }
    }

    public class WorldMapData
    {
        public Dictionary<string, List<string>> ChunkData { get; set; } = new();
    }

    public class RegionSaveData
    {
        public int RegionX { get; set; }
        public int RegionZ { get; set; }
        public Dictionary<string, ChunkSaveData> Chunks { get; set; } = new();
    }

    public class ChunkSaveData
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
        public List<string> Blocks { get; set; } = new();
    }

    public static void SaveAll(ServerContext ctx)
    {
        SaveAllRegions();
        SaveState(ctx);
    }

    public static void SaveState(ServerContext ctx)
    {
        try
        {
            var stateData = new WorldSaveData
            {
                NetworkObjects = ctx.NetworkObjectDetails.Values.ToList(),
                ElapsedTicks = ctx.ElapsedTicks
            };

            File.WriteAllText(StateFile, JsonSerializer.Serialize(stateData, JsonOptions));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Save] State save failed: {ex.Message}");
        }
    }

    public static void SaveAllRegions()
    {
        var grouped = ServerMapManager.Instance.Chunks.Values
            .GroupBy(chunk =>
            {
                ServerMapManager.GetRegionCoordsByChunk(chunk.X, chunk.Z, out int rx, out int rz);
                return (rx, rz);
            });

        foreach (var group in grouped)
            SaveRegion(group.Key.rx, group.Key.rz, group);
    }

    public static void SaveRegion(int rx, int rz)
    {
        SaveRegion(rx, rz, ServerMapManager.Instance.GetChunksInRegion(rx, rz));
    }

    public static void SaveRegion(int rx, int rz, IEnumerable<ServerChunk> chunks)
    {
        try
        {
            var regionData = new RegionSaveData
            {
                RegionX = rx,
                RegionZ = rz
            };

            foreach (var chunk in chunks)
            {
                string key = ServerMapManager.GetChunkKey(chunk.X, chunk.Z);
                regionData.Chunks[key] = new ChunkSaveData
                {
                    ChunkX = chunk.X,
                    ChunkZ = chunk.Z,
                    Blocks = VoxelData.Flatten(chunk.Blocks)
                };
            }

            string path = GetRegionFilePath(rx, rz);
            File.WriteAllText(path, JsonSerializer.Serialize(regionData, JsonOptions));

            foreach (var chunk in ServerMapManager.Instance.GetChunksInRegion(rx, rz))
                chunk.IsDirty = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Save] Region ({rx},{rz}) save failed: {ex.Message}");
        }
    }

    public static void LoadAll(ServerContext ctx)
    {
        LoadState(ctx);
        LoadAllRegionsOrLegacy();
    }

    public static void LoadState(ServerContext ctx)
    {
        if (!File.Exists(StateFile))
            return;

        try
        {
            string json = File.ReadAllText(StateFile);
            var stateData = JsonSerializer.Deserialize<WorldSaveData>(json, JsonOptions);

            if (stateData == null)
                return;

            ctx.ElapsedTicks = stateData.ElapsedTicks;

            int maxId = 0;
            foreach (var obj in stateData.NetworkObjects)
            {
                ctx.NetworkObjectDetails[obj.NetworkId] = obj;
                ctx.NetworkObjects[obj.NetworkId] = obj.OwnerPlayerId;
                maxId = Math.Max(maxId, obj.NetworkId);
            }

            ctx.SetNextNetworkId(maxId);
            Console.WriteLine($"[Load] 서버 상태 로드 완료. (객체: {stateData.NetworkObjects.Count}개)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Load] State load failed: {ex.Message}");
        }
    }

    public static void LoadAllRegionsOrLegacy()
    {
        var regionFiles = Directory.GetFiles(RegionsDirectory, "r.*.*.json", SearchOption.TopDirectoryOnly);
        if (regionFiles.Length > 0)
        {
            LoadAllRegions();
            return;
        }

        if (File.Exists(LegacyMapFile))
        {
            Console.WriteLine("[Load] legacy world_map.sur detected. Starting migration to region files...");
            LoadLegacyMap();
            SaveAllRegions();
            Console.WriteLine("[Load] legacy map migration completed.");
        }
    }

    public static void LoadAllRegions()
    {
        ServerMapManager.Instance.ClearAll();

        var regionFiles = Directory.GetFiles(RegionsDirectory, "r.*.*.json", SearchOption.TopDirectoryOnly);
        foreach (var path in regionFiles)
            LoadRegionFromPath(path);

        Console.WriteLine($"[Load] region files loaded. chunk count: {ServerMapManager.Instance.Chunks.Count}");
    }

    public static ServerChunk LoadChunk(int cx, int cz)
    {
        ServerMapManager.GetRegionCoordsByChunk(cx, cz, out int rx, out int rz);
        string path = GetRegionFilePath(rx, rz);

        if (!File.Exists(path))
            return null;

        try
        {
            var region = JsonSerializer.Deserialize<RegionSaveData>(File.ReadAllText(path), JsonOptions);
            if (region == null)
                return null;

            string key = ServerMapManager.GetChunkKey(cx, cz);
            if (!region.Chunks.TryGetValue(key, out var chunkData))
                return null;

            var chunk = new ServerChunk(chunkData.ChunkX, chunkData.ChunkZ);
            VoxelData.Unflatten(chunkData.Blocks, chunk.Blocks);
            chunk.IsDirty = false;
            return chunk;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Load] Chunk ({cx},{cz}) load failed: {ex.Message}");
            return null;
        }
    }

    private static void LoadRegionFromPath(string path)
    {
        try
        {
            var regionData = JsonSerializer.Deserialize<RegionSaveData>(File.ReadAllText(path), JsonOptions);
            if (regionData == null)
                return;

            foreach (var chunkData in regionData.Chunks.Values)
            {
                var chunk = new ServerChunk(chunkData.ChunkX, chunkData.ChunkZ);
                VoxelData.Unflatten(chunkData.Blocks, chunk.Blocks);
                chunk.IsDirty = false;
                ServerMapManager.Instance.AddOrReplaceChunk(chunk, markDirty: false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Load] Region file load failed ({Path.GetFileName(path)}): {ex.Message}");
        }
    }

    private static void LoadLegacyMap()
    {
        try
        {
            var mapData = JsonSerializer.Deserialize<WorldMapData>(File.ReadAllText(LegacyMapFile), JsonOptions);
            if (mapData == null)
                return;

            ServerMapManager.Instance.ClearAll();

            foreach (var kvp in mapData.ChunkData)
            {
                string[] c = kvp.Key.Split('_');
                var chunk = new ServerChunk(int.Parse(c[0]), int.Parse(c[1]));
                VoxelData.Unflatten(kvp.Value, chunk.Blocks);
                ServerMapManager.Instance.AddOrReplaceChunk(chunk, markDirty: false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Load] Legacy map load failed: {ex.Message}");
        }
    }

    private static string GetRegionFilePath(int rx, int rz)
    {
        return Path.Combine(RegionsDirectory, $"r.{rx}.{rz}.json");
    }
}
