using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject segmentPrefab;    // 줄을 구성하는 세그먼트 프리팹
    [SerializeField] private Transform player1;           // 줄 시작 지점 (플레이어1)
    [SerializeField] private Transform player2;           // 줄 끝 지점 (플레이어2)
    [SerializeField] private int segmentCount = 20;        // 기본 생성할 줄 마디 수
    [SerializeField] private float segmentSpacing = 0.2f; // 줄 마디 간 간격 (배치 위치 계산용)
    [SerializeField] private float tensionThreshold = 0.98f; // 98% 이상 팽팽하면 긴장 상태
    [SerializeField] private float maxTensionTime = 3f;    // 최대 유지 가능 시간

    private float tensionTimer = 0f;

    private List<GameObject> segments = new List<GameObject>(); // 생성된 세그먼트 리스트 저장
    private bool isBroken = false;

    void Start()
    {
        // 시작 시 줄 자동 생성
        GenerateRope();
    }

    void Update()
    {
        // 테스트용: K 키를 누르면 줄이 1개 추가됨
        if (Input.GetKeyDown(KeyCode.K))
        {
            AddSegment();
        }

        CheckRopeDistance();
    }

    // 줄 전체 생성 함수
    public void GenerateRope()
    {
        ClearRope(); // 기존 줄 삭제

        Rigidbody2D prevBody = player1.GetComponent<Rigidbody2D>(); // 첫 연결은 player1
        Vector2 startPos = player1.position;
        Vector2 endPos = player2.position;

        Vector2 dir = (endPos - startPos).normalized;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector2 spawnPos = startPos + dir * segmentSpacing * (i + 1);
            GameObject segment = Instantiate(segmentPrefab, spawnPos, Quaternion.identity, this.transform);
            segments.Add(segment);

            Rigidbody2D rb = segment.GetComponent<Rigidbody2D>();
            HingeJoint2D joint = segment.GetComponent<HingeJoint2D>();
            joint.connectedBody = prevBody;

            prevBody = rb;
        }


        // 마지막 세그먼트 → player2에 연결
        HingeJoint2D endJoint = player2.gameObject.AddComponent<HingeJoint2D>();
        endJoint.connectedBody = prevBody;
        endJoint.autoConfigureConnectedAnchor = false;
        endJoint.anchor = Vector2.zero;
        endJoint.connectedAnchor = Vector2.zero;
    }

    // 기존 체인 제거 함수
    public void ClearRope()
    {
        // 기존 세그먼트 모두 제거
        foreach (GameObject seg in segments)
        {
            Destroy(seg);
        }
        segments.Clear();

        // player2에 달려 있던 기존 HingeJoint 제거
        var oldJoint = player2.GetComponent<HingeJoint2D>();
        if (oldJoint != null)
            Destroy(oldJoint);
    }

    // 세그먼트 1개 추가 함수
    public void AddSegment()
    {
        if (segments.Count == 0)
            return;

        GameObject lastSegment = segments[segments.Count - 1];
        Rigidbody2D lastBody = lastSegment.GetComponent<Rigidbody2D>();

        // 새로운 세그먼트를 마지막 세그먼트의 연장선에 배치
        Vector2 spawnPos = lastSegment.transform.position -
            (Vector3)(player2.position - lastSegment.transform.position).normalized * segmentSpacing;

        GameObject newSegment = Instantiate(segmentPrefab, spawnPos, Quaternion.identity, this.transform);
        segments.Add(newSegment);

        // 새로운 세그먼트를 기존 마지막 세그먼트에 연결
        Rigidbody2D rb = newSegment.GetComponent<Rigidbody2D>();
        HingeJoint2D joint = newSegment.GetComponent<HingeJoint2D>();
        joint.connectedBody = lastBody;

        // player2의 기존 연결 제거
        var oldJoint = player2.GetComponent<HingeJoint2D>();
        if (oldJoint != null) Destroy(oldJoint);

        // player2 → 새 세그먼트로 연결
        HingeJoint2D newEndJoint = player2.gameObject.AddComponent<HingeJoint2D>();
        newEndJoint.connectedBody = rb;
        newEndJoint.autoConfigureConnectedAnchor = false;
        newEndJoint.anchor = Vector2.zero;
        newEndJoint.connectedAnchor = Vector2.zero;
    }

    private void CheckRopeDistance()
    {
        float currentDistance = Vector2.Distance(player1.position, player2.position);
        float maxDistance = segmentSpacing * segments.Count;

        // 팽팽한 상태 유지 시간 측정
        if (!isBroken && currentDistance >= maxDistance)
        {
            tensionTimer += Time.deltaTime;

            if (tensionTimer >= maxTensionTime)
            {
                BreakChain();
            }
        }
        else
        {
            tensionTimer = 0f; // 줄이 느슨해졌으면 타이머 리셋
        }

        if (isBroken && Input.GetKeyDown(KeyCode.R))
        {
            RestoreChain();
        }
    }

    public void BreakChain()
    {
        isBroken = true;

        if (segments.Count == 0)
            return;

        int midIndex = segments.Count / 2;

        if (segments.Count % 2 == 0)
        {
            Destroy(segments[midIndex - 1]);
            Destroy(segments[midIndex]);

            segments.RemoveAt(midIndex);       // 뒤쪽 먼저 제거
            segments.RemoveAt(midIndex - 1);   // 앞쪽 제거
        }
        else
        {
            Destroy(segments[midIndex]);
            segments.RemoveAt(midIndex);
        }
       
      StartCoroutine(CameraShake.Instance.Shake(0.1f, 0.2f));
        Scene2.Instance.OpenMenu();
        Debug.Log("줄이 중앙에서 끊어졌습니다!");
    }

    public void RestoreChain()
    {
        if (!isBroken) return;

        isBroken = false;
        GenerateRope();
    }
}
