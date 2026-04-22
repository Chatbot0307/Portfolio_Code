using System.Net;
using System.Collections.Generic;
using GameSync;
using System;

/// <summary>
/// 패킷을 해석해 올바른 핸들러로 전달.
/// </summary>
public class MessageDispatcher
{
    private readonly Dictionary<GameMessage.PayloadOneofCase, IMessageHandler> handlers = new();

    public void RegisterHandler(GameMessage.PayloadOneofCase type, IMessageHandler handler)
    {
        handlers[type] = handler;
    }

    public void Dispatch(GameMessage msg, IPEndPoint sender, ServerContext ctx)
    {
        if (handlers.TryGetValue(msg.PayloadCase, out var handler))
        {
            handler.Handle(msg, sender, ctx);
        }
        else
        {
            Console.WriteLine($"No handler for message type: {msg.PayloadCase}");
        }
    }
}
