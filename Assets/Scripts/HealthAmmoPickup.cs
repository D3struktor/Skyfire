using UnityEngine;
using Photon.Pun;

public class HealthAmmoPickup : MonoBehaviour
{
    public float healthRestorePercentage = 0.16f; // 16% zdrowia
    public float ammoRestorePercentage = 0.16f;  // 16% amunicji
    public AudioClip pickupSound;  // Dźwięk podniesienia
    private Rigidbody rb;
    private AudioSource audioSource; // Komponent AudioSource

    void Start()
    {
        // Dodaj Rigidbody, jeśli nie ma
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = true;        // Włącz grawitację
        rb.isKinematic = false;      // Wyłącz kinematykę, aby fizyka działała
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Aby uniknąć przenikania przez obiekty

        // Dodaj lub znajdź AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1.0f;  // Ustaw przestrzenność dźwięku (3D)
    }

    // Funkcja, która wywoła się po kolizji z ziemią
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground")) // Sprawdź, czy pickup dotknął ziemi
        {
            // Zatrzymaj ruch po dotknięciu ziemi
            rb.isKinematic = true; // Ustaw Rigidbody na kinematyczny, aby zatrzymać ruch
        }
    }

    // Funkcja wywoływana, gdy gracz wejdzie w trigger
    void OnTriggerEnter(Collider other)
    {
        // Sprawdzamy, czy obiekt, który wszedł w trigger, to gracz
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.photonView.IsMine)  // Tylko lokalny gracz
        {
            Debug.Log("Pickup zebrany przez: " + player.name);

            // Odtwórz dźwięk lokalnie dla gracza przed zniszczeniem obiektu
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            // Odnawiamy zdrowie
            player.RestoreHealthOverTime(0.3f, 3f);

            // Odnawiamy amunicję
            PlayerAmmoManager ammoManager = player.GetComponent<PlayerAmmoManager>();
            if (ammoManager != null)
            {
                ammoManager.RestoreAmmo(ammoRestorePercentage);
            }
            else
            {
                Debug.LogError("Brak komponentu PlayerAmmoManager na obiekcie gracza.");
            }

            // Zniszcz pickup natychmiast po zebraniu
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
