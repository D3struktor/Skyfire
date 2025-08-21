using UnityEngine;
using Photon.Pun;

public class HealthAmmoPickup : MonoBehaviour
{
    public float healthRestorePercentage = 0.16f; // 16% health
    public float ammoRestorePercentage = 0.16f;  // 16% ammo
    public AudioClip pickupSound;  // Pickup sound
    private Rigidbody rb;
    private AudioSource audioSource; // Komponent AudioSource
    private PhotonView photonView;   // Dodajemy PhotonView

    void Start()
    {
        // Initialize PhotonView
        photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            Debug.LogError("[HealthAmmoPickup] PhotonView jest null. Upewnij się, że PhotonView jest przypisany do tego obiektu.");
        }

        // Add Rigidbody if missing
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = true;        // Enable gravity
        rb.isKinematic = false;      // Disable kinematic for physics
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Avoid tunneling

        // Dodaj lub znajdź AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1.0f;  // Set sound spatialization (3D)
    }

    // Function called when colliding with the ground
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground")) // Check if pickup touched the ground
        {
            // Stop movement after touching the ground
            rb.isKinematic = true; // Make Rigidbody kinematic to stop movement
        }
    }

    // Function called when a player enters the trigger
    void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is a player
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.photonView.IsMine)  // Tylko lokalny gracz
        {
            Debug.Log("Pickup collected by: " + player.name);

            // Play sound locally for the player before destroying the object
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            // Restore health
            player.RestoreHealthOverTime(0.3f, 3f);

            // Restore ammo
            PlayerAmmoManager ammoManager = player.GetComponent<PlayerAmmoManager>();
            if (ammoManager != null)
            {
                ammoManager.RestoreAmmo(ammoRestorePercentage);
            }
            else
            {
                Debug.LogError("PlayerAmmoManager component missing on player object.");
            }

            // Instead of directly destroying the pickup, delegate to MasterClient via RPC
            photonView.RPC("DestroyPickup", RpcTarget.MasterClient); // Use photonView
        }
    }

    [PunRPC]
    public void DestroyPickup()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("MasterClient: Destroying pickup");
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
