using UnityEngine;

public class CameraHeadBob : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform playerRoot; // 최상위 'Player' 오브젝트 할당

    [Header("흔들림 강도")]
    [SerializeField] private float bobFrequency = 12.0f; // 흔들림 속도
    [SerializeField] private float bobHorizontalAmplitude = 0.04f; // 좌우 폭
    [SerializeField] private float bobVerticalAmplitude = 0.06f;   // 위아래 폭

    [Header("복귀 설정")]
    [SerializeField] private float smoothReturnSpeed = 5.0f;

    private float _timer;
    private Vector3 _initialLocalPos;
    private Vector3 _lastPosition;

    private void Start()
    {
        _initialLocalPos = transform.localPosition;
        if (playerRoot != null) _lastPosition = playerRoot.position;
    }

    private void LateUpdate()
    {
        if (playerRoot == null) return;

        // 1. 실제 수평 이동 거리 계산
        Vector3 currentPos = playerRoot.position;
        Vector3 horizontalDelta = new Vector3(currentPos.x, 0, currentPos.z) - new Vector3(_lastPosition.x, 0, _lastPosition.z);
        float moveMagnitude = horizontalDelta.magnitude;

        // 2. 이동 중일 때만 타이머 진행 (0.001f는 미세한 떨림 방지)
        if (moveMagnitude > 0.001f)
        {
            _timer += Time.deltaTime * bobFrequency;

            // 사인 함수를 이용한 좌표 계산
            float posX = Mathf.Cos(_timer / 2) * bobHorizontalAmplitude;
            float posY = Mathf.Sin(_timer) * bobVerticalAmplitude;

            transform.localPosition = _initialLocalPos + new Vector3(posX, posY, 0);
        }
        else
        {
            // 3. 멈췄을 때는 부드럽게 원래 위치로 복귀
            _timer = 0;
            transform.localPosition = Vector3.Lerp(transform.localPosition, _initialLocalPos, Time.deltaTime * smoothReturnSpeed);
        }

        _lastPosition = currentPos;
    }
}