using System.Net;
using GameSync;

/// <summary>
/// 모든 메시지 핸들러의 공통 인터페이스.
/// </summary>
public interface IMessageHandler
{
    void Handle(GameMessage msg, IPEndPoint sender, ServerContext ctx);
}
