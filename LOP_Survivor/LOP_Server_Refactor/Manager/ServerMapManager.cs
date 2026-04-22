
using Island;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public class ServerMapManager
{
    private static ServerMapManager instance;
    public static ServerMapManager Instance => instance ??= new ServerMapManager();

    public const int RegionSizeInChunks = 8;

    public ConcurrentDictionary<string, ServerChunk> Chunks { get; } = new();

    private readonly ConcurrentDictionary<string, byte> _dirtyRegions = new();

    private int _seed = 1234;

    // 지형 설정을 위한 상수
    private const int maxWaterHeight = 7;
    private const int maxStoneHeight = 7;

    public void ConfigureSeed(int seed)
    {
        _seed = seed;
    }

    public void GenerateInitialMap(int radius, int seed)
    {
        ConfigureSeed(seed);

        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                GetOrGenerateChunk(x, z, seed);
            }
        }
    }

    public ServerChunk GetOrGenerateChunk(int cx, int cz, int seed)
    {
        string key = GetChunkKey(cx, cz);
        if (Chunks.TryGetValue(key, out var chunk))
            return chunk;

        var newChunk = new ServerChunk(cx, cz);
        GenerateChunkData(newChunk, seed);
        GenerateResourceClusters(newChunk, seed);

        Chunks[key] = newChunk;
        return newChunk;
    }

    public ServerChunk GetOrLoadChunk(int cx, int cz)
    {
        string key = GetChunkKey(cx, cz);
        if (Chunks.TryGetValue(key, out var chunk))
            return chunk;

        chunk = SaveManager.LoadChunk(cx, cz);
        if (chunk != null)
        {
            Chunks[key] = chunk;
            return chunk;
        }

        return GetOrGenerateChunk(cx, cz, _seed);
    }

    public void AddOrReplaceChunk(ServerChunk chunk, bool markDirty = false)
    {
        Chunks[GetChunkKey(chunk.X, chunk.Z)] = chunk;

        if (markDirty)
        {
            chunk.IsDirty = true;
            MarkRegionDirtyByChunk(chunk.X, chunk.Z);
        }
        else
        {
            chunk.IsDirty = false;
        }
    }

    public void ClearAll(bool clearDirtyRegions = true)
    {
        Chunks.Clear();
        if (clearDirtyRegions)
            _dirtyRegions.Clear();
    }

    public void MarkAllRegionsDirty()
    {
        foreach (var chunk in Chunks.Values)
        {
            chunk.IsDirty = true;
            MarkRegionDirtyByChunk(chunk.X, chunk.Z);
        }
    }

    public List<(int rx, int rz)> GetDirtyRegions()
    {
        var result = new List<(int rx, int rz)>();

        foreach (var key in _dirtyRegions.Keys)
        {
            if (TryParseRegionKey(key, out int rx, out int rz))
                result.Add((rx, rz));
        }

        return result;
    }

    public void ClearDirtyRegion(int rx, int rz)
    {
        _dirtyRegions.TryRemove(GetRegionKey(rx, rz), out _);

        foreach (var chunk in GetChunksInRegion(rx, rz))
            chunk.IsDirty = false;
    }

    public IEnumerable<ServerChunk> GetChunksInRegion(int rx, int rz)
    {
        foreach (var chunk in Chunks.Values)
        {
            int chunkRegionX = FloorDiv(chunk.X, RegionSizeInChunks);
            int chunkRegionZ = FloorDiv(chunk.Z, RegionSizeInChunks);

            if (chunkRegionX == rx && chunkRegionZ == rz)
                yield return chunk;
        }
    }

    public void UpdateBlock(int worldX, int worldY, int worldZ, string newBlockId)
    {
        if (worldY < 0 || worldY >= ChunkConfig.ChunkHeightValue)
            return;

        int cx = FloorDiv(worldX, ChunkConfig.ChunkWidthValue);
        int cz = FloorDiv(worldZ, ChunkConfig.ChunkLengthValue);

        int lx = Mod(worldX, ChunkConfig.ChunkWidthValue);
        int lz = Mod(worldZ, ChunkConfig.ChunkLengthValue);

        var chunk = GetOrLoadChunk(cx, cz);
        if (chunk == null)
            return;

        if (chunk.Blocks[lx, worldY, lz] == newBlockId)
            return;

        chunk.Blocks[lx, worldY, lz] = newBlockId;
        chunk.IsDirty = true;
        MarkRegionDirtyByChunk(cx, cz);
    }

    public static string GetChunkKey(int cx, int cz) => $"{cx}_{cz}";
    public static string GetRegionKey(int rx, int rz) => $"{rx}_{rz}";

    public static void GetRegionCoordsByChunk(int cx, int cz, out int rx, out int rz)
    {
        rx = FloorDiv(cx, RegionSizeInChunks);
        rz = FloorDiv(cz, RegionSizeInChunks);
    }

    public static string GetRegionKeyByChunk(int cx, int cz)
    {
        GetRegionCoordsByChunk(cx, cz, out int rx, out int rz);
        return GetRegionKey(rx, rz);
    }

    public static bool TryParseRegionKey(string key, out int rx, out int rz)
    {
        rx = 0;
        rz = 0;

        if (string.IsNullOrWhiteSpace(key))
            return false;

        var split = key.Split('_');
        if (split.Length != 2)
            return false;

        return int.TryParse(split[0], out rx) && int.TryParse(split[1], out rz);
    }

    public static int FloorDiv(int value, int divisor)
    {
        int quotient = value / divisor;
        int remainder = value % divisor;

        if (remainder != 0 && ((value < 0) != (divisor < 0)))
            quotient--;

        return quotient;
    }

    public static int Mod(int value, int divisor)
    {
        int mod = value % divisor;
        return mod < 0 ? mod + divisor : mod;
    }

    private void MarkRegionDirtyByChunk(int cx, int cz)
    {
        _dirtyRegions[GetRegionKeyByChunk(cx, cz)] = 0;
    }

    private void GenerateChunkData(ServerChunk chunk, int seed)
    {
        float scale = 30.0f;

        for (int y = 0; y < ChunkConfig.ChunkHeightValue; y++)
        {
            for (int x = 0; x < ChunkConfig.ChunkWidthValue; x++)
            {
                for (int z = 0; z < ChunkConfig.ChunkLengthValue; z++)
                {
                    float worldX = (chunk.X * ChunkConfig.ChunkWidthValue) + x;
                    float worldZ = (chunk.Z * ChunkConfig.ChunkLengthValue) + z;

                    float noise = Perlin.Noise(worldX / scale + seed, worldZ / scale + seed);
                    int height = (int)(noise * ChunkConfig.ChunkHeightValue);
                    height = Math.Max(1, height);

                    if (y < height)
                    {
                        if (y < maxStoneHeight && y < height - 3)
                            chunk.Blocks[x, y, z] = BlockConstants.Stone;
                        else
                            chunk.Blocks[x, y, z] = BlockConstants.Ground;
                    }
                    else if (y <= maxWaterHeight)
                    {
                        if (y == height)
                            chunk.Blocks[x, y, z] = BlockConstants.Ground;
                        else
                            chunk.Blocks[x, y, z] = BlockConstants.Water;
                    }
                    else
                    {
                        if (y == height)
                        {
                            float typeNoise = Perlin.Noise(worldX * 0.1f, worldZ * 0.1f);
                            chunk.Blocks[x, y, z] = (typeNoise > 0.5f) ? BlockConstants.Snow : BlockConstants.Ground;
                        }
                        else
                        {
                            chunk.Blocks[x, y, z] = BlockConstants.Air;
                        }
                    }
                }
            }
        }
    }

    private void GenerateResourceClusters(ServerChunk chunk, int seed)
    {
        Random rand = new Random(seed + chunk.X * 1000 + chunk.Z);

        // BlockConstants에 없는 값은 제외
        string[] ores =
        {
            BlockConstants.GoldOre,
            BlockConstants.SilverOre,
            BlockConstants.Stone
        };

        for (int i = 0; i < 5; i++)
        {
            int rx = rand.Next(0, ChunkConfig.ChunkWidthValue);
            int rz = rand.Next(0, ChunkConfig.ChunkLengthValue);
            int ry = rand.Next(5, Math.Max(6, maxStoneHeight));

            if (chunk.Blocks[rx, ry, rz] == BlockConstants.Stone)
            {
                string targetOre = ores[rand.Next(ores.Length)];
                CreateCluster(chunk, rx, ry, rz, targetOre, rand);
            }
        }
    }

    private void CreateCluster(ServerChunk chunk, int x, int y, int z, string id, Random rand)
    {
        int size = rand.Next(2, 5);

        int boundX = chunk.Blocks.GetLength(0);
        int boundY = chunk.Blocks.GetLength(1);
        int boundZ = chunk.Blocks.GetLength(2);

        for (int dx = -size; dx <= size; dx++)
        {
            for (int dy = -size; dy <= size; dy++)
            {
                for (int dz = -size; dz <= size; dz++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    int nz = z + dz;

                    if (nx >= 0 && nx < boundX &&
                        ny >= 0 && ny < boundY &&
                        nz >= 0 && nz < boundZ)
                    {
                        if (chunk.Blocks[nx, ny, nz] == BlockConstants.Stone && rand.NextDouble() > 0.4)
                            chunk.Blocks[nx, ny, nz] = id;
                    }
                }
            }
        }
    }
}

public class ServerChunk
{
    public int X { get; }
    public int Z { get; }
    public string[,,] Blocks { get; }
    public bool IsDirty { get; set; }

    public ServerChunk(int x, int z)
    {
        X = x;
        Z = z;
        Blocks = new string[ChunkConfig.ChunkWidthValue, ChunkConfig.ChunkHeightValue, ChunkConfig.ChunkLengthValue];
        IsDirty = false;
    }
}
