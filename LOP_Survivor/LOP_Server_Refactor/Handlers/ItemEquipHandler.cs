using System.Net;
using GameSync;

/// <summary>
/// 아이템 장착 이벤트 처리.
/// 모든 클라에 즉시 전달.
/// </summary>
public class ItemEquipHandler : IMessageHandler
{
    public void Handle(GameMessage msg, IPEndPoint sender, ServerContext ctx)
    {
        var id = msg.PlayerId;
        ctx.Broadcast(msg, id);
    }
}
