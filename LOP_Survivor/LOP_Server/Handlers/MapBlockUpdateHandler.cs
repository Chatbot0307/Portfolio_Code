using System;
using System.Net;
using GameSync;

/// <summary>
/// 맵 블록 업데이트 메시지 핸들러: 서버 상태에 변경사항 저장 후 브로드캐스트.
/// </summary>
public class MapBlockUpdateHandler : IMessageHandler
{
    private readonly ServerContext ctx;
    public MapBlockUpdateHandler(ServerContext ctx) => this.ctx = ctx;

    public void Handle(GameMessage msg, IPEndPoint sender, ServerContext ctx)
    {
        var update = msg.MapBlockUpdate;
        if (update == null) return;

        // 1. 서버 상태에 블록 변경 사항 저장 (핵심)
        string key = ServerContext.BlockPosToKey(update.X, update.Y, update.Z);
        ctx.ChangedBlocks[key] = update.NewBlockId;

        Console.WriteLine($"[Server] Block updated at {key} to {update.NewBlockId} by {msg.PlayerId}. Stored in server context.");

        // 2. 호출한 클라이언트 제외하고 모든 클라이언트에게 브로드캐스트
        this.ctx.Broadcast(msg);
    }
}