using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

/// <summary>
/// 스톤 네트워크 오브젝트
/// </summary>
public class Stone : NetworkBehaviour
{
    [Networked] public int CharacterIndex { get; set; }
    [Networked] public int teamID { get; private set; }
    [Networked] public NetworkBool IsReady { get; private set; }
    [Networked] public NetworkBool IsKO { get; private set; }
    [Networked] public NetworkBool IsRespawning { get; private set; }

    [Networked] public int LastHitByTeamId { get; private set; }
    [Networked] public PlayerRef LastHitByPlayerRef { get; private set; }
    [Networked] public int LastHitTick { get; private set; }
    [Networked] public NetworkBool WasLastHitBySelfLaunch { get; private set; }

    public StoneData data;
    public Rigidbody2D rb;

    [Header("Root References")]
    [SerializeField] private GameObject visualRoot;

    private Animator anim;
    private Collider2D[] cachedColliders;
    private NetworkRigidbody networkRigidbody;

    public float CurrentStopThreshold => data != null ? data.baseStopThreshold * (1 / data.handling) : 0.1f;

    /// <summary>
    /// 스폰 초기화
    /// </summary>
    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        networkRigidbody = GetComponent<NetworkRigidbody>();
        cachedColliders = GetComponentsInChildren<Collider2D>(true);

        LoadCharacterData(CharacterIndex);
        ApplyVisibleState();

        if (HasStateAuthority)
        {
            ResetLastHitInfo();
        }
    }

    /// <summary>
    /// 렌더 상태 반영
    /// </summary>
    public override void Render()
    {
        ApplyVisibleState();
    }

    /// <summary>
    /// 캐릭터 데이터 로드
    /// </summary>
    private void LoadCharacterData(int index)
    {
        data = Resources.Load<StoneData>($"Characters/StoneData_{index}");
        if (data != null)
            ApplyData(data);
        else
            Debug.LogError($"[Stone] StoneData 로드 실패 / Characters/StoneData_{index}");
    }

    /// <summary>
    /// 캐릭터 데이터 적용
    /// </summary>
    public void ApplyData(StoneData newData)
    {
        data = newData;

        if (rb != null)
        {
            rb.mass = data.weight;
            rb.linearDamping = data.handling * 1.5f;
            rb.angularDamping = data.handling;
        }

        if (data.animatorController != null && anim != null)
            anim.runtimeAnimatorController = data.animatorController;
    }

    /// <summary>
    /// 틱 단위 물리 처리
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (IsKO || IsRespawning)
        {
            ForceStop();
            return;
        }
    }

    /// <summary>
    /// 팀 설정
    /// </summary>
    public void SetTeam(int newTeamID)
    {
        if (!HasStateAuthority)
            return;

        teamID = newTeamID;
    }

    /// <summary>
    /// 준비 상태 설정
    /// </summary>
    public void SetReady(bool ready)
    {
        if (!HasStateAuthority)
            return;

        IsReady = ready;
    }

    /// <summary>
    /// 마지막 판정자 기록
    /// </summary>
    public void RecordLastHit(int attackerTeamId, PlayerRef attackerPlayerRef, bool isSelfLaunch)
    {
        if (!HasStateAuthority)
            return;

        LastHitByTeamId = attackerTeamId;
        LastHitByPlayerRef = attackerPlayerRef;
        LastHitTick = Runner.Tick;
        WasLastHitBySelfLaunch = isSelfLaunch;
    }

    /// <summary>
    /// 자기 발사 기록
    /// </summary>
    public void RecordSelfLaunch()
    {
        if (!HasStateAuthority)
            return;

        RecordLastHit(teamID, Object.InputAuthority, true);
    }

    /// <summary>
    /// 마지막 판정자 초기화
    /// </summary>
    public void ResetLastHitInfo()
    {
        if (!HasStateAuthority)
            return;

        LastHitByTeamId = -1;
        LastHitByPlayerRef = PlayerRef.None;
        LastHitTick = 0;
        WasLastHitBySelfLaunch = false;
    }

    /// <summary>
    /// 최근 유효 타격 여부
    /// </summary>
    public bool HasValidRecentHit(int validTickWindow)
    {
        if (Runner == null)
            return false;

        if (LastHitByTeamId < 0)
            return false;

        int tickDelta = Runner.Tick - LastHitTick;
        return tickDelta >= 0 && tickDelta <= validTickWindow;
    }

    /// <summary>
    /// 최근 자기 발사 여부
    /// </summary>
    public bool IsRecentSelfLaunch(int validTickWindow)
    {
        if (!HasValidRecentHit(validTickWindow))
            return false;

        return WasLastHitBySelfLaunch && LastHitByTeamId == teamID;
    }

    /// <summary>
    /// KO 진입
    /// </summary>
    public void EnterKO()
    {
        if (!HasStateAuthority)
            return;

        if (IsKO)
            return;

        IsKO = true;
        IsRespawning = false;

        ForceStop();
        SetCollisionState(false);
    }

    /// <summary>
    /// 리스폰 대기 시작
    /// </summary>
    public void BeginRespawnState()
    {
        if (!HasStateAuthority)
            return;

        IsRespawning = true;
        IsKO = false;

        ForceStop();
        SetCollisionState(false);
    }

    /// <summary>
    /// 리스폰 완료
    /// </summary>
    public void FinishRespawn(Vector3 position)
    {
        if (!HasStateAuthority)
            return;

        if (networkRigidbody != null)
        {
            networkRigidbody.Teleport(position, Quaternion.identity);
        }
        else
        {
            transform.position = position;
            transform.rotation = Quaternion.identity;
        }

        IsKO = false;
        IsRespawning = false;

        ForceStop();
        ResetLastHitInfo();
        SetCollisionState(true);
    }

    /// <summary>
    /// 강제 정지
    /// </summary>
    public void ForceStop()
    {
        if (rb == null)
            return;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    /// <summary>
    /// 충돌 상태 적용
    /// </summary>
    private void SetCollisionState(bool enabledState)
    {
        if (cachedColliders == null)
            return;

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            if (cachedColliders[i] != null)
                cachedColliders[i].enabled = enabledState;
        }
    }

    /// <summary>
    /// 표시 상태 적용
    /// </summary>
    private void ApplyVisibleState()
    {
        if (visualRoot == null)
            return;

        bool visible = !IsKO && !IsRespawning;
        if (visualRoot.activeSelf != visible)
            visualRoot.SetActive(visible);
    }

    /// <summary>
    /// 충돌 처리
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasStateAuthority)
            return;

        if (IsKO || IsRespawning)
            return;

        if (!collision.gameObject.CompareTag("Stone"))
            return;

        if (rb.linearVelocity.magnitude <= 0.5f)
            return;

        Stone enemyStone = collision.gameObject.GetComponent<Stone>();
        if (enemyStone == null)
            return;

        if (enemyStone.IsKO || enemyStone.IsRespawning)
            return;

        Vector2 dir = (collision.transform.position - transform.position).normalized;
        float forceMagnitude = (rb.linearVelocity.magnitude - 1.0f) * data.impactPower;
        Vector2 finalForce = dir * forceMagnitude;

        enemyStone.rb.AddForce(finalForce, ForceMode2D.Impulse);
        enemyStone.RecordLastHit(teamID, Object.InputAuthority, false);
    }

    /// <summary>
    /// 충돌 힘 요청
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestImpact(Vector2 force)
    {
        if (rb != null)
            rb.AddForce(force, ForceMode2D.Impulse);
    }
}