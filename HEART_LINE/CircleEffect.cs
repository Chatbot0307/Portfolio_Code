using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class CircleEffect : Singleton<CircleEffect>
{

    public GameObject circle;


    private Coroutine routine;

    public void Effect(float start, float end, float time, Action OnEnd = default)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(Cor());

        IEnumerator Cor()
        {
            float re = 1f / time;
            for (float t = 0; t < 1; t += Time.fixedDeltaTime * re)
            {
                circle.transform.localScale = Vector3.one * Mathf.Lerp(start, end, t);
                yield return new WaitForSeconds(1f);
            }

            circle.transform.localScale = Vector3.one * end;
            OnEnd?.Invoke();
        }
    }

    public void LoadScene(string name)
    {
        Effect(1, 0, 0.3f, () =>
        {
            SceneManager.LoadScene("KJH");
            Effect(0, 1, 0.3f);
        });
    }
}