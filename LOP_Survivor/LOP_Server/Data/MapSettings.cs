using System;

[Serializable]
public class MapSettings
{
    // 맵 기본 설정
    public int Seed { get; set; } = 1234;
    public int MapRadius { get; set; } = 10;

    // 지형 노이즈 설정
    public float NoiseScale { get; set; } = 30.0f;
    public float SurfaceNoiseScale { get; set; } = 0.1f;

    // 높이 제한 및 해수면 상수
    public int MaxWaterHeight { get; set; } = 7;
    public int MaxStoneHeight { get; set; } = 7;

    // 청크 설정
    public int ChunkWidth { get; set; } = 10;
    public int ChunkHeight { get; set; } = 25;
    public int ChunkLength { get; set; } = 10;
}