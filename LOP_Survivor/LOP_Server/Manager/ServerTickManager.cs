using System;
using System.Threading;
using System.Threading.Tasks;
using GameSync;

/// <summary>
/// 서버의 메인 틱 루프를 관리합니다.
/// 1초마다 틱을 증가시키고 모든 클라이언트에게 브로드캐스트합니다.
/// </summary>
public class ServerTickManager
{
    private readonly ServerContext ctx;
    private CancellationTokenSource cts;
    private Task tickLoopTask;
    private const int TickIntervalMs = 1000; // 1초

    public ServerTickManager(ServerContext context)
    {
        ctx = context;
    }

    public void Start()
    {
        if (tickLoopTask != null) return;

        cts = new CancellationTokenSource();
        tickLoopTask = Task.Run(() => TickLoop(cts.Token));
        Console.WriteLine("[ServerTickManager] 틱 루프 시작.");
    }

    public void Stop()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts = null;
        }
        tickLoopTask = null;
        Console.WriteLine("[ServerTickManager] 틱 루프 중지.");
    }

    private async Task TickLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TickIntervalMs, token);

                if (token.IsCancellationRequested) break;

                // 1. 틱 증가
                ctx.ElapsedTicks += 1.0f;

                // 2. 틱 메시지 생성
                var tickMsg = new GameMessage
                {
                    PlayerId = "SERVER",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    TickSync = new TickSync
                    {
                        ElapsedTicks = ctx.ElapsedTicks
                    }
                };

                // 3. 모든 클라이언트에게 브로드캐스트
                ctx.Broadcast(tickMsg);
            }
            catch (TaskCanceledException)
            {
                break; // 정상적인 중지
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServerTickManager] 틱 루프 오류: {ex.Message}");
            }
        }
    }
}