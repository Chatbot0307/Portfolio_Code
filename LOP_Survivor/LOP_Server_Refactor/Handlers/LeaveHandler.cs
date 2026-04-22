using System;
using System.Linq;
using System.Net;
using GameSync;

/// <summary>
/// 클라이언트가 나갔을 때 처리.
/// 스포너가 나갔을 때, 역할 위임 및 객체 소유권 이전
/// </summary>
public class LeaveHandler : IMessageHandler
{
    public void Handle(GameMessage msg, IPEndPoint sender, ServerContext ctx)
    {
        var id = msg.PlayerId;

        // 클라이언트 목록에서 제거 시도
        bool removedClient = ctx.Clients.TryRemove(id, out _);

        // 성공적으로 제거되었다면
        if (removedClient)
        {
            Console.WriteLine($"Client left: {id}");

            // 다른 클라이언트들에게 떠났음을 알림 (기존 로직)
            var leaveMsg = new GameMessage
            {
                PlayerId = id,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Leave = new Leave()
            };
            ctx.Broadcast(leaveMsg, id); // 떠난 자신에게는 보내지 않음

            // 스포너가 나갔는지 여부를 먼저 확인
            bool spawnerLeft = (id == ctx.FirstPlayerId);
            string newSpawnerId = null;
            IPEndPoint newSpawnerEp = null;

            // 떠난 클라이언트가 World Spawner였는지 확인
            if (spawnerLeft && !ctx.Clients.IsEmpty)
            {
                // 새 스포너 찾기 (ConcurrentDictionary는 순서를 보장하지 않으므로 First()는 임의의 유저)
                var newSpawner = ctx.Clients.First();
                newSpawnerId = newSpawner.Key;
                newSpawnerEp = newSpawner.Value;

                ctx.FirstPlayerId = newSpawnerId; // 서버 컨텍스트의 스포너 ID 갱신

                Console.WriteLine($"[Server] World Spawner left. Re-assigned role to {newSpawnerId}.");

                // 새로운 World Spawner 할당 메시지 전송
                var assignMsg = new GameMessage
                {
                    PlayerId = "SERVER",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    AssignWorldSpawner = new AssignWorldSpawner()
                };

                ctx.Send(assignMsg, newSpawnerEp);
            }

            // 떠난 플레이어가 소유한 객체 목록 가져오기
            var ownedNetworkIds = ctx.NetworkObjects
                                     .Where(kvp => kvp.Value == id)
                                     .Select(kvp => kvp.Key)
                                     .ToList();

            // 스포너가 나갔고, 새 스포너가 있다면 -> 소유권 이전
            if (spawnerLeft && newSpawnerId != null)
            {
                Console.WriteLine($"[Server] Transferring {ownedNetworkIds.Count} objects to new spawner ({newSpawnerId}).");

                foreach (var networkId in ownedNetworkIds)
                {
                    // 1. 서버 상태 업데이트: 객체 소유자를 새 스포너로 변경
                    ctx.NetworkObjects[networkId] = newSpawnerId;
                    if (ctx.NetworkObjectDetails.TryGetValue(networkId, out var details))
                    {
                        details.OwnerPlayerId = newSpawnerId;
                    }

                    // 2. 모든 클라이언트에게 소유권 이전을 브로드캐스트
                    var transferMsg = new GameMessage
                    {
                        PlayerId = "SERVER",
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        TransferOwnership = new TransferOwnership
                        {
                            NetworkId = networkId,
                            NewOwnerPlayerId = newSpawnerId
                        }
                    };
                    ctx.Broadcast(transferMsg);
                }
            }

            // 스포너가 나갔는데 남은 사람이 없어 새 스포너가 없는 경우 -> 객체 파괴
            else if (ownedNetworkIds.Count > 0)
            {
                Console.WriteLine($"[Server] Player left. Destroying {ownedNetworkIds.Count} orphaned objects.");
                foreach (var networkId in ownedNetworkIds)
                {
                    // 서버 상태에서 객체 정보 제거
                    ctx.NetworkObjects.TryRemove(networkId, out _);
                    ctx.NetworkObjectDetails.TryRemove(networkId, out _);

                    // 모든 클라이언트에게 이 객체를 파괴하라고 알림
                    var destroyMsg = new GameMessage
                    {
                        PlayerId = "SERVER",
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        ObjectDestroyMessage = new ObjectDestroyMessage { NetworkId = networkId }
                    };
                    ctx.Broadcast(destroyMsg);
                }
            }

            // LastSeq 정리 (소유권 이전과 별개로 항상 실행)
            foreach (var networkId in ownedNetworkIds)
            {
                ctx.LastSeq.TryRemove(networkId, out _);
            }
            Console.WriteLine($"Cleaned up {ownedNetworkIds.Count} LastSeq entries for leaving player {id}.");
        }
        else
        {
            // 이미 제거되었거나 존재하지 않는 클라이언트 ID일 경우
            Console.WriteLine($"[Warning] Attempted to remove non-existent client: {id}");
        }
    }
}