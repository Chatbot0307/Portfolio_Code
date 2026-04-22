using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicCameraController : MonoBehaviour
{
    public Transform player1;
    public Transform player2;

    public float minFOV = 30f;
    public float maxFOV = 100f;
    public float zoomSensitivity = 15f;
    public float smoothSpeed = 5f;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (player1 == null || player2 == null) return;

        // 카메라 위치를 두 플레이어의 중간으로 이동
        Vector3 midPoint = (player1.position + player2.position) / 2f;
        Vector3 newCamPos = new Vector3(midPoint.x, midPoint.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, newCamPos, Time.deltaTime * smoothSpeed);

        // 두 플레이어 간 거리 계산
        float distance = Vector2.Distance(player1.position, player2.position);

        // 거리 기반 FOV 계산
        float targetFOV = Mathf.Clamp(distance * zoomSensitivity, minFOV, maxFOV);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * smoothSpeed);
    }
}
