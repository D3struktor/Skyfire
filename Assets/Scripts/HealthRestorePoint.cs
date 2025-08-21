using UnityEngine;
using Photon.Pun;

public class HealthRestorePoint : MonoBehaviour
{
    public float healthRestorePercentage = 0.16f; // 16% health
    public float ammoRestorePercentage = 0.16f;   // 16% ammo
    public float restoreCooldown = 60f;           // Cooldown after pickup (60 seconds)
    public AudioClip pickupSound;                 // Pickup sound
    private Rigidbody rb;
    private PhotonView photonView;
    private bool isAvailable = true;              // Checks if pickup is available
    public int volumeBoostFactor = 5;             // How many times to play sound to boost it (500% = 5x)

    void Start()
    {
        // Initialize PhotonView
        photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            Debug.LogError("[HealthRestorePoint] PhotonView jest null. Upewnij się, że PhotonView jest przypisany do tego obiektu.");
        }

        // Add Rigidbody if missing
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false; // Disable gravity
        rb.isKinematic = true; // Make object kinematic (no physics)
    }

    // void Update()
    // {
    //     // Rotate object around its local axis
    //     transform.Rotate(Vector3.up * 50 * Time.deltaTime, Space.Self); // Space.Self ensures rotation uses local coordinates
    // }

    // Function called when player enters the trigger
    void OnTriggerEnter(Collider other)
    {
        if (!isAvailable) return; // Check if pickup is available

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.photonView.IsMine)  // Only local player
        {
            Debug.Log("Restore point collected by: " + player.name);

            // Boost sound by playing multiple times
            if (pickupSound != null)
            {
                Debug.Log("Playing pickupSound with boost.");
                for (int i = 0; i < volumeBoostFactor; i++)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position, 1.0f); // Play sound 5 times
                }
            }
            else
            {
                Debug.LogWarning("No sound to play.");
            }

            // Restore health
            player.RestoreHealthOverTime(healthRestorePercentage, 3f);

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

            // Trigger cooldown and hide pickup (entire object with children)
            photonView.RPC("ActivateCooldown", RpcTarget.AllBuffered);
        }
    }

    // Funkcja uruchamia cooldown na wszystkich klientach
    [PunRPC]
    public void ActivateCooldown()
    {
        if (isAvailable) // Check if pickup was available
        {
            isAvailable = false; // Mark pickup as unavailable
            Debug.Log("Hiding HealthRestorePoint");
            gameObject.SetActive(false); // Disable whole object
            Invoke(nameof(ResetPickup), restoreCooldown); // Restore after cooldown (60 seconds)
        }
    }

    // Function that restores pickup availability
    private void ResetPickup()
    {
        Debug.Log("Resetting HealthRestorePoint");
        isAvailable = true;
        gameObject.SetActive(true); // Show pickup again
    }
}
