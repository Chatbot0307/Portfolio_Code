using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exp : MonoBehaviour
{
    private Transform target;
    [SerializeField] private int expAmount;
    [SerializeField] private AudioSource sound;

    public void SetUp(int _expAmount, Transform _target)
    {
        expAmount = _expAmount;
        target = _target;

        StartCoroutine(AutoAbsorb());
    }

    private IEnumerator AutoAbsorb()
    {
        float duration = 0.7f;
        float timer = 0f;
        Vector3 start = transform.position;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target.position, timer / duration);
            yield return null;
        }

        LevelSystem.Instance.AddExp(expAmount);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        sound.Play();
    }
}
