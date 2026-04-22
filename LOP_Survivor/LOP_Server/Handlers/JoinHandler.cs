using System;
using System.Net;
using GameSync;
using System.Linq; // Console.WriteLine에서 using이 필요할 수 있습니다.

/// <summary>
/// 새로운 클라이언트가 서버에 접속했을 때 처리.
/// 1) 새 ID 부여
/// 2) 본인에게 ID 전달
/// 3) 기존 클라에게 새 클라 알림
/// 4) 새 클라에게 기존 클라들 목록 전달
/// 5) 새 클라에게 기존 Network Objects 목록 전달 (추가됨)
/// </summary>
public class JoinHandler : IMessageHandler
{
    public void Handle(GameMessage msg, IPEndPoint sender, ServerContext ctx)
    {
        // 1. 새 플레이어 ID 할당 및 등록
        var newId = Guid.NewGuid().ToString();
        var clientNickname = msg.Join?.Nickname ?? "Player";
        ctx.Clients[newId] = sender;
        ctx.Nicknames[newId] = clientNickname;

        int newNetworkId = ctx.GetNextNetworkId(newId);

        bool isWorldSpawner = false;

        if (ctx.Clients.Count == 1)
        {
            ctx.FirstPlayerId = newId; //첫번째 플레이어로 등록
            isWorldSpawner = true;
            Console.WriteLine($"[Server] {newId} is assigned as the First Player/World Spawner.");

            ctx.TickManager.Start();
        }

        // 2. 자기 ID 알려주기 (Join 승인)
        var assign = new GameMessage
        {
            PlayerId = newId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Join = new Join()
            {
                IsWorldSpawner = isWorldSpawner,
                NetworkId = newNetworkId,
                Nickname = clientNickname
            }
        };
        ctx.Send(assign, sender);

        // 3. 기존 클라에게 새 클라 알림 (브로드캐스트)
        var broadcastJoin = new GameMessage
        {
            PlayerId = newId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Join = new Join()
            {
                NetworkId = newNetworkId,
                Nickname = clientNickname
            }
        };
        ctx.Broadcast(broadcastJoin, newId);

        // 4. 새 클라에게 기존 클라 목록 전달 (기존 로직 유지)
        foreach (var kv in ctx.Clients)
        {
            if (kv.Key == newId) continue;

            int existingNetworkId = -1;
            foreach (var netObj in ctx.NetworkObjects)
            {
                if (netObj.Value == kv.Key)
                {
                    existingNetworkId = netObj.Key;
                    break;
                }
            }

            if (existingNetworkId == -1)
            {
                Console.WriteLine($"[Warning] Cannot find NetworkId for existing player {kv.Key}");
                continue;
            }

            var existing = new GameMessage
            {
                PlayerId = kv.Key,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Join = new Join()
                {
                    NetworkId = existingNetworkId, // [!] networkId 필드 추가
                    Nickname = ctx.Nicknames.TryGetValue(kv.Key, out var nick) ? nick : "OldPlayer"
                }
            };
            ctx.Send(existing, sender);
        }

        // 5. 현재 존재하는 네트워크 오브젝트 목록을 생성하여 전송합니다.
        var objectList = ctx.GetCurrentNetworkObjects();
        if (objectList.Objects.Count > 0)
        {
            var listMsg = new GameMessage
            {
                PlayerId = "SERVER",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                NetworkObjectList = objectList
            };
            ctx.Send(listMsg, sender);
            Console.WriteLine($"[Server] Sent {objectList.Objects.Count} existing objects to new player {newId}.");
        }

        // 6. 맵 변경 사항 동기화
        if (ctx.ChangedBlocks.Count > 0)
        {
            Console.WriteLine($"Sending {ctx.ChangedBlocks.Count} block updates to new player {msg.PlayerId}.");

            foreach (var kvp in ctx.ChangedBlocks)
            {
                if (!ServerContext.TryParseBlockKey(kvp.Key, out int x, out int y, out int z)) continue;

                var updateMsg = new GameMessage
                {
                    PlayerId = "SERVER",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    MapBlockUpdate = new MapBlockUpdate
                    {
                        X = x,
                        Y = y,
                        Z = z,
                        NewBlockId = kvp.Value
                    }
                };

                ctx.Send(updateMsg, sender);
            }
        }

        Console.WriteLine($"Client joined: {newId} (NetworkID: {newNetworkId})");
    }
}
