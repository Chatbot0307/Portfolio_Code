using System.Collections.Concurrent;
using System.Net;
using GameSync;
using Google.Protobuf;
using System.Threading;
using System;
using System.Linq;

/// <summary>
/// 서버의 런타임 세션과 네트워크 오브젝트 상태를 관리합니다.
/// 맵 데이터는 ServerMapManager에서 별도로 관리합니다.
/// </summary>
public class ServerContext
{
    // 세션 관리
    public ConcurrentDictionary<string, IPEndPoint> Clients { get; } = new();
    public ConcurrentDictionary<string, string> Nicknames { get; } = new();
    public string FirstPlayerId { get; internal set; } = null;

    // 플레이어 저장 데이터 
    public ConcurrentDictionary<string, PlayerData> PlayerDataCache { get; } = new();

    // 오브젝트 상태
    private int nextNetworkId = 1;
    public ConcurrentDictionary<int, string> NetworkObjects { get; } = new(); // ID -> OwnerPlayerId
    public ConcurrentDictionary<int, GameSync.NetworkInstantiate> NetworkObjectDetails { get; } = new();
    public ConcurrentDictionary<int, int> LastSeq { get; } = new(); // 패킷 순서 보장용

    // 시간/틱 관리
    public ServerTickManager TickManager { get; private set; }
    public float ElapsedTicks { get; set; } = 0.0f;
    public ConcurrentDictionary<string, DateTime> LastHeartbeat { get; } = new();

    private readonly UdpTransport transport;

    public ServerContext(UdpTransport transport)
    {
        this.transport = transport;
        this.TickManager = new ServerTickManager(this);
    }

    // 메시지 전송 기능
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

    // ID 발급 및 관리
    public int GetNextNetworkId(string playerId)
    {
        int id = Interlocked.Increment(ref nextNetworkId) - 1; 
        NetworkObjects[id] = playerId;
        return id;
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

    public void SetNextNetworkId(int lastId)
    {
        Interlocked.Exchange(ref nextNetworkId, lastId + 1);
    }

    // 클라이언트 접속 확인 로직
    public void UpdateHeartbeat(string playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return;

        LastHeartbeat[playerId] = DateTime.UtcNow;
    }

    // 권한 분배 로직
    public void RebalanceMonsterAuthority()
    {
        var clientIds = Clients.Keys.ToList();
        if (clientIds.Count == 0) return;

        int index = 0;
        // 몬스터 판별
        foreach (var netId in NetworkObjectDetails.Keys)
        {
            string assignedOwner = clientIds[index % clientIds.Count];

            NetworkObjects[netId] = assignedOwner;
            NetworkObjectDetails[netId].OwnerPlayerId = assignedOwner;

            Broadcast(new GameMessage
            {
                TransferOwnership = new TransferOwnership
                {
                    NetworkId = netId,
                    NewOwnerPlayerId = assignedOwner
                }
            });
            index++;
        }
    }

    public void ResetState()
    {
        LastSeq.Clear();
        NetworkObjects.Clear();
        NetworkObjectDetails.Clear();
        Nicknames.Clear();
        Interlocked.Exchange(ref nextNetworkId, 1);
        FirstPlayerId = null;
        TickManager.Stop();
        ElapsedTicks = 0.0f;
        Console.WriteLine("[Server] Last clients left. State has been reset.");
    }
}