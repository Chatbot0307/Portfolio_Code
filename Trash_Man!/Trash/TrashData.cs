using UnityEngine;

[CreateAssetMenu(fileName = "New Trash Data", menuName = "Trash Game/Trash Data")]
public class TrashData : ScriptableObject
{
    [Header("기본 정보")]
    public string typeName; // 쓰레기 종류 (재활용품같은거) 
    public bool isBomb = false;

    [Header("색상 설정")]
    public Color playerColor = Color.white;
    public bool tintTrash = true;

    [Header("쓰레기 이미지들")]
    public Sprite[] sprites;

    // 랜덤 스프라이트 하나 뽑기
    public Sprite GetRandomSprite()
    {
        if (sprites == null || sprites.Length == 0) return null;
        return sprites[Random.Range(0, sprites.Length)];
    }
}