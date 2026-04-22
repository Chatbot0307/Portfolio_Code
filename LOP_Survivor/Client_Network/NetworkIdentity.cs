using UnityEngine;

/// <summary>
/// 네트워크 오브젝트의 고유 ID와 소유자 정보를 저장합니다.
/// </summary>
public class NetworkIdentity : MonoBehaviour
{
    // NetworkId는 서버가 할당하며, 클라이언트에서 직접 할당할 수 없도록 private set
    [field: SerializeField] public int NetworkId { get; private set; } = 0;
    [field: SerializeField] public bool IsOwner { get; private set; } = false; // 이 클라이언트가 소유자인지 여부

    // LOPNetworkManager에서 호출되어 ID를 설정합니다.
    public void SetIdentity(int networkId, bool isOwner)
    {
        NetworkId = networkId;
        IsOwner = isOwner;
    }
}