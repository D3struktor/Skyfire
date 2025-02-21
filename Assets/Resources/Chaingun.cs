using UnityEngine;
using Photon.Pun;

public class Chaingun : MonoBehaviourPunCallbacks
{
    public float baseFireRate = 0.1f;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public ParticleSystem muzzleFlash;
    public float bulletSpeed = 100f;
    public GameObject rotatingPart; 
    public float maxRotationSpeed = 1000f; 
    public float coolDownSpeed = 500f; 

    public float maxSpread = 0.5f;
    public float minSpread = 0.1f;

    private float nextTimeToFire = 0f;
    private AudioSource audioSource;
    public AudioClip shootSound;
    private PlayerAmmoManager playerAmmoManager;
    private AmmoUI ammoUI;
    private CoolingSystem coolingSystem;

    public bool isActiveWeapon = false;
    private float weaponSwitchTime;
    private float timeFiring;
    [SerializeField] private float rampUpTime = 1f;
    private float lastWeaponSwitchTime = 0f;

    private float currentRotationSpeed = 0f; 

    void Start()
    {
        if (!photonView.IsMine) return;

        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        coolingSystem = GetComponent<CoolingSystem>();

        Transform playerTransform = transform.root;
        playerAmmoManager = playerTransform.GetComponent<PlayerAmmoManager>();

        if (playerAmmoManager == null)
            Debug.LogError("Brak komponentu PlayerAmmoManager na obiekcie gracza: " + playerTransform.name);

        ammoUI = FindObjectOfType<AmmoUI>();
        if (ammoUI == null)
            Debug.LogError("Nie znaleziono komponentu AmmoUI w scenie.");

        UpdateAmmoUI();
    }

    void Update()
    {
        if (!photonView.IsMine || !isActiveWeapon) return;
        if (Time.time < lastWeaponSwitchTime + 0.3f) return;

        if (Input.GetButton("Fire1"))
        {
            if (Time.time >= nextTimeToFire)
            {
                if (playerAmmoManager != null && playerAmmoManager.UseAmmo("Chaingun") && coolingSystem.currentHeat < coolingSystem.maxHeat)
                {
                    float rampUpFactor = Mathf.Clamp01((Time.time - weaponSwitchTime) / rampUpTime);
                    float fireRate = Mathf.Lerp(baseFireRate * 2, baseFireRate, rampUpFactor);
                    nextTimeToFire = Time.time + fireRate;

                    Shoot();
                    coolingSystem.IncreaseHeat();
                    UpdateAmmoUI();
                }
            }
            RotateBarrel(true);
        }
        else if (Input.GetButtonUp("Fire1"))
        {
            timeFiring = 0f;
        }

        RotateBarrel(false);
    }

    public void SetActiveWeapon(bool active)
    {
        if (!photonView.IsMine) return;

        isActiveWeapon = active;
        if (active)
        {
            weaponSwitchTime = Time.time;
            lastWeaponSwitchTime = Time.time;
            timeFiring = 0.12f;
            UpdateAmmoUI();
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
            audioSource.PlayOneShot(shootSound);

        GameObject bullet = PhotonNetwork.Instantiate(bulletPrefab.name, firePoint.position, firePoint.rotation);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        Vector3 spread = new Vector3(
            Random.Range(-GetSpread(), GetSpread()), 
            Random.Range(-GetSpread(), GetSpread()), 
            0f);
        
        Vector3 direction = firePoint.forward + firePoint.TransformDirection(spread);
        rb.velocity = direction.normalized * bulletSpeed;

        Destroy(bullet, 2f);
    }

    float GetSpread()
    {
        float heatRatio = coolingSystem.currentHeat / coolingSystem.maxHeat;
        return Mathf.Lerp(minSpread, maxSpread, heatRatio);
    }

    // 🔥 Obracanie beczki z ramp-upem i zwalnianiem
    void RotateBarrel(bool isFiring)
    {
        if (rotatingPart != null)
        {
            if (isFiring)
            {
                float rampUpFactor = Mathf.Clamp01((Time.time - weaponSwitchTime) / rampUpTime);
                currentRotationSpeed = Mathf.Lerp(0, maxRotationSpeed, rampUpFactor);
            }
            else
            {
                currentRotationSpeed = Mathf.Max(0, currentRotationSpeed - coolDownSpeed * Time.deltaTime);
            }

            rotatingPart.transform.Rotate(Vector3.down, currentRotationSpeed * Time.deltaTime);
        }
    }

    void UpdateAmmoUI()
    {
        if (ammoUI != null)
            ammoUI.SetCurrentWeapon("Chaingun");
    }

    public void SetLastShotTime(float time)
    {
        nextTimeToFire = time;
    }

    public void SetHeat(float heat)
    {
        if (coolingSystem != null)
            coolingSystem.currentHeat = heat;
    }
}
