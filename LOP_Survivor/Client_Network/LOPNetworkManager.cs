using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Google.Protobuf;
using UnityEngine;
using GameSync;
using Island;
using Lop.Survivor;

public class LOPNetworkManager : MonoBehaviour
{
    public static LOPNetworkManager Instance;

    [Header("Server Settings")]
    public string serverIp;
    public int serverPort;

    [Header("Prefabs")]
    public GameObject playerPrefab;

    [Header("Dependencies")]
    [SerializeField] private NetworkPrefabRegister prefabRegister;

    [Header("Spawn Settings")]
    public Transform[] spawnPoints;

    [Header("Chat Settings")]
    public ChatHandler chatHandler;

    private string playerId;
    public bool IsWorldSpawner { get; private set; } = false;

    public bool isConnected = false;

    private LopRpcSystem rpcSystem;

    private UdpClient udp;
    private IPEndPoint serverEp;
    private Thread recvThread;

    private ConcurrentQueue<byte[]> receivedMessageQueue = new ConcurrentQueue<byte[]>();
    private Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    private int lastSentSequence = 0;

    private Dictionary<int, GameObject> networkObjects = new Dictionary<int, GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(spawnPoints[0]);
        }
        else
        {
            NetworkDestroy(gameObject);
            return;
        }

        rpcSystem = new LopRpcSystem();

        rpcSystem.SetObjectResolver(GetNetworkObject);

        // NetworkPrefabRegister 초기화 및 유효성 검사
        if (prefabRegister == null)
        {
            Debug.LogError("[LOPNetworkManager] NetworkPrefabRegister is NULL! Assign the Register component.");
        }
        else
        {
            prefabRegister.Initialize();
        }

        Application.runInBackground = true;
    }

    public GameObject GetNetworkObject(int networkId)
    {
        // players 딕셔너리도 필요하다면 여기서 통합하여 검색해야 합니다. (현재는 networkObjects만 검색)
        networkObjects.TryGetValue(networkId, out var obj);
        return obj;
    }

    private void Start()
    {
        //Connect();

        if (chatHandler == null)
            chatHandler = FindFirstObjectByType<ChatHandler>();
        else { }
    }

    private void Update()
    {
        while (receivedMessageQueue.TryDequeue(out var data))
        {
            OnDataReceived(data);
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("[LOPNetworkManager] Application is quitting. Sending Leave message...");
        Disconnect(); // 이 호출이 이제 자동으로 SendLeave()를 포함합니다.
    }

    // -------------------- 서버 연결 --------------------
    public void Connect(string id, string port)
    {
        string serverIp = id;
        int serverPort = int.Parse(port);
        isConnected = true;

        if (udp != null) Disconnect();

        serverEp = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
        udp = new UdpClient();
        udp.Connect(serverEp);

        recvThread = new Thread(ReceiveLoop) { IsBackground = true };
        recvThread.Start();

        var joinMsg = new GameMessage { Join = new Join() };
        SendRaw(joinMsg);
        Debug.Log("[LOPNetworkManager] 서버 연결 및 Join 메시지 전송.");
    }

    public void Disconnect()
    {
        if (udp == null || !isConnected) return;

        SendLeave();

        isConnected = false;

        udp.Close();
        udp = null;
        if (recvThread != null && recvThread.IsAlive) recvThread.Join();

        Debug.Log("[LOPNetworkManager] Sent Leave message and disconnected.");
    }

    private void ReceiveLoop()
    {
        try
        {
            while (true)
            {
                try
                {
                    var receiveBytes = udp.Receive(ref serverEp);
                    receivedMessageQueue.Enqueue(receiveBytes);
                }
                catch (SocketException e)
                {
                    // 씬 전환 때문에 발생하는 WSACancelBlockingCall 무시
                    if (e.SocketErrorCode != SocketError.Interrupted)
                        Debug.LogError($"[LOPNetworkManager] SocketException: {e.Message}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LOPNetworkManager] ReceiveLoop 종료: {e.Message}");
        }
    }


    // -------------------- 메시지 처리 --------------------
    private void OnDataReceived(byte[] data)
    {
        try
        {
            var msg = GameMessage.Parser.ParseFrom(data);

            switch (msg.PayloadCase)
            {
                case GameMessage.PayloadOneofCase.Join:
                    HandleJoinMessage(msg);
                    break;
                case GameMessage.PayloadOneofCase.Leave:
                    HandleLeaveMessage(msg);
                    break;
                case GameMessage.PayloadOneofCase.Position:
                    HandlePositionMessage(msg);
                    break;
                case GameMessage.PayloadOneofCase.AnimState:
                    HandleAnimationStateMessage(msg);
                    break;
                case GameMessage.PayloadOneofCase.AnimTrigger:
                    HandleAnimationTriggerMessage(msg);
                    break;
                case GameMessage.PayloadOneofCase.ChatMessage:
                    HandleChatMessage(msg);
                    break;
                case GameMessage.PayloadOneofCase.NetworkInstantiate:
                    HandleInstantiateMessage(msg.NetworkInstantiate);
                    break;
                case GameMessage.PayloadOneofCase.NetworkObjectList:
                    HandleNetworkObjectList(msg.NetworkObjectList);
                    break;
                case GameMessage.PayloadOneofCase.MapBlockUpdate:
                    HandleMapBlockUpdate(msg);
                    break;
                case GameMessage.PayloadOneofCase.ObjectDestroyMessage:
                    HandleObjectDestroy(msg.ObjectDestroyMessage);
                    break;
                case GameMessage.PayloadOneofCase.BlockDestroyFlag:
                    HandleBlockDestroyFlag(msg.BlockDestroyFlag);
                    break;
                case GameMessage.PayloadOneofCase.TickSync:
                    HandleTickSync(msg.TickSync);
                    break;
            }

            if (msg.PayloadCase == GameMessage.PayloadOneofCase.Rpc)
                rpcSystem.OnReceive(msg);
        }
        catch (Google.Protobuf.InvalidProtocolBufferException ex)
        {
            Debug.LogError($"[Protobuf 파싱 오류] 원본 데이터 크기: {data.Length}. 서버/클라이언트 gamesync.proto 일치 여부를 확인하세요. 오류 메시지: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[메시지 처리 오류] 수신 패킷 처리 중 예외 발생. \n원본 오류(ToString): {ex.ToString()}");
        }
    }

    // -------------------- Join --------------------
    private void HandleJoinMessage(GameMessage msg)
    {
        string newPlayerId = msg.PlayerId;
        GameSync.Join join = msg.Join;

        if (players.ContainsKey(newPlayerId) && players[newPlayerId] != null)
            return;

        bool isLocalPlayer = string.IsNullOrEmpty(playerId) && players.Count == 0;
        if (isLocalPlayer) playerId = newPlayerId;

        if (isLocalPlayer)
        {
            if (join.HasIsWorldSpawner && join.IsWorldSpawner)
            {
                IsWorldSpawner = true;
                Debug.Log($"[Client] I am the World Spawner!");
            }
            else
            {
                IsWorldSpawner = false;
            }
        }

        SpawnPlayer(newPlayerId, isLocalPlayer);

        if (isLocalPlayer)
        {
            Debug.Log($"[Client] Local Player Joined: {newPlayerId}. IsWorldSpawner: {IsWorldSpawner}");

            if (IsWorldSpawner)
            {
                if (TreeManager.Instance != null)
                {
                    TreeManager.Instance.SpawnTree();
                }
                else
                {
                    Debug.LogError("TreeManager.Instance가 NULL입니다. 씬에 TreeManager가 있는지 확인하세요.");
                }
            }
        }
        else
        {
            Debug.Log($"[Client] Remote Player Joined: {newPlayerId}");
        }
    }

    // -------------------- Leave --------------------
    private void HandleLeaveMessage(GameMessage msg)
    {
        string leaveId = msg.PlayerId;
        if (players.TryGetValue(leaveId, out var obj) && obj != null)
        {
            Destroy(obj);
            players.Remove(leaveId);
            Debug.Log($"[LOPNetworkManager] Player {leaveId} left.");
        }
    }

    // -------------------- TickSync --------------------
    private void HandleTickSync(TickSync tickSync)
    {
        if (Lop.Survivor.TickManager.Instance != null)
        {
            Debug.Log($"[TickSync] Received ElapsedTicks: {tickSync.ElapsedTicks}");
            TickManager.Instance.ServerOnTick(tickSync.ElapsedTicks);
        }
    }

    // -------------------- Position --------------------
    private void HandlePositionMessage(GameMessage msg)
    {
        if (msg.PlayerId == playerId) return;
        if (!players.TryGetValue(msg.PlayerId, out var playerObj) || playerObj == null) return;

        var sync = playerObj.GetComponent<NetworkSyncPlayer>();

        if (sync != null)
        {
            Vector3 pos = new Vector3(msg.Position.X, msg.Position.Y, msg.Position.Z);
            Quaternion rot = Quaternion.Euler(0, msg.Position.RotY, 0);
            sync.ApplyPosition(pos, rot);
        }
    }

    // -------------------- AnimState --------------------
    private void HandleAnimationStateMessage(GameMessage msg)
    {
        if (msg.PlayerId == playerId) return;
        if (!players.TryGetValue(msg.PlayerId, out var playerObj) || playerObj == null) return;

        var sync = playerObj.GetComponent<NetworkSyncPlayer>();
        if (sync != null)
        {
            foreach (var state in msg.AnimState.States)
                sync.ApplyAnimationState(state.Key, state.Value);
        }
    }

    // -------------------- AnimTrigger --------------------
    private void HandleAnimationTriggerMessage(GameMessage msg)
    {
        if (msg.PlayerId == playerId) return;
        if (!players.TryGetValue(msg.PlayerId, out var playerObj) || playerObj == null) return;

        var sync = playerObj.GetComponent<NetworkSyncPlayer>();
        if (sync != null)
        {
            foreach (var trigger in msg.AnimTrigger.Triggers)
                sync.ApplyAnimationTrigger(trigger);
        }
    }

    // -------------------- Spawn Player --------------------
    private void SpawnPlayer(string id, bool isLocal)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[LOPNetworkManager] playerPrefab is NULL! Inspector에서 연결했는지 확인하세요.");
            return;
        }

        Transform spawnPoint = GetSpawnPoint(players.Count);
        var newPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        newPlayer.name = $"Player_{id}";

        var networkIdentity = newPlayer.GetComponent<NetworkIdentity>();
        if (networkIdentity != null)
        {
            networkIdentity.SetIdentity(networkId: 0, isOwner: isLocal);

            foreach (var component in newPlayer.GetComponents<Component>())
            {
                rpcSystem.RegisterRpcComponent(component);
            }
        }
        else
        {
            Debug.LogWarning($"Player Prefab is missing NetworkIdentity component: {newPlayer.name}");
        }

        if (isLocal)
        {
            var controller = newPlayer.GetComponent<CharacterController>();
            if (chatHandler != null)
            {
                chatHandler.SetCharacterController(controller);
                //GameManager.Instance.SetCharacterController(controller);

            }
            else
            {
                if (controller == null) Debug.LogError("CharacterController를 찾을 수 없습니다! 로컬 플레이어 프리팹 확인 필요.");
                if (chatHandler == null) Debug.LogError("ChatHandler가 LOPNetworkManager에 연결되지 않았습니다! 인스펙터 확인 필요.");
            }
        }

        var sync = newPlayer.GetComponent<NetworkSyncPlayer>();
        if (sync != null) sync.SetPlayerId(id);

        players[id] = newPlayer;
        Debug.Log($"[LOPNetworkManager] Player spawned: {id} at {spawnPoint.position}");
    }

    private Transform GetSpawnPoint(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[index % spawnPoints.Length];

        Debug.LogWarning("[LOPNetworkManager] SpawnPoints not set, using Vector3.zero");
        GameObject temp = new GameObject("TempSpawnPoint");
        temp.transform.position = Vector3.zero;
        return temp.transform;
    }

    // -------------------- ChatMessage --------------------

    private void HandleChatMessage(GameMessage msg)
    {
        string content = msg.ChatMessage.Message;
        Debug.Log($"[LOPNetworkManager] 챗 도착");
        // 여기에 채팅 UI 업데이트 코드 추가 가능

        chatHandler.SyncChatMessage(content);
    }

    // -------------------- InstantiateMessage --------------------

    private void HandleInstantiateMessage(GameSync.NetworkInstantiate inst)
    {
        if (networkObjects.ContainsKey(inst.NetworkId)) return;

        // 1. 프리팹 식별 (Register를 통해 가져옴)
        if (!prefabRegister.TryGetPrefab(inst.PrefabHash, out var prefab))
        {
            Debug.LogError($"Prefab not found for hash: {inst.PrefabHash}");
            return;
        }

        // 2. 위치 및 회전 설정
        Vector3 position = new Vector3(inst.Position.X, inst.Position.Y, inst.Position.Z);
        Quaternion rotation = Quaternion.Euler(inst.Rotation.X, inst.Rotation.Y, inst.Rotation.Z);

        // 3. 오브젝트 생성 및 등록
        SpawnNetworkObject(inst, prefab, position, rotation);

        Debug.Log($"[Client] Spawning Network Object: ID={inst.NetworkId}, Owner={inst.OwnerPlayerId}");
    }

    private void SpawnNetworkObject(GameSync.NetworkInstantiate inst, GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject newObject = Instantiate(prefab, position, rotation);
        newObject.name = $"{prefab.name}_NetID_{inst.NetworkId}";

        var netId = newObject.GetComponent<NetworkIdentity>();
        bool isOwner = inst.OwnerPlayerId == playerId;

        if (netId != null)
        {
            netId.SetIdentity(inst.NetworkId, isOwner);

            // 새로 생성된 오브젝트의 모든 RPC 컴포넌트 등록
            foreach (var component in newObject.GetComponents<Component>())
            {
                rpcSystem.RegisterRpcComponent(component);
            }
        }

        if (!string.IsNullOrEmpty(inst.DropItemId))
        {
            if (newObject.TryGetComponent<DropItemManage>(out var dropItemManage))
            {
                ItemDatabase itemData = ItemGenerator.Instance.GetItemData(inst.DropItemId);

                if (itemData != null)
                {
                    InventoryItem itemToSet = new InventoryItem(
                        itemData,
                        inst.DropItemCount
                    );

                    dropItemManage.item = itemToSet;
                    dropItemManage.DropItemUIInit();
                }
                else
                {
                    Debug.LogError($"[Network] Failed to find ItemData for ID: {inst.DropItemId}");
                }
            }
        }
        networkObjects[inst.NetworkId] = newObject;
    }


    // -------------------- NetworkObjectList --------------------

    private void HandleNetworkObjectList(GameSync.NetworkObjectList list)
    {
        Debug.Log($"[Client] Received existing object list with {list.Objects.Count} objects.");

        foreach (var inst in list.Objects)
        {
            // NetworkInstantiate 메시지 처리 함수를 재사용합니다.
            HandleInstantiateMessage(inst);
        }
    }

    // -------------------- MapBlockUpdate --------------------
    private void HandleMapBlockUpdate(GameMessage msg)
    {
        var update = msg.MapBlockUpdate;
        if (update == null || MapSettingManager.Instance == null) return;

        Vector3 pos = new Vector3(update.X, update.Y, update.Z);
        var map = MapSettingManager.Instance.Map;

        var chunk = map.GetChunkFromPosition(pos, Island.ChunkType.Ground);
        if (chunk == null) return;

        int localX = Mathf.FloorToInt(pos.x) - Mathf.FloorToInt(chunk.Position.x);
        int localY = Mathf.FloorToInt(pos.y);
        int localZ = Mathf.FloorToInt(pos.z) - Mathf.FloorToInt(chunk.Position.z);

        BlockData newBlock = map.FindBlockType(update.NewBlockId);
        if (newBlock == null) return;
        newBlock.id = update.NewBlockId;

        chunk.chunkData.chunkBlocks[localX, localY, localZ] =
        MapSettingManager.Instance.Map.FindBlockType(update.NewBlockId);


        if (update.NewLevel != -1)
        {
            chunk.chunkData.chunkBlocks[localX, localY, localZ] = newBlock;
            chunk.chunkData.chunkBlocks[localX, localY, localZ].level = update.NewLevel;
        }

        Debug.Log($"[MapSync] Block at {pos} updated to {update.NewBlockId}. Local data synchronized.");

        chunk.UpdateChunk();
        map.UpdateChunk(pos);
    }


    // -------------------- ObjectDestroy --------------------

    private void HandleObjectDestroy(ObjectDestroyMessage destroyMsg)
    {
        int networkId = destroyMsg.NetworkId;

        if (networkObjects.TryGetValue(networkId, out var obj))
        {
            if (obj == null)
            {
                Debug.LogWarning($"[Client Warning] ID={networkId}는 dict에 있지만 이미 유니티에서 파괴되었거나 유효하지 않습니다. 제거합니다.");
            }
            else
            {
                Destroy(obj);
                Debug.Log($"[Client] Successfully requested Destroy for Network Object: ID={networkId}, Name={obj.name}");
            }

            networkObjects.Remove(networkId);
        }
        else
        {
            Debug.LogWarning($"[Client] Received destroy message for an unknown object: ID={networkId}");
        }
    }

    private void HandleBlockDestroyFlag(BlockDestroyFlag destroyFlag)
    {
        // 수신된 int32 좌표를 Vector3로 변환하여 맵 함수에 전달
        var pos = new Vector3(destroyFlag.X, destroyFlag.Y, destroyFlag.Z);

        if (MapSettingManager.Instance != null && MapSettingManager.Instance.Map != null)
        {
            var mapInstance = MapSettingManager.Instance.Map;

            try
            {
                // GetBlockInChunk는 Vector3를 받습니다.
                var blockData = mapInstance.GetBlockInChunk(pos, ChunkType.Ground);

                if (blockData != null)
                {
                    blockData.isDestroy = destroyFlag.IsDestroyed;
                    mapInstance.UpdateChunk(pos);
                    Debug.Log($"[Client] Block {(destroyFlag.IsDestroyed ? "DESTROYED" : "RESTORED")} at {pos}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Client Error] Failed to handle BlockDestroyFlag at {pos}: {ex.Message}");
            }
        }
    }

    // -------------------- Send --------------------
    private void SendRaw(GameMessage msg)
    {
        try
        {
            if (udp != null)
            {
                byte[] data = msg.ToByteArray();
                udp.Send(data, data.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LOPNetworkManager] 메시지 전송 오류: {e.Message}");
        }
    }

    public void SendLeave()
    {
        // playerId가 없거나 이미 연결이 끊긴 상태면 전송하지 않습니다.
        if (string.IsNullOrEmpty(playerId) || !isConnected) return;

        var msg = new GameMessage
        {
            PlayerId = playerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Leave = new GameSync.Leave()
        };
        SendRaw(msg);
        Debug.Log("[LOPNetworkManager] Sending Leave message.");
    }

    public void SendPosition(Vector3 pos, float rotY)
    {
        lastSentSequence++;
        var msg = new GameMessage
        {
            PlayerId = playerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Position = new PositionSync
            {
                Sequence = lastSentSequence,
                X = pos.x,
                Y = pos.y,
                Z = pos.z,
                RotY = rotY
            }
        };
        SendRaw(msg);
    }

    public void SendAnimationState(int paramHash, bool value)
    {
        //Debug.Log($"[LOPNetworkManager] Sending AnimState: Hash={paramHash}, Value={value}");
        var msg = new GameMessage
        {
            PlayerId = playerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            AnimState = new GameSync.AnimationState()
        };
        msg.AnimState.States.Add(paramHash, value);
        SendRaw(msg);
    }

    public void SendAnimationTrigger(int paramHash)
    {
        //Debug.Log($"[LOPNetworkManager] Sending AnimTrigger: Hash={paramHash}");
        var msg = new GameMessage
        {
            PlayerId = playerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            AnimTrigger = new AnimationTrigger()
        };
        msg.AnimTrigger.Triggers.Add(paramHash);
        SendRaw(msg);
    }

    public void RPC(object component, string methodName, params object[] args)
    {
        var msg = rpcSystem.CreateRpcMessage(component, methodName, args);
        msg.PlayerId = playerId;
        SendRaw(msg);
    }

    public void NetworkInstantiate(GameObject prefab, Vector3 position, Quaternion rotation, string dropItemId = "", int dropItemCount = 1)
    {
        int prefabHash = prefabRegister.GetPrefabHash(prefab);

        var protoPos = new ProtoPos { X = position.x, Y = position.y, Z = position.z };
        var eulerAngles = rotation.eulerAngles;
        var protoRot = new ProtoRot { X = eulerAngles.x, Y = eulerAngles.y, Z = eulerAngles.z };

        var msg = new GameMessage
        {
            PlayerId = playerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            NetworkInstantiate = new GameSync.NetworkInstantiate
            {
                PrefabHash = prefabHash,
                Position = protoPos,
                Rotation = protoRot,
                DropItemId = dropItemId,
                DropItemCount = dropItemCount
            }
        };
        SendRaw(msg);
    }

    public void SendChatMessage(string content)
    {
        var msg = new GameMessage
        {
            PlayerId = playerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ChatMessage = new ChatMessage
            {
                Message = content
            }
        };
        SendRaw(msg);
    }

    public void SendBlockUpdate(Vector3Int position, string newBlockId, int level = -1)
    {
        var msg = new GameMessage
        {
            PlayerId = playerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            MapBlockUpdate = new MapBlockUpdate
            {
                X = position.x,
                Y = position.y,
                Z = position.z,
                NewBlockId = newBlockId,
                NewLevel = level
            }
        };
        SendRaw(msg);
    }

    public void SendObjectDestroy(int networkId)
    {
        var msg = new GameMessage
        {
            PlayerId = playerId, // 로컬 플레이어 ID를 포함
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ObjectDestroyMessage = new ObjectDestroyMessage
            {
                NetworkId = networkId
            }
        };
        SendRaw(msg);
    }

    public void NetworkDestroy(GameObject obj)
    {
        if (obj.TryGetComponent<NetworkIdentity>(out var identity))
        {
            SendObjectDestroy(identity.NetworkId);
        }
        else
        {
            Destroy(obj); // 네트워크 객체가 아니면 일반 파괴
        }
    }

    public void SendBlockDestroyFlag(Vector3Int position, bool isDestroyed)
    {
        var msg = new GameMessage
        {
            PlayerId = playerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            BlockDestroyFlag = new BlockDestroyFlag
            {
                // Vector3Int의 정수 값을 직접 할당
                X = position.x,
                Y = position.y,
                Z = position.z,
                IsDestroyed = isDestroyed
            }
        };
        SendRaw(msg);
    }
}
