using System;
using System.Net;
using Google.Protobuf;
using GameSync;

public class GameServer
{
    private readonly UdpTransport transport = new();
    private readonly MessageDispatcher dispatcher = new();
    private readonly ServerContext context;

    public GameServer()
    {
        context = new ServerContext(transport);

        // 메시지 처리 핸들러 등록
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.Join, new JoinHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.Leave, new LeaveHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.Position, new PositionHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.AnimState, new AnimHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.AnimTrigger, new AnimHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.ItemEquip, new ItemEquipHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.Rpc, new RpcBroadcastHandler(context));
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.ChatMessage, new ChatMessageHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.NetworkInstantiate, new InstantiateHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.MapBlockUpdate, new MapBlockUpdateHandler(context));
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.ObjectDestroyMessage, new ObjectDestroyHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.BlockDestroyFlag, new BlockDestroyFlagHandler(context));

        transport.OnDataReceived += OnDataReceived;
    }

    public void Start(int port) => transport.Start(port);
    public void Stop() => transport.Stop();

    private void OnDataReceived(byte[] data, IPEndPoint sender)
    {
        try
        {
            var msg = GameMessage.Parser.ParseFrom(data);
            dispatcher.Dispatch(msg, sender, context);
        }
        catch
        {
            Console.WriteLine("Invalid packet received.");
        }
    }
}
