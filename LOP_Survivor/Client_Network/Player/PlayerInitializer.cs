using UnityEngine;

public class PlayerInitializer : MonoBehaviour
{
    [SerializeField] private NetworkIdentity networkIdentity;

    [Header("Script References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private NetworkSyncPlayer networkSync;
    [SerializeField] private LocalStateSender stateSender;

    [Header("GameObject References")]
    [SerializeField] private GameObject playerUI;
    [SerializeField] private GameObject tpRoot;
    [SerializeField] private GameObject fpRoot;
    [SerializeField] private  GameObject penguinObj;

    [Header("Physic References")]
    [SerializeField] private Collider playerCollider;
    [SerializeField] private Rigidbody playerRigidbody;

    void Start()
    {
        if (networkIdentity.IsOwner)
        {
            characterController.enabled = true;
            networkSync.enabled = false;
            stateSender.enabled = true;

            playerUI.SetActive(true);
            tpRoot.SetActive(true);
            fpRoot.SetActive(true);
            penguinObj.SetActive(false);

            playerCollider.isTrigger = false;
            playerRigidbody.useGravity = true;
        }
        else
        {
            characterController.enabled = false;
            networkSync.enabled = true;
            stateSender.enabled = false;

            playerUI.SetActive(false);
            tpRoot.SetActive(false);
            fpRoot.SetActive(false);
            penguinObj.SetActive(true);

            playerCollider.isTrigger = true;
            playerRigidbody.useGravity = false;
        }
    }

    private void OnDestroy()
    {
        Debug.Log("[PlayerInitializer] Player object destroyed.");
    }
}