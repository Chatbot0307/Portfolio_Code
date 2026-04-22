using Member.LCH._01.Scripts.Event;
using Member.LCH._01.Scripts.Horror;
using UnityEngine;

public class SimpleSoundHorror : DirectingHorror
{
    [Header("사운드 설정")]
    [SerializeField] private AudioClip scareSound;
    [Range(0f, 1f)][SerializeField] private float volume = 1.0f;
    [SerializeField] private bool playAtPlayerPos = false;

    protected override void HandleHorror(HorrorTriggerEvent evt)
    {
        FearManager.Instance.AddBPMGradual(horrorDataSO.fearAmount, horrorDataSO.fearDuration);

        Vector3 playPos = playAtPlayerPos ? Camera.main.transform.position : transform.position;

        if (SoundManager.Instance != null && scareSound != null)
        {
            SoundManager.Instance.Play3DSound(scareSound, playPos, volume);
        }
    }
}
