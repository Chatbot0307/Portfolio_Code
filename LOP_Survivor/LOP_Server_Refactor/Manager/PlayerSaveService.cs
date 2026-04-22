using System;

public class PlayerSaveService
{
    private readonly ServerContext _ctx;
    private readonly PlayerDataManager _pdm;

    private DateTime _lastAutoSaveUtc = DateTime.UtcNow;
    private readonly TimeSpan _autoSaveInterval;

    public PlayerSaveService(ServerContext ctx, PlayerDataManager pdm, TimeSpan? autoSaveInterval = null)
    {
        _ctx = ctx;
        _pdm = pdm;
        _autoSaveInterval = autoSaveInterval ?? TimeSpan.FromSeconds(5);
    }

    // Join에서 호출: 로드(없으면 생성) 후 캐시에 넣기
    public PlayerData LoadOrCreate(string playerId, string nickname, float defaultX = 0, float defaultY = 5, float defaultZ = 0)
    {
        var data = _pdm.Load(playerId);
        if (data == null)
        {
            data = new PlayerData(playerId, nickname, defaultX, defaultY, defaultZ);
            // 생성자는 Dirty=true로 만들어 둠
        }
        else
        {
            // 닉네임 동기화 정책은 취향: 최신 닉으로 갱신하고 Dirty
            if (!string.Equals(data.Nickname, nickname, StringComparison.Ordinal))
            {
                data.Nickname = nickname;
                data.Dirty = true;
            }
        }

        data.LastLogin = DateTime.Now;
        data.Dirty = true;

        _ctx.PlayerDataCache[playerId] = data;
        return data;
    }

    // BackgroundService가 호출
    public void RunAutoSaveIfNecessary()
    {
        var now = DateTime.UtcNow;
        if (now - _lastAutoSaveUtc < _autoSaveInterval) return;

        SaveDirtyPlayers();
        _lastAutoSaveUtc = now;
    }

    // Dirty 체크 & 저장
    public void SaveDirtyPlayers()
    {
        foreach (var kv in _ctx.PlayerDataCache)
        {
            var data = kv.Value;
            if (!data.Dirty) continue;

            _pdm.Save(data);
            data.Dirty = false;
        }
    }

    // Leave/타임아웃/서버종료 시 즉시 저장
    public void SaveNow(string playerId)
    {
        if (_ctx.PlayerDataCache.TryGetValue(playerId, out var data))
        {
            _pdm.Save(data);
            data.Dirty = false;
        }
    }

    public void RemoveFromCache(string playerId)
    {
        _ctx.PlayerDataCache.TryRemove(playerId, out _);
    }
}