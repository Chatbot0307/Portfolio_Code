using System;
using System.Net;
using GameSync;

/// <summary>
/// 클라이언트의 NetworkInstantiate 요청을 받아 ID를 할당하고 브로드캐스트합니다.
/// </summary>
public class InstantiateHandler : IMessageHandler
{
    public void Handle(GameMessage msg, IPEndPoint sender, ServerContext ctx)
    {
        var request = msg.NetworkInstantiate;
        if (request == null) return;

        int newId = ctx.GetNextNetworkId(msg.PlayerId, request);

        request.NetworkId = newId;
        request.OwnerPlayerId = msg.PlayerId;

        ctx.Broadcast(msg);

        Console.WriteLine($"[Server] NetworkInstantiate: ID={newId}, PrefabHash={request.PrefabHash}, Owner={msg.PlayerId}");
    }
}