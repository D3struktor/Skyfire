using UnityEngine;
using Photon.Pun;

public class HealthRestorePoint : MonoBehaviour
{
    public float healthRestorePercentage = 0.16f; // 16% zdrowia
    public float ammoRestorePercentage = 0.16f;   // 16% amunicji
    public float restoreCooldown = 60f;           // Czas odnowienia po zebraniu (60 sekund)
    public AudioClip pickupSound;                 // Dźwięk podniesienia
    private Rigidbody rb;
    private PhotonView photonView;
    private bool isAvailable = true;              // Sprawdza, czy pickup jest dostępny
    public int volumeBoostFactor = 5;             // Jak wiele razy odtworzyć dźwięk, aby go wzmocnić (500% = 5x)

    void Start()
    {
        // Inicjalizujemy PhotonView
        photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            Debug.LogError("[HealthRestorePoint] PhotonView jest null. Upewnij się, że PhotonView jest przypisany do tego obiektu.");
        }

        // Dodaj Rigidbody, jeśli nie ma
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false; // Wyłączamy grawitację
        rb.isKinematic = true; // Ustawiamy obiekt jako kinematyczny (nie podlega fizyce)
    }

    // void Update()
    // {
    //     // Obracanie obiektu wokół jego własnej osi lokalnej
    //     transform.Rotate(Vector3.up * 50 * Time.deltaTime, Space.Self); // Space.Self zapewnia, że obrót dotyczy lokalnego układu współrzędnych obiektu
    // }

    // Funkcja wywoływana, gdy gracz wejdzie w trigger
    void OnTriggerEnter(Collider other)
    {
        if (!isAvailable) return; // Sprawdź, czy pickup jest dostępny

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.photonView.IsMine)  // Tylko lokalny gracz
        {
            Debug.Log("Restore point zebrany przez: " + player.name);

            // Wzmocnij dźwięk przez wielokrotne odtworzenie
            if (pickupSound != null)
            {
                Debug.Log("Odtwarzanie dźwięku z pickupSound z wzmocnieniem.");
                for (int i = 0; i < volumeBoostFactor; i++)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position, 1.0f); // Odtwarzanie dźwięku 5 razy
                }
            }
            else
            {
                Debug.LogWarning("Brak dźwięku do odtworzenia.");
            }

            // Odnawiamy zdrowie
            player.RestoreHealthOverTime(healthRestorePercentage, 3f);

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

            // Wywołujemy cooldown i ukrywamy pickup (cały obiekt z dziećmi)
            photonView.RPC("ActivateCooldown", RpcTarget.AllBuffered);
        }
    }

    // Funkcja uruchamia cooldown na wszystkich klientach
    [PunRPC]
    public void ActivateCooldown()
    {
        if (isAvailable) // Sprawdzamy, czy pickup był dostępny
        {
            isAvailable = false; // Ustawiamy, że pickup jest niedostępny
            Debug.Log("Ukrywanie HealthRestorePoint");
            gameObject.SetActive(false); // Wyłączamy cały obiekt
            Invoke(nameof(ResetPickup), restoreCooldown); // Przywracamy po cooldownie (60 sekund)
        }
    }

    // Funkcja przywracająca dostępność pickup'a
    private void ResetPickup()
    {
        Debug.Log("Resetowanie HealthRestorePoint");
        isAvailable = true;
        gameObject.SetActive(true); // Ponownie pokazujemy pickup
    }
}
