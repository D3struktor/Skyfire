using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DiscShooter : MonoBehaviourPunCallbacks
{
    public GameObject discPrefab; // Prefab dla dysku
    public Transform shootingPoint; // Punkt, z którego strzelamy
    public float discSpeed = 60f; // Prędkość dysku
    public float fireCooldown = 0.7f; // Czas oczekiwania między strzałami
    public AudioClip shootSound; // Dźwięk strzału
    public float weaponSwitchDelay = 1f; // Opóźnienie po zmianie broni

    public bool isActiveWeapon = false;
    private float lastShotTime = 0f; // Czas ostatniego strzału
    private float lastWeaponSwitchTime = 0f; // Czas ostatniej zmiany broni
    private AudioSource audioSource;
    private PlayerAmmoManager playerAmmoManager; // Lokalne zarządzanie amunicją dla każdego gracza
    private AmmoUI ammoUI; // UI dla lokalnego gracza

    public float ignoreCollisionTime = 0.5f; // Czas ignorowania kolizji

void Start()
{
    if (!photonView.IsMine) return; // Tylko lokalny gracz obsługuje broń

    audioSource = GetComponent<AudioSource>();
    if (audioSource == null)
    {
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    // Szukamy PlayerAmmoManager na rodzicu gracza
    Transform playerTransform = transform.root; // Znajdź główny obiekt gracza
    playerAmmoManager = playerTransform.GetComponent<PlayerAmmoManager>();

    if (playerAmmoManager == null)
    {
        Debug.LogError("Brak komponentu PlayerAmmoManager na obiekcie gracza: " + playerTransform.name);
    }

    // Znajdź AmmoUI w scenie tylko raz
    ammoUI = FindObjectOfType<AmmoUI>();
    if (ammoUI == null)
    {
        Debug.LogError("Nie znaleziono komponentu AmmoUI w scenie.");
    }
    else
    {
        Debug.Log("AmmoUI został poprawnie przypisany.");
    }
    
        // Zaktualizuj UI na starcie
        UpdateAmmoUI();
}



    void Update()
    {
        if (!photonView.IsMine) return; // Tylko lokalny gracz obsługuje broń

        if (!isActiveWeapon) return;

        if (Time.time < lastWeaponSwitchTime + weaponSwitchDelay) return;

        if (Input.GetButtonDown("Fire1") && Time.time >= lastShotTime + fireCooldown)
        {
            if (playerAmmoManager != null && playerAmmoManager.UseAmmo("DiscShooter"))
            {
                ShootDisc();
                lastShotTime = Time.time; // Aktualizacja czasu ostatniego strzału
                UpdateAmmoUI(); // Zaktualizuj UI amunicji po strzale
            }
            else
            {
                Debug.Log("Brak amunicji!");
            }
        }

        Debug.DrawRay(shootingPoint.position, shootingPoint.forward * 10, Color.red);
    }

    public void SetActiveWeapon(bool active)
    {
        if (!photonView.IsMine) return; // Tylko lokalny gracz zarządza aktywacją broni

        if (active)
        {
            isActiveWeapon = true;
            lastWeaponSwitchTime = Time.time;

            // Zaktualizuj UI amunicji, gdy broń zostanie aktywowana
            UpdateAmmoUI();
        }
        else
        {
            isActiveWeapon = false;
        }
    }

    // Funkcja do ustawiania czasu ostatniego strzału z zewnętrznych skryptów
    public void SetLastShotTime(float time)
    {
        lastShotTime = time;
    }

  void ShootDisc()
    {
        if (discPrefab == null || shootingPoint == null)
        {
            Debug.LogError("Prefab dysku lub punkt strzału nie jest przypisany.");
            return;
        }
        Vector3 spawnPosition = shootingPoint.position + shootingPoint.forward * 0.5f; // Przesuń pocisk o 0.5 jednostki w przód

        GameObject disc = PhotonNetwork.Instantiate(discPrefab.name, shootingPoint.position, shootingPoint.rotation);

        Rigidbody rb = disc.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = shootingPoint.forward * discSpeed;
        }
        else
        {
            Debug.LogError("Brak komponentu Rigidbody na prefabbie dysku.");
        }

        PlayShootSound();
    }

    void PlayShootSound()
    {
        if (shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        else
        {
            Debug.LogError("AudioSource lub dźwięk strzału nie są przypisane.");
        }
    }

    // Funkcja do aktualizacji stanu UI amunicji
    void UpdateAmmoUI()
    {
        if (ammoUI != null)
        {
            ammoUI.SetCurrentWeapon("DiscShooter");
        }
    }
}
