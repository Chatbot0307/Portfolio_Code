using System.Net;
using GameSync;

public class RpcBroadcastHandler : IMessageHandler
{
    private readonly ServerContext ctx;
    public RpcBroadcastHandler(ServerContext ctx) => this.ctx = ctx;

    public void Handle(GameMessage msg, IPEndPoint sender, ServerContext ctx)
    {
        var rpc = msg.Rpc;
        if (rpc == null) return;

        this.ctx.Broadcast(msg);
    }
}
