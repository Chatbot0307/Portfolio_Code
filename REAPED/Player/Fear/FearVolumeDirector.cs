using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class FearVolumeDirector : MonoBehaviour
{
    [Header("Volume Settings")]
    [SerializeField] private Volume globalVolume;

    private Vignette _vignette;

    private void Start()
    {
        if (globalVolume == null) globalVolume = GetComponent<Volume>();

        VolumeProfile profile = globalVolume.profile;
        if (profile.TryGet(out _vignette)) _vignette.active = true;
    }

    private void Update()
    {
        if (FearManager.Instance == null) return;

        float bpm = FearManager.Instance.currentBPM;

        // 1. 비네트: 60~200 BPM 전체 구간에서 서서히 진해짐 (0.2 -> 0.5)
        float vignetteRatio = (bpm - 60f) / 140f;
        if (_vignette != null)
        {
            _vignette.intensity.value = Mathf.Lerp(0.3f, 0.55f, vignetteRatio);
        }
    }
}