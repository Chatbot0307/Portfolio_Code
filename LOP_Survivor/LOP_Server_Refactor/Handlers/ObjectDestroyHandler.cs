using System;
using System.Net;
using GameSync;

/// <summary>
/// 네트워크 객체 파괴 요청을 처리합니다.
/// 서버 상태(NetworkObjectDetails)에서 객체를 제거하고 모든 클라이언트에게 브로드캐스트합니다.
/// </summary>
public class ObjectDestroyHandler : IMessageHandler
{
    public void Handle(GameMessage msg, IPEndPoint sender, ServerContext ctx)
    {
        var destroyMsg = msg.ObjectDestroyMessage;
        if (destroyMsg == null) return;

        int networkId = destroyMsg.NetworkId;
        string playerId = msg.PlayerId;

        // 1. 서버 상태에서 객체 제거
        if (ctx.NetworkObjectDetails.TryRemove(networkId, out var details))
        {
            ctx.NetworkObjects.TryRemove(networkId, out _); // NetworkObjects 딕셔너리에서도 제거

            ctx.LastSeq.TryRemove(networkId, out _);

            Console.WriteLine($"[Server] Network Object Destroyed: ID={networkId}, Requested by {playerId}");

            // 2. 파괴 메시지를 모든 클라이언트에게 브로드캐스트
            ctx.Broadcast(msg);
        }
        else
        {
            Console.WriteLine($"[Server Warning] Destroy request for unknown or already destroyed object: ID={networkId}");
        }
    }
}