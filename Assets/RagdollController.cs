using UnityEngine;

public class RagdollActivator : MonoBehaviour
{
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    void Awake()
    {
        // Pobierz wszystkie Rigidbody i Collidery w prefabie
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        // Ustaw ragdolla jako aktywnego od razu, jeśli prefab jest wywoływany bezpośrednio jako ragdoll
        ActivateRagdoll();
    }

    public void ActivateRagdoll()
    {
        // Ustaw fizykę ragdolla
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = false; // Wyłącz isKinematic, aby aktywować fizykę
            rb.detectCollisions = true;
        }

        // Włącz Collidery
        foreach (Collider col in ragdollColliders)
        {
            col.enabled = true;
        }
    }
}
