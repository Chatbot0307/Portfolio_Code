using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class CountdownManager : MonoBehaviour
{
    public TMP_Text countdownText;
    public GameObject countdownUI;
    public float interval = 1f;
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            StartCountdown(() => Debug.Log("Countdown Complete!"));
        }
    }
    public void StartCountdown(System.Action onComplete)
    {
        StartCoroutine(CountdownCoroutine(onComplete));
    }

    private IEnumerator CountdownCoroutine(System.Action onComplete)
    {
        Time.timeScale = 0f; 
        countdownUI.SetActive(true);

        string[] messages = { "3", "2", "1", "Ω√¿€!" };

        foreach (var msg in messages)
        {
            countdownText.text = msg;
            countdownText.transform.localScale = Vector3.zero;
            countdownText.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

            yield return new WaitForSecondsRealtime(interval);
        }

        countdownUI.SetActive(false);
        Time.timeScale = 1f;
        onComplete?.Invoke();
    }
}
