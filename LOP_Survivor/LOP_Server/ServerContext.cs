using System.Collections.Concurrent;
using System.Net;
using GameSync;
using Google.Protobuf;
using System.Threading;
using System;

/// <summary>
/// 서버 전체 상태를 보관하고,
/// 메시지 전송/브로드캐스트 기능 제공.
/// </summary>
public class ServerContext
{
    public ConcurrentDictionary<string, IPEndPoint> Clients { get; } = new();

    public ConcurrentDictionary<string, string> Nicknames { get; } = new();

    public ConcurrentDictionary<int, int> LastSeq { get; } = new();

    private int nextNetworkId = 1;

    public ConcurrentDictionary<int, string> NetworkObjects { get; } = new();
    public ConcurrentDictionary<string, string> ChangedBlocks { get; } = new();

    public ConcurrentDictionary<int, GameSync.NetworkInstantiate> NetworkObjectDetails { get; } = new();

    public string FirstPlayerId { get; internal set; } = null;

    public ServerTickManager TickManager { get; private set; }

    public float ElapsedTicks { get; set; } = 0.0f;

    private readonly UdpTransport transport;

    public ServerContext(UdpTransport transport)
    {
        this.transport = transport;
        // 틱 관리자 초기화
        this.TickManager = new ServerTickManager(this);
    }

    public void Send(GameMessage msg, IPEndPoint ep)
    {
        transport.SendAsync(msg.ToByteArray(), ep);
    }

    public void Broadcast(GameMessage msg, string exceptPlayerId = null)
    {
        var data = msg.ToByteArray();
        foreach (var kv in Clients)
        {
            if (kv.Key == exceptPlayerId) continue;
            transport.SendAsync(data, kv.Value);
        }
    }

    public int GetNextNetworkId(string ownerPlayerId)
    {
        int newId = Interlocked.Increment(ref nextNetworkId);
        NetworkObjects[newId] = ownerPlayerId;
        return newId;
    }

    public int GetNextNetworkId(string ownerPlayerId, GameSync.NetworkInstantiate instantiateDetails)
    {
        int newId = Interlocked.Increment(ref nextNetworkId);

        NetworkObjects[newId] = ownerPlayerId;

        var details = new GameSync.NetworkInstantiate(instantiateDetails);

        details.NetworkId = newId;
        details.OwnerPlayerId = ownerPlayerId;

        NetworkObjectDetails[newId] = details;

        return newId;
    }

    public GameSync.NetworkObjectList GetCurrentNetworkObjects()
    {
        var list = new GameSync.NetworkObjectList();

        list.Objects.AddRange(NetworkObjectDetails.Values);

        return list;
    }

    public static string BlockPosToKey(int x, int y, int z) => $"{x}_{y}_{z}";

    public static bool TryParseBlockKey(string key, out int x, out int y, out int z)
    {
        x = y = z = 0;
        string[] parts = key.Split('_');
        if (parts.Length != 3) return false;

        return int.TryParse(parts[0], out x) &&
               int.TryParse(parts[1], out y) &&
               int.TryParse(parts[2], out z);
    }

    public void ResetState()
    {
        LastSeq.Clear();
        NetworkObjects.Clear();
        NetworkObjectDetails.Clear();
        ChangedBlocks.Clear();
        Nicknames.Clear();

        Interlocked.Exchange(ref nextNetworkId, 0);

        FirstPlayerId = null;

        TickManager.Stop();
        ElapsedTicks = 0.0f;

        Console.WriteLine("[Server] Last client left. Server state has been reset.");
    }
}
