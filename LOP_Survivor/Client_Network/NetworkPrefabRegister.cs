using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Dictionary<int, GameObject>에서 .ToDictionary() 사용 시 필요

/// <summary>
/// 네트워크에서 생성 가능한 모든 프리팹 목록을 관리하고,
/// 프리팹 이름 해시 <-> 프리팹 GameObject 매핑 정보를 제공합니다.
/// </summary>
public class NetworkPrefabRegister : MonoBehaviour
{
    [Tooltip("네트워크에서 생성 가능한 모든 프리팹을 등록하세요.")]
    public GameObject[] networkPrefabs;

    // Key: PrefabName.GetHashCode(), Value: GameObject Prefab
    private Dictionary<int, GameObject> prefabHashToPrefab;

    public void Initialize()
    {
        if (prefabHashToPrefab != null) return;

        try
        {
            // O(N)으로 배열을 Dictionary로 변환 (Key는 프리팹 이름 해시)
            prefabHashToPrefab = networkPrefabs
                .ToDictionary(p => p.name.GetHashCode(), p => p);

            Debug.Log($"[NetworkPrefabRegister] Registered {prefabHashToPrefab.Count} network prefabs.");
        }
        catch (System.ArgumentException ex)
        {
            // 해시 충돌 발생 시
            Debug.LogError($"[NetworkPrefabRegister] Prefab Hash Collision detected! Check networkPrefabs for duplicate names or hashes. Error: {ex.Message}");
            prefabHashToPrefab = new Dictionary<int, GameObject>(); // 초기화 실패 시 빈 딕셔너리 유지
        }
    }

    /// <summary>
    /// 프리팹 이름 해시로 프리팹 오브젝트를 가져옵니다.
    /// </summary>
    public bool TryGetPrefab(int prefabHash, out GameObject prefab)
    {
        if (prefabHashToPrefab == null)
        {
            Debug.LogError("[NetworkPrefabRegister] Not initialized!");
            prefab = null;
            return false;
        }
        return prefabHashToPrefab.TryGetValue(prefabHash, out prefab);
    }

    /// <summary>
    /// 특정 프리팹의 해시 값을 가져옵니다.
    /// </summary>
    public int GetPrefabHash(GameObject prefab)
    {
        return prefab.name.GetHashCode();
    }
}