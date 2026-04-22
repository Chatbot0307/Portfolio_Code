
using System;
using System.Net;
using GameSync;

/// <summary>
/// 맵 블록 업데이트 메시지 핸들러: 서버 상태에 변경사항 저장 후 브로드캐스트.
/// </summary>
public class MapBlockUpdateHandler : IMessageHandler
{
    public void Handle(GameMessage msg, IPEndPoint sender, ServerContext ctx)
    {
        var update = msg.MapBlockUpdate;
        if (update == null)
            return;

        ServerMapManager.Instance.UpdateBlock(update.X, update.Y, update.Z, update.NewBlockId);
        ctx.Broadcast(msg, msg.PlayerId);

        Console.WriteLine($"[Server] BlockUpdate: ({update.X},{update.Y},{update.Z}) to {update.NewBlockId}");
    }
}
