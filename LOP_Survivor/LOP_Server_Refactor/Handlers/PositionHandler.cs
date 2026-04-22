using System.Net;
using GameSync;

/// <summary>
/// 위치 패킷 처리.
/// Sequence 번호가 더 큰 최신 패킷만 반영.
/// </summary>
public class PositionHandler : IMessageHandler
{
    public void Handle(GameMessage msg, IPEndPoint sender, ServerContext ctx)
    {
        //var networkId = msg.Position.NetworkId;
        //var seq = msg.Position.Sequence;

        //if (!ctx.LastSeq.ContainsKey(networkId) || seq > ctx.LastSeq[networkId])
        //{
        //    ctx.LastSeq[networkId] = seq;
        //    ctx.Broadcast(msg, msg.PlayerId);
        //}
        //else
        //{
        //    // 오래된 패킷은 무시
        //}

        ctx.Broadcast(msg, msg.PlayerId);
    }
}
