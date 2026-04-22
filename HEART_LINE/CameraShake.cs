using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : Singleton<CameraShake>
{
    [SerializeField] private GameObject cameraObj;

    public IEnumerator Shake(float duration, float magnitude)
    {

        Vector3 originalPos = transform.position;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.position = new Vector3(originalPos.x + x, originalPos.y + y, -10);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (cameraObj != null)
        {
            transform.position = new Vector3(cameraObj.transform.position.x, cameraObj.transform.position.y, -10);
        }
    }
}
