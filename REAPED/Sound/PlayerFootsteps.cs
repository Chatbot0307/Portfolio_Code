using Member.LCH._01.Scripts.Player;
using Member.LCH._01.Scripts.Tracker;
using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Rigidbody rb;

    [SerializeField] private NavMovement trackerMovement;

    [Header("추가 설정")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float walkStepDistance = 2.2f;
    [SerializeField] private float volume = 0.4f;

    [Header("크리처 설정")]
    [SerializeField] private bool isCreature = false;
    [SerializeField] private float maxSoundDistance = 20f; // 플레이어와 이 거리 이상이면 소리 안 남

    private Vector3 _lastPosition;
    private float _distanceAccumulated;

    private Transform _playerTransform;

    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (trackerMovement == null) trackerMovement = GetComponent<NavMovement>();

        if (isCreature)
        {
            // 플레이어 찾기
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                _playerTransform = playerObj.transform;
        }
    }

    private void FixedUpdate()
    {
        Vector3 currentPos = transform.position;
        Vector3 distanceMoved = currentPos - _lastPosition;
        distanceMoved.y = 0;

        float moveMagnitude = distanceMoved.magnitude;

        if (moveMagnitude > 0.001f)
        {
            _distanceAccumulated += moveMagnitude;

            if (_distanceAccumulated >= walkStepDistance)
            {
                PlayFootstep();
                _distanceAccumulated = 0;
            }
        }

        _lastPosition = currentPos;
    }

    private void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        if (isCreature && _playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            if (distanceToPlayer > maxSoundDistance)
                return; 
        }

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        SoundManager.Instance.Play3DSound(clip, transform.position, volume);
    }
}
