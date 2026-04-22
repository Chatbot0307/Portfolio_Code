using System.Collections.Generic;
using Island;

public static class VoxelData
{
    /// <summary>
    /// 3차원 배열 데이터를 1차원 리스트로 변환 (Flatten)
    /// </summary>
    public static List<string> Flatten(string[,,] blocks)
    {
        var flat = new List<string>(ChunkConfig.ChunkHeightValue * ChunkConfig.ChunkWidthValue * ChunkConfig.ChunkLengthValue);

        // 순서는 루프 효율성과 일관성을 위해 Y -> X -> Z 형식을 유지합니다.
        for (int y = 0; y < ChunkConfig.ChunkHeightValue; y++)
            for (int x = 0; x < ChunkConfig.ChunkWidthValue; x++)
                for (int z = 0; z < ChunkConfig.ChunkLengthValue; z++)
                    flat.Add(blocks[x, y, z]);

        return flat;
    }

    /// <summary>
    /// 1차원 리스트 데이터를 3차원 배열로 복원 (Unflatten)
    /// </summary>
    public static void Unflatten(IList<string> flatData, string[,,] targetBlocks)
    {
        int idx = 0;
        for (int y = 0; y < ChunkConfig.ChunkHeightValue; y++)
            for (int x = 0; x < ChunkConfig.ChunkWidthValue; x++)
                for (int z = 0; z < ChunkConfig.ChunkLengthValue; z++)
                    targetBlocks[x, y, z] = flatData[idx++];
    }
}