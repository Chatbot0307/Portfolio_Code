using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/EquippedItemDatabase")]
public class EquippedItemHandler : ScriptableObject
{
    [System.Serializable]
    public struct ItemEntry { public int itemId; public GameObject prefab; }

    public List<ItemEntry> items = new List<ItemEntry>();

    public GameObject GetPrefabById(int id)
    {
        foreach (var e in items) if (e.itemId == id) return e.prefab;
        return null;
    }
}
