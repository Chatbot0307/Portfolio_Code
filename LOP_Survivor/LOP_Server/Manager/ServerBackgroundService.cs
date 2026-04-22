
using System;
using System.Linq;
using System.Threading;

public class ServerBackgroundService
{
    private readonly ServerContext _ctx;
    private readonly WorldSaveService _saveManager;
    private readonly PlayerSaveService _playerSave;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _timeoutLimit = TimeSpan.FromSeconds(60);

    private volatile bool _isRunning;
    private Thread _bgThread;

    public ServerBackgroundService(ServerContext ctx, WorldSaveService saveManager, PlayerSaveService playerSave)
    {
        _ctx = ctx;
        _saveManager = saveManager;
        _playerSave = playerSave;
    }

    public void Start()
    {
        if (_isRunning)
            return;

        _isRunning = true;
        _bgThread = new Thread(RunLoop)
        {
            IsBackground = true,
            Name = "ServerBackgroundService"
        };
        _bgThread.Start();

        Console.WriteLine("[Service] 백그라운드 관리 서비스가 시작되었습니다.");
    }

    private void RunLoop()
    {
        while (_isRunning)
        {
            try
            {
                Thread.Sleep(_checkInterval);

                if (!_isRunning)
                    break;

                _saveManager.RunAutoSaveIfNecessary();
                _playerSave.RunAutoSaveIfNecessary();
                CheckClientHeartbeats();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Service Error] 백그라운드 루프 예외 발생: {ex.Message}");
            }
        }
    }

    private void CheckClientHeartbeats()
    {
        var now = DateTime.UtcNow;
        var timeoutPlayers = _ctx.LastHeartbeat
            .Where(kv => now - kv.Value > _timeoutLimit)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var playerId in timeoutPlayers)
        {
            Console.WriteLine($"[Heartbeat] 유저 {playerId}의 응답이 없어 세션을 종료합니다.");

            _playerSave.SaveNow(playerId);
            _playerSave.RemoveFromCache(playerId);

            _ctx.Clients.TryRemove(playerId, out _);
            _ctx.LastHeartbeat.TryRemove(playerId, out _);
            _ctx.Nicknames.TryRemove(playerId, out _);

            _ctx.RebalanceMonsterAuthority();
        }
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;

        if (_bgThread != null && _bgThread.IsAlive)
            _bgThread.Join(TimeSpan.FromSeconds(2));

        Console.WriteLine("[Service] 백그라운드 서비스를 중지합니다.");
    }
}
