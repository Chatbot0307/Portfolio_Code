
using System;
using System.Net;
using GameSync;

public class GameServer
{
    private readonly UdpTransport transport = new();
    private readonly MessageDispatcher dispatcher = new();
    private readonly ServerContext context;

    private readonly WorldSaveService _worldService;
    private readonly ServerBackgroundService _bgService;
    private readonly PlayerSaveService _playerSaveService;
    private readonly PlayerDataManager _playerDataManager = new();

    private bool _stopped;

    public GameServer()
    {
        context = new ServerContext(transport);

        _worldService = new WorldSaveService(context);
        _playerSaveService = new PlayerSaveService(context, _playerDataManager);
        _bgService = new ServerBackgroundService(context, _worldService, _playerSaveService);

        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.Join, new JoinHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.Leave, new LeaveHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.Position, new PositionHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.AnimState, new AnimHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.AnimTrigger, new AnimHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.ItemEquip, new ItemEquipHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.Rpc, new RpcBroadcastHandler(context));
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.ChatMessage, new ChatMessageHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.NetworkInstantiate, new InstantiateHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.MapBlockUpdate, new MapBlockUpdateHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.ObjectDestroyMessage, new ObjectDestroyHandler());
        dispatcher.RegisterHandler(GameMessage.PayloadOneofCase.MapChunkData, new MapChunkHandler());

        transport.OnDataReceived += OnDataReceived;
    }

    public void Start(int port)
    {
        MapConfigManager.Instance.LoadConfig();

        int radius = MapConfigManager.Instance.Settings?.MapRadius ?? 10;
        int seed = MapConfigManager.Instance.Settings?.Seed ?? 1330;

        _worldService.InitializeWorld(radius, seed);
        context.TickManager.Start();
        _bgService.Start();

        transport.Start(port);
        Console.WriteLine($"[Server] started. port={port}, radius={radius}, seed={seed}");
    }

    public void Stop()
    {
        if (_stopped)
            return;

        _stopped = true;

        _bgService.Stop();
        context.TickManager.Stop();
        SaveManager.SaveAll(context);
        transport.Stop();

        Console.WriteLine("[Server] stopped safely.");
    }

    private void OnDataReceived(byte[] data, IPEndPoint sender)
    {
        try
        {
            var msg = GameMessage.Parser.ParseFrom(data);

            if (!string.IsNullOrEmpty(msg.PlayerId))
                context.UpdateHeartbeat(msg.PlayerId);

            dispatcher.Dispatch(msg, sender, context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Server] packet handling failed from {sender}: {ex.Message}");
        }
    }

    public ServerContext GetContext() => context;
}
