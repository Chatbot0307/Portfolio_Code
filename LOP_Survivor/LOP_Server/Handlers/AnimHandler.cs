using System.Net;
using GameSync;

/// <summary>
/// 애니메이션 상태/트리거는 바로 브로드캐스트.
/// </summary>
public class AnimHandler : IMessageHandler
{
    public void Handle(GameMessage msg, IPEndPoint sender, ServerContext ctx)
    {
        var id = msg.PlayerId;
        ctx.Broadcast(msg);
    }
}
