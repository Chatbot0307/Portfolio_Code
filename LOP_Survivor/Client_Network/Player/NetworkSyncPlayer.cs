using System.Collections.Generic;
using UnityEngine;

public class NetworkSyncPlayer : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private Vector3 targetPos;
    private Quaternion targetRot;
    public string playerId;

    // equipped item instance for remote view
    private GameObject equippedInstance;

    private float positionLerpSpeed = 10f;
    private float rotationLerpSpeed = 10f;

    private Dictionary<int, bool> lastApplied = new Dictionary<int, bool>();

    void Awake()
    {
        // Animator가 null이면 자식에서 찾아서 가져오기
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true); // 비활성 포함

        targetPos = transform.position;
        targetRot = transform.rotation;
    }

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * positionLerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationLerpSpeed);
    }

    public void SetPlayerId(string id) => playerId = id;

    public void ApplyPosition(Vector3 pos, Quaternion rot)
    {
        targetPos = pos;
        targetRot = rot;
    }

    public void ApplyAnimationState(int paramHash, bool value)
    {
        if (lastApplied.TryGetValue(paramHash, out var prev) && prev == value) return;
        lastApplied[paramHash] = value;

        animator.SetBool(paramHash, value);

        Debug.Log($"Applied animation state: {paramHash} = {value}");
    }

    public void ApplyAnimationTrigger(int paramHash)
    {
        Debug.Log($"Applying animation trigger: {paramHash}");
        animator.SetTrigger(paramHash);
    }

    //public void ApplyEquippedItem(GameObject prefab)
    //{
    //    if (equippedInstance != null) Destroy(equippedInstance);
    //    if (prefab == null) return;

    //    // find a hand bone (heuristic): try "Hand_R" or "RightHand" or use transform
    //    Transform hand = FindHandTransform();
    //    equippedInstance = Instantiate(prefab, hand ? hand : transform);
    //    equippedInstance.transform.localPosition = Vector3.zero;
    //    equippedInstance.transform.localRotation = Quaternion.identity;
    //}

    private Transform FindHandTransform()
    {
        // try common names, fallback to first child
        var t = transform.Find("Hand_R") ?? transform.Find("RightHand");
        if (t != null) return t;
        if (transform.childCount > 0) return transform.GetChild(0);
        return transform;
    }
}
