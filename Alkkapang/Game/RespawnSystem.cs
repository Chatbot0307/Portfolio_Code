using Fusion;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 리스폰 시스템
/// </summary>
public class RespawnSystem : NetworkBehaviour
{
    public struct RespawnEntry : INetworkStruct
    {
        public NetworkId StoneId;
        public TickTimer Timer;
        public Vector3 Position;
    }

    private readonly List<RespawnEntry> pendingRespawns = new();

    /// <summary>
    /// 리스폰 예약
    /// </summary>
    public void EnqueueRespawn(Stone stone, float delay, Vector3 position)
    {
        if (!HasStateAuthority)
            return;

        if (stone == null)
            return;

        stone.BeginRespawnState();

        RespawnEntry entry = new RespawnEntry
        {
            StoneId = stone.Object.Id,
            Timer = TickTimer.CreateFromSeconds(Runner, delay),
            Position = position
        };

        pendingRespawns.Add(entry);

        Debug.Log($"[RespawnSystem] 예약 / team:{stone.teamID} / delay:{delay}");
    }

    /// <summary>
    /// 리스폰 큐 처리
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        for (int i = pendingRespawns.Count - 1; i >= 0; i--)
        {
            RespawnEntry entry = pendingRespawns[i];

            if (!entry.Timer.Expired(Runner))
                continue;

            if (Runner.TryFindObject(entry.StoneId, out NetworkObject obj))
            {
                Stone stone = obj.GetComponent<Stone>();
                if (stone != null)
                {
                    stone.FinishRespawn(entry.Position);
                    Debug.Log($"[RespawnSystem] 실행 / team:{stone.teamID}");
                }
            }

            pendingRespawns.RemoveAt(i);
        }
    }
}