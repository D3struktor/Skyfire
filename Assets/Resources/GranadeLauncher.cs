using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GrenadeLauncher : MonoBehaviourPunCallbacks
{
    [Header("Prefaby / Punkty")]
    public GameObject grenadePrefab;
    public Transform shootingPoint;

    [Header("Parametry strzału")]
    public float grenadeSpeed = 45f;          // „muzzle speed” – szybkie, ale łuk (feeling TV)
    public float inheritVelocityFactor = 1.0f;// dziedziczenie prędkości gracza jak w Tribes
    public float grenadeDrag = 0.2f;
    public float grenadeAngularDrag = 0.05f;

    [Header("Strzelanie / cooldown")]
    public float fireCooldown = 0.6f;
    public AudioClip shootSound;
    public bool isActiveWeapon = false;

    [Header("UI / Ammo")]
    public float weaponSwitchDelay = 0.5f;
    private PlayerAmmoManager playerAmmoManager;
    private AmmoUI ammoUI;

    private float lastShotTime = 0f;
    private float lastWeaponSwitchTime = 0f;
    private AudioSource audioSource;

    void Start()
    {
        if (!photonView.IsMine) return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        Transform playerRoot = transform.root;
        playerAmmoManager = playerRoot.GetComponent<PlayerAmmoManager>();
        if (playerAmmoManager == null)
            Debug.LogError("Brak PlayerAmmoManager na: " + playerRoot.name);

        ammoUI = FindObjectOfType<AmmoUI>();
        if (ammoUI == null) Debug.LogError("Nie znaleziono AmmoUI w scenie.");

        UpdateAmmoUI();
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        if (!isActiveWeapon) return;
        if (Time.time < lastWeaponSwitchTime + weaponSwitchDelay) return;

        if (Input.GetButtonDown("Fire1") && Time.time >= lastShotTime + fireCooldown)
        {
            if (playerAmmoManager != null && playerAmmoManager.UseAmmo("GrenadeLauncher"))
            {
                ShootGrenade();
                lastShotTime = Time.time;
                UpdateAmmoUI();
            }
            else
            {
                // brak ammo – ewentualnie klik / UI
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
            UpdateAmmoUI();
        }
    }

    void ShootGrenade()
    {
        if (grenadePrefab == null || shootingPoint == null)
        {
            Debug.LogError("Grenade prefab lub shootingPoint nie są przypisane.");
            return;
        }

        // Dziedziczenie prędkości + prędkość wylotowa – jak w Tribes
        Vector3 muzzleVel = shootingPoint.forward * grenadeSpeed;
        Rigidbody playerRb = transform.root.GetComponent<Rigidbody>();
        Vector3 inherit = playerRb ? playerRb.velocity * inheritVelocityFactor : Vector3.zero;
        Vector3 initialVelocity = muzzleVel + inherit;

        // InstantiationData: startowa prędkość + numer aktora właściciela
        object[] data = new object[] { initialVelocity, PhotonNetwork.LocalPlayer.ActorNumber };

        GameObject grenade = PhotonNetwork.Instantiate(
            grenadePrefab.name,
            shootingPoint.position,
            shootingPoint.rotation,
            0,
            data
        );

        // Lokalne dopieszczenie RB (drag, angularDrag). Prędkość i „owner” ustawi się na wszystkich w Grenade.OnPhotonInstantiate
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;          // parabola „tribesowa”
            rb.drag = grenadeDrag;
            rb.angularDrag = grenadeAngularDrag;
        }

        PlayShootSound();
    }

    void PlayShootSound()
    {
        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);
    }

    void UpdateAmmoUI()
    {
        if (ammoUI != null)
            ammoUI.SetCurrentWeapon("GrenadeLauncher");
    }

    public void SetLastShotTime(float t) => lastShotTime = t;
}
