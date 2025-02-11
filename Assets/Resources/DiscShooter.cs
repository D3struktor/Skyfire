using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DiscShooter : MonoBehaviourPunCallbacks
{
    public GameObject discPrefab;
    public Transform shootingPoint;
    public float discSpeed = 60f;
    public float fireCooldown = 0.7f;
    public AudioClip shootSound;
    public float weaponSwitchDelay = 1f;

    public bool isActiveWeapon = false;
    private float lastShotTime = 0f;
    private float lastWeaponSwitchTime = 0f;
    private AudioSource audioSource;
    private PlayerAmmoManager playerAmmoManager;
    private AmmoUI ammoUI;

    public float ignoreCollisionTime = 0.5f; // 💥 Przywrócone!
    public float discRotationSpeed = 360f;
    public GameObject attachedDisc;

    void Start()
    {
        if (!photonView.IsMine) return;

        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        Transform playerTransform = transform.root;
        playerAmmoManager = playerTransform.GetComponent<PlayerAmmoManager>();

        if (playerAmmoManager == null)
            Debug.LogError("Brak komponentu PlayerAmmoManager na obiekcie gracza: " + playerTransform.name);

        ammoUI = FindObjectOfType<AmmoUI>();
        if (ammoUI == null)
            Debug.LogError("Nie znaleziono komponentu AmmoUI w scenie.");

        attachedDisc = FindDiscInChildren(transform);

        if (attachedDisc != null)
            Debug.Log("✅ Przypisano obiekt Disc: " + attachedDisc.name);
        else
            Debug.LogError("🚨 Nie znaleziono obiektu Disc w hierarchii!");

        UpdateAmmoUI();
    }

    private GameObject FindDiscInChildren(Transform parent)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Disc")
                return child.gameObject;
        }
        return null;
    }

    void Update()
    {
        if (!photonView.IsMine || !isActiveWeapon || Time.time < lastWeaponSwitchTime + weaponSwitchDelay) return;

        if (attachedDisc != null)
            attachedDisc.transform.Rotate(Vector3.forward * discRotationSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Fire1") && Time.time >= lastShotTime + fireCooldown)
        {
            if (playerAmmoManager != null && playerAmmoManager.UseAmmo("DiscShooter"))
            {
                ShootDisc();
                lastShotTime = Time.time;
                UpdateAmmoUI();
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
        if (!photonView.IsMine) return;

        isActiveWeapon = active;
        if (active)
        {
            lastWeaponSwitchTime = Time.time;
            UpdateAmmoUI();
        }
    }

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

        GameObject disc = PhotonNetwork.Instantiate(discPrefab.name, shootingPoint.position, shootingPoint.rotation);
        Rigidbody rb = disc.GetComponent<Rigidbody>();

        if (rb != null)
            rb.velocity = shootingPoint.forward * discSpeed;
        else
            Debug.LogError("Brak komponentu Rigidbody na prefabbie dysku.");

        PlayShootSound();
        StartCoroutine(IgnoreCollisionTemporarily(disc));
    }

    void PlayShootSound()
    {
        if (shootSound != null)
            audioSource.PlayOneShot(shootSound);
        else
            Debug.LogError("AudioSource lub dźwięk strzału nie są przypisane.");
    }

    void UpdateAmmoUI()
    {
        if (ammoUI != null)
            ammoUI.SetCurrentWeapon("DiscShooter");
    }

IEnumerator IgnoreCollisionTemporarily(GameObject disc)
{
    if (disc == null) yield break; // Jeśli dysk już nie istnieje, kończymy coroutine

    Collider discCollider = disc.GetComponent<Collider>();
    Collider playerCollider = transform.root.GetComponent<Collider>();

    if (discCollider == null || playerCollider == null) yield break; // Dodatkowe zabezpieczenie

    Physics.IgnoreCollision(discCollider, playerCollider, true);
    yield return new WaitForSeconds(ignoreCollisionTime);

    if (discCollider == null || playerCollider == null) yield break; // Sprawdzamy jeszcze raz przed przywróceniem kolizji

    Physics.IgnoreCollision(discCollider, playerCollider, false);
}

}
