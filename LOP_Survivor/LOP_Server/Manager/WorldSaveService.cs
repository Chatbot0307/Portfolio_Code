
using System;
using System.Collections.Generic;

public class WorldSaveService
{
    private readonly ServerContext _ctx;

    public WorldSaveService(ServerContext ctx)
    {
        _ctx = ctx;
    }

    public void RunAutoSaveIfNecessary()
    {
        List<(int rx, int rz)> dirtyRegions = ServerMapManager.Instance.GetDirtyRegions();
        if (dirtyRegions.Count == 0)
            return;

        foreach (var (rx, rz) in dirtyRegions)
        {
            SaveManager.SaveRegion(rx, rz);
            ServerMapManager.Instance.ClearDirtyRegion(rx, rz);
        }

        SaveManager.SaveState(_ctx);
        Console.WriteLine($"[WorldSave] dirty regions saved: {dirtyRegions.Count}");
    }

    public void InitializeWorld(int radius, int seed)
    {
        ServerMapManager.Instance.ConfigureSeed(seed);
        SaveManager.LoadAll(_ctx);

        if (ServerMapManager.Instance.Chunks.IsEmpty)
        {
            Console.WriteLine("[WorldSave] 저장된 월드가 없어 초기 생성을 시작합니다.");
            ServerMapManager.Instance.GenerateInitialMap(radius, seed);
            SaveManager.SaveAll(_ctx);
            return;
        }

        Console.WriteLine($"[WorldSave] 저장된 월드를 로드했습니다. chunk count: {ServerMapManager.Instance.Chunks.Count}");
    }
}
