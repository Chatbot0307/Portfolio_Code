using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalStateSender : MonoBehaviour
{
    public float sendPositionInterval = 0.05f; 
    public float positionThreshold = 0.05f;
    public float rotationThreshold = 1f;

    private Vector3 lastSentPos;
    private float lastSentRotY;
    private NetworkSyncPlayer sync;

    void Start()
    {
        sync = GetComponent<NetworkSyncPlayer>();
        StartCoroutine(PositionSendLoop());
    }

    IEnumerator PositionSendLoop()
    {
        while (true)
        {
            TrySendPosition();
            yield return new WaitForSeconds(sendPositionInterval);
        }
    }

    void TrySendPosition()
    {
        Vector3 pos = transform.position;
        float rotY = transform.eulerAngles.y;

        if (Vector3.Distance(pos, lastSentPos) > positionThreshold || Mathf.Abs(Mathf.DeltaAngle(rotY, lastSentRotY)) > rotationThreshold)
        {
            LOPNetworkManager.Instance?.SendPosition(pos, rotY);
            lastSentPos = pos;
            lastSentRotY = rotY;
        }
    }
}
