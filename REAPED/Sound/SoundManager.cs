using UnityEngine.Pool;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("심장 박동 (2D)")]
    [SerializeField] private AudioSource heartbeatSource;
    [SerializeField] private AudioClip heartbeatClip;
    private float _nextBeatTime;

    [Header("3D 효과음 풀링")]
    [SerializeField] private AudioSource sfxPrefab; // 3D 설정이 완료된 AudioSource 프리팹
    private IObjectPool<AudioSource> _sfxPool;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 오브젝트 풀 초기화
        _sfxPool = new ObjectPool<AudioSource>(
            CreateSound, OnGet, OnRelease, OnDestroyPoolObject,
            collectionCheck: true, defaultCapacity: 10, maxSize: 20
        );
    }

    private void Update()
    {
        HandleHeartbeat();
    }

    #region Heartbeat System (BPM 동기화)

    private void HandleHeartbeat()
    {
        if (FearManager.Instance == null || heartbeatClip == null) return;

        float bpm = FearManager.Instance.currentBPM;

        // 65 BPM 이하 안정 상태에선 소리를 끔
        if (bpm <= 65f) return;

        // BPM 기반 박동 간격 계산 (예: 120 BPM = 0.5초마다)
        float beatInterval = 60f / bpm;

        if (Time.time >= _nextBeatTime)
        {
            // 60~200 범위를 활용해 볼륨조절
            float intensity = (bpm - 60f) / 140f;
            heartbeatSource.volume = Mathf.Lerp(0.1f, 0.8f, intensity);

            heartbeatSource.PlayOneShot(heartbeatClip);
            _nextBeatTime = Time.time + beatInterval;
        }
    }

    #endregion

    #region 3D SFX Pooling (공간 음향)

    public void Play3DSound(AudioClip clip, Vector3 position, float volume = 1.0f)
    {
        if (clip == null) return;

        var source = _sfxPool.Get();
        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.Play();

        // 재생 완료 후 풀로 반환
        StartCoroutine(ReturnToPoolAfterPlay(source, clip.length));
    }

    private System.Collections.IEnumerator ReturnToPoolAfterPlay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        _sfxPool.Release(source);
    }

    public void Play3DSoundFollow(AudioClip clip, Transform target, float volume = 1.0f, float pitch = 1.0f)
    {
        if (clip == null || target == null) return;

        var source = _sfxPool.Get();

        // 소리 소스를 물체의 자식으로 넣어 물리적으로 따라다니게 함
        source.transform.SetParent(target);
        source.transform.localPosition = Vector3.zero; // 물체 중심에서 소리 발생

        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();

        StartCoroutine(ReturnToPoolAfterFollow(source, clip.length));
    }

    private System.Collections.IEnumerator ReturnToPoolAfterFollow(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);

        source.transform.SetParent(transform);
        source.pitch = 1.0f;
        _sfxPool.Release(source);
    }

    public void Play3DSoundLoop(AudioClip clip, Vector3 position, float duration, float volume = 1.0f)
    {
        if (clip == null) return;

        var source = _sfxPool.Get();
        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.loop = true; // 루프 활성화
        source.Play();

        StartCoroutine(ReturnToPoolAfterDuration(source, duration));
    }

    private System.Collections.IEnumerator ReturnToPoolAfterDuration(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);

        source.Stop();
        source.loop = false; // 루프 초기화
        _sfxPool.Release(source);
    }

    // 풀 관리용 내부 메서드
    private AudioSource CreateSound() => Instantiate(sfxPrefab, transform);
    private void OnGet(AudioSource source) => source.gameObject.SetActive(true);
    private void OnRelease(AudioSource source) => source.gameObject.SetActive(false);
    private void OnDestroyPoolObject(AudioSource source) => Destroy(source.gameObject);

    #endregion
}