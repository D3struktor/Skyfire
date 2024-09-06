using UnityEngine;
using Photon.Pun;

public class GrenadeLauncher : MonoBehaviourPunCallbacks
{
    public GameObject grenadePrefab; // Prefab for the grenade
    public Transform shootingPoint; // The point from which grenades are shot
    public float grenadeSpeed = 20f; // Initial speed of the grenade
    public float grenadeGravity = 9.81f; // Gravity applied to the grenade
    public float grenadeDrag = 1f; // Drag for the grenade
    public float grenadeAngularDrag = 5f; // Angular drag for the grenade
    public float fireCooldown = 0.7f; // Cooldown time between shots
    public AudioClip shootSound; // Sound clip to play when shooting
    public float weaponSwitchDelay = 0.5f; // Delay after switching weapon

    public bool isActiveWeapon = false;
    private float lastShotTime = 0f; // Time when the last shot was fired
    private float lastWeaponSwitchTime = 0f; // Time when the weapon was last switched
        private AudioSource audioSource;

    private PlayerAmmoManager playerAmmoManager; // Lokalne zarządzanie amunicją
    private AmmoUI ammoUI; // UI dla lokalnego gracza

    void Start()
    {
        if (!photonView.IsMine) return; // Tylko lokalny gracz obsługuje broń

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Szukamy PlayerAmmoManager na obiekcie gracza
        Transform playerTransform = transform.root; // Znajdź główny obiekt gracza
        playerAmmoManager = playerTransform.GetComponent<PlayerAmmoManager>();

        if (playerAmmoManager == null)
        {
            Debug.LogError("Brak komponentu PlayerAmmoManager na obiekcie gracza: " + playerTransform.name);
        }

        // Znajdź AmmoUI
        ammoUI = FindObjectOfType<AmmoUI>();
        if (ammoUI == null)
        {
            Debug.LogError("Nie znaleziono komponentu AmmoUI w scenie.");
        }
        else
        {
            Debug.Log("AmmoUI zostało poprawnie przypisane.");
        }

        // Zaktualizuj UI na starcie
        UpdateAmmoUI();
    }

    void Update()
    {
        if (!photonView.IsMine) return; // Tylko lokalny gracz obsługuje broń

        if (!isActiveWeapon) return;

        if (Time.time < lastWeaponSwitchTime + 0.5f) return;

        if (Input.GetButtonDown("Fire1") && Time.time >= lastShotTime + fireCooldown)
        {
            if (playerAmmoManager != null && playerAmmoManager.UseAmmo("GrenadeLauncher"))
            {
                ShootGrenade();
                lastShotTime = Time.time;
                UpdateAmmoUI(); // Zaktualizuj UI po strzale
            }
            else
            {
                Debug.Log("Brak amunicji!");
            }
        }
    }

    public void SetActiveWeapon(bool active)
    {
        if (!photonView.IsMine) return;

        isActiveWeapon = active;

        if (active)
        {
            lastWeaponSwitchTime = Time.time;
            UpdateAmmoUI(); // Zaktualizuj UI podczas aktywacji broni
        }
    }

  void ShootGrenade()
    {
        if (grenadePrefab == null || shootingPoint == null)
        {
            Debug.LogError("Grenade prefab or shooting point is not assigned.");
            return;
        }

        GameObject grenade = PhotonNetwork.Instantiate(grenadePrefab.name, shootingPoint.position, shootingPoint.rotation);

        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = shootingPoint.forward * grenadeSpeed;
            rb.useGravity = true;
            rb.mass = 1f; // Adjust mass as needed
            rb.drag = grenadeDrag; // Apply drag
            rb.angularDrag = grenadeAngularDrag; // Apply angular drag
        }
        else
        {
            Debug.LogError("Rigidbody component not found on grenade prefab.");
        }

        PlayShootSound();
    }
        void PlayShootSound()
    {
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        else
        {
            Debug.LogError("AudioSource or shootSound not assigned.");
        }
    }

    void UpdateAmmoUI()
    {
        if (ammoUI != null)
        {
            ammoUI.SetCurrentWeapon("GrenadeLauncher");
        }
    }
    public void SetLastShotTime(float time)
    {
        lastShotTime = time;
    }

}
