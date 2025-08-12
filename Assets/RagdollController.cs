using UnityEngine;
using Photon.Pun;

public class RagdollActivator : MonoBehaviour
{
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    private Collider playerCollider; // Lokalny collider gracza

    void Awake()
    {
        // Pobierz wszystkie Rigidbody i Collidery w prefabie
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        // Znajdź lokalnego gracza
        FindLocalPlayerCollider();

        // Ustaw ragdolla jako aktywnego
        ActivateRagdoll();
    }

private void FindLocalPlayerCollider()
{
    foreach (var player in FindObjectsOfType<PlayerController>()) // Zmień PlayerController na nazwę Twojej klasy kontrolera gracza
    {
        var photonView = player.GetComponent<PhotonView>();
        if (photonView != null && photonView.IsMine) // Tylko lokalny gracz
        {
            playerCollider = player.GetComponent<Collider>();
            if (playerCollider == null)
            {
                Debug.LogError("PlayerController does not have a Collider component!");
            }
            return;
        }
    }
    Debug.LogError("Local player not found!");
}


public void ActivateRagdoll()
{
    foreach (Rigidbody rb in ragdollRigidbodies)
    {
        rb.isKinematic = false; // Aktywuj fizykę ragdolla
        rb.detectCollisions = true;
    }

    // foreach (Collider col in ragdollColliders)
    // {
    //     col.enabled = true;

    //     // Ignoruj kolizje między ragdollem a lokalnym graczem
    //     if (playerCollider != null)
    //     {
    //         Physics.IgnoreCollision(col, playerCollider, true);
    //     }
    // }
}

}
