using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 스톤 발사 컨트롤러
/// </summary>
public class StoneLauncher : NetworkBehaviour
{
    private Stone stone;
    private Rigidbody2D rb;
    private LineRenderer previewLine;

    [Header("발사 설정")]
    public float maxDrag = 3.0f;

    [Header("조준선 설정")]
    public float distanceMultiplier = 2f;
    public float maxTrajectoryLength = 5f;
    public LayerMask wallLayer;

    private Vector2 startPos;
    private bool isDragging = false;

    private Vector2 _queuedDirection;
    private float _queuedPower;
    private bool _isInputReady;

    public override void Spawned()
    {
        stone = GetComponent<Stone>();
        rb = GetComponent<Rigidbody2D>();
        previewLine = GetComponent<LineRenderer>();

        Debug.Log($"[StoneLauncher] Spawned / HasInputAuthority:{HasInputAuthority}");

        if (HasInputAuthority)
        {
            var auto = Runner != null ? Runner.GetComponent<AutoConnect>() : null;
            if (auto == null)
                auto = FindAnyObjectByType<AutoConnect>();

            if (auto != null)
            {
                auto.RegisterLocalStone(this);
                Debug.Log("[StoneLauncher] RegisterLocalStone 성공");
            }
            else
            {
                Debug.LogError("[StoneLauncher] AutoConnect 못 찾음");
            }

            // 경기 종료 UI 로컬 스톤 연결
            MatchEndPresenter presenter = FindAnyObjectByType<MatchEndPresenter>();
            if (presenter != null && stone != null)
            {
                presenter.SetLocalStone(stone);
                Debug.Log("[StoneLauncher] MatchEndPresenter 로컬 스톤 연결 성공");
            }
        }
    }

    // 프레임 입력 처리
    private void Update()
    {
        if (!HasInputAuthority)
            return;

        if (stone == null || rb == null)
            return;

        if (stone.data == null)
        {
            Debug.LogError("[StoneLauncher] stone.data null");
            return;
        }

        if (GameRuleManager.Instance == null)
            return;

        if (GameRuleManager.Instance.MatchState != MatchState.Playing)
        {
            StopDragging();
            return;
        }

        if (stone.IsKO || stone.IsRespawning)
        {
            StopDragging();
            return;
        }

        if (rb.linearVelocity.magnitude > stone.CurrentStopThreshold)
        {
            StopDragging();
            return;
        }

        HandleInput();
    }

    // 드래그 입력 처리
    private void HandleInput()
    {
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (Input.GetMouseButtonDown(0) && !overUI)
        {
            startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            isDragging = true;
        }

        if (isDragging && Input.GetMouseButton(0))
            DrawPreviewLine();

        if (isDragging && Input.GetMouseButtonUp(0))
        {
            Vector2 dragDir = startPos - (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 limitedDrag = Vector2.ClampMagnitude(dragDir, maxDrag);

            float finalPower = limitedDrag.magnitude * stone.data.launchSpeedMultiplier;

            _queuedDirection = limitedDrag.normalized;
            _queuedPower = finalPower;
            _isInputReady = true;

            StopDragging();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (stone == null)
            return;

        if (stone.IsKO || stone.IsRespawning)
            return;

        if (GetInput(out NetworkInputData data))
        {
            if (data.IsFiring)
            {
                rb.linearVelocity = Vector2.zero;
                rb.AddForce(data.Direction * data.Power, ForceMode2D.Impulse);

                if (HasStateAuthority)
                {
                    stone.RecordSelfLaunch();
                }
            }
        }
    }

    // 로컬 입력 반환
    public NetworkInputData GetLocalInput()
    {
        var data = new NetworkInputData();

        if (_isInputReady)
        {
            data.Direction = _queuedDirection;
            data.Power = _queuedPower;
            data.IsFiring = true;
            _isInputReady = false;
        }

        return data;
    }

    // 드래그 종료
    private void StopDragging()
    {
        isDragging = false;

        if (previewLine != null)
            previewLine.enabled = false;
    }

    // 조준선 그리기
    private void DrawPreviewLine()
    {
        Vector2 currentPos = GetMouseWorldPos();
        Vector2 dragDir = startPos - currentPos;
        Vector2 limitedDrag = Vector2.ClampMagnitude(dragDir, maxDrag);

        float calculatedDist = limitedDrag.magnitude * distanceMultiplier;
        float remainingDistance = Mathf.Min(calculatedDist, maxTrajectoryLength);

        if (remainingDistance < 0.2f)
        {
            previewLine.enabled = false;
            return;
        }

        previewLine.enabled = true;

        Vector2 origin = transform.position;
        Vector2 direction = limitedDrag.normalized;

        previewLine.positionCount = 1;
        previewLine.SetPosition(0, origin);

        for (int i = 1; i < 4; i++)
        {
            if (remainingDistance <= 0.01f)
                break;

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, remainingDistance, wallLayer);

            if (hit.collider != null)
            {
                previewLine.positionCount++;
                previewLine.SetPosition(i, hit.point);

                remainingDistance -= hit.distance;
                direction = Vector2.Reflect(direction, hit.normal);
                origin = hit.point + (direction * 0.01f);
            }
            else
            {
                previewLine.positionCount++;
                previewLine.SetPosition(i, origin + direction * remainingDistance);
                break;
            }
        }
    }

    // 마우스 월드좌표 반환
    private Vector2 GetMouseWorldPos()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
}