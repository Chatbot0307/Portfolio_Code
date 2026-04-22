//using System;
//using System.Net;
//using GameSync;

//public class BlockDestroyFlagHandler : IMessageHandler
//{
//    private readonly ServerContext ctx;
//    public BlockDestroyFlagHandler(ServerContext ctx) => this.ctx = ctx;

//    public void Handle(GameMessage msg, IPEndPoint sender, ServerContext ctx)
//    {
//        var destroyFlag = msg.BlockDestroyFlag;
//        if (destroyFlag == null) return;

//        string key = ServerContext.BlockPosToKey(destroyFlag.X, destroyFlag.Y, destroyFlag.Z);

//        if (destroyFlag.IsDestroyed)
//        {
//            ctx.ChangedBlocks[key] = "DESTROYED";
//            Console.WriteLine($"[Server] Block DESTROYED at {key} by {msg.PlayerId}");
//        }
//        else
//        {
//            ctx.ChangedBlocks.TryRemove(key, out _);
//            Console.WriteLine($"[Server] Block RESTORED at {key} by {msg.PlayerId}");
//        }

//        this.ctx.Broadcast(msg);
//    }
//}