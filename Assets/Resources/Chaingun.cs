using UnityEngine;
using Photon.Pun;

public class Chaingun : MonoBehaviourPunCallbacks
{
    public float baseFireRate = 0.1f; // Podstawowa częstotliwość ognia
    public GameObject bulletPrefab;
    public Transform firePoint;
    public ParticleSystem muzzleFlash;
    public float bulletSpeed = 100f;
    public GameObject rotatingPart; // Rotująca część broni
    public float rotationSpeed = 1000f;

    public float maxSpread = 0.5f; // Maksymalne rozproszenie
    public float minSpread = 0.1f; // Minimalne rozproszenie

    private float nextTimeToFire = 0f;
    private AudioSource audioSource;
    public AudioClip shootSound;
    private PlayerAmmoManager playerAmmoManager; // Lokalne zarządzanie amunicją dla gracza
    private AmmoUI ammoUI; // UI dla lokalnego gracza
    private CoolingSystem coolingSystem; // System chłodzenia

    private bool isActiveWeapon = false;
    private float weaponSwitchTime; // Czas przełączenia broni
    private float timeFiring; // Czas strzelania
    [SerializeField] private float rampUpTime = 1f; // Czas przyspieszenia ognia

    void Start()
    {
        if (!photonView.IsMine) return; // Tylko lokalny gracz obsługuje broń

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        coolingSystem = GetComponent<CoolingSystem>();

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

        UpdateAmmoUI();
    }

    void Update()
    {
        if (!photonView.IsMine) return; // Tylko lokalny gracz obsługuje broń

        if (!isActiveWeapon) return;

        if (Input.GetButton("Fire1"))
        {
            if (Time.time >= nextTimeToFire)
            {
                if (playerAmmoManager != null && playerAmmoManager.UseAmmo("Chaingun") && coolingSystem.currentHeat < coolingSystem.maxHeat)
                {
                    timeFiring = Time.time - weaponSwitchTime;
                    float fireRate = Mathf.Lerp(baseFireRate * 2, baseFireRate, Mathf.Clamp01(timeFiring / rampUpTime));
                    nextTimeToFire = Time.time + fireRate;
                    Shoot();
                    coolingSystem.IncreaseHeat();
                    UpdateAmmoUI(); // Zaktualizuj UI po strzale
                }
            }
            RotateBarrel();
        }
        else if (Input.GetButtonUp("Fire1"))
        {
            timeFiring = 0f;
        }
    }

    public void SetActiveWeapon(bool active)
    {
        if (!photonView.IsMine) return;

        isActiveWeapon = active;

        if (active)
        {
            weaponSwitchTime = Time.time; // Zapisz czas, w którym broń została wybrana
            timeFiring = 0f; // Reset czasu strzelania
            UpdateAmmoUI(); // Zaktualizuj UI przy aktywowaniu broni
        }
    }

    void Shoot()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
            audioSource.PlayOneShot(shootSound);
        }

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        GameObject bullet = PhotonNetwork.Instantiate(bulletPrefab.name, firePoint.position, firePoint.rotation);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        Vector3 spread = firePoint.forward + new Vector3(Random.Range(-GetSpread(), GetSpread()), Random.Range(-GetSpread(), GetSpread()), 0);
        rb.velocity = spread * bulletSpeed;
        Destroy(bullet, 2f); // Zniszcz pocisk po 2 sekundach
    }

    float GetSpread()
    {
        float heatRatio = coolingSystem.currentHeat / coolingSystem.maxHeat;
        return Mathf.Lerp(minSpread, maxSpread, heatRatio);
    }

    void RotateBarrel()
    {
        if (rotatingPart != null)
        {
            rotatingPart.transform.Rotate(Vector3.down, rotationSpeed * Time.deltaTime);
        }
    }

    void UpdateAmmoUI()
    {
        if (ammoUI != null)
        {
            ammoUI.SetCurrentWeapon("Chaingun");
        }
    }

    // Funkcja do ustawiania czasu ostatniego strzału
    public void SetLastShotTime(float time)
    {
        nextTimeToFire = time;
    }

    // Funkcja do ustawiania ciepła w systemie chłodzenia
    public void SetHeat(float heat)
    {
        if (coolingSystem != null)
        {
            coolingSystem.currentHeat = heat;
        }
    }
}
