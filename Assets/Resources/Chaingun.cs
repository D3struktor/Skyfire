using UnityEngine;
using Photon.Pun;

public class Chaingun : MonoBehaviourPunCallbacks
{
    public float baseFireRate = 0.1f; // Base fire rate (in seconds)
    public GameObject bulletPrefab;
    public Transform firePoint;
    public ParticleSystem muzzleFlash;
    public float bulletSpeed = 100f;
    public GameObject rotatingPart; // Rotating part of the gun
    public float rotationSpeed = 1000f; // Rotation speed of the rotating part

    public float maxSpread = 0.5f; // Maximum spread
    public float minSpread = 0.1f; // Minimum spread

    private float nextTimeToFire = 0f;
    private CoolingSystem coolingSystem;
    private AudioSource audioSource;
    public AudioClip shootSound;

    private float lastShotTime;
    private bool isActiveWeapon = false; // Flag to check if the weapon is active
    private float weaponSwitchTime; // Time when the weapon was selected
    private float timeFiring; // Time the weapon has been firing
    [SerializeField] private float rampUpTime = 1f; // Time it takes to ramp up to full fire rate

    void Start()
    {
        coolingSystem = GetComponent<CoolingSystem>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (muzzleFlash == null)
        {
            Debug.LogError("Muzzle flash particle system is not assigned.");
        }
    }

    void Update()
    {
        if (!isActiveWeapon) return;

        if (photonView.IsMine && Input.GetButton("Fire1"))
        {
            if (Time.time >= nextTimeToFire && Time.time >= weaponSwitchTime + 0.1f)
            {
                if (coolingSystem.currentHeat < coolingSystem.maxHeat)
                {
                    // Calculate dynamic fire rate based on how long the player has been firing
                    timeFiring = Time.time - weaponSwitchTime; // Calculate the continuous firing duration
                    float fireRate = Mathf.Lerp(baseFireRate * 2, baseFireRate, Mathf.Clamp01(timeFiring / rampUpTime));

                    nextTimeToFire = Time.time + fireRate;
                    Shoot();
                    coolingSystem.IncreaseHeat();
                }
            }
            RotateBarrel();
        }
        else if (Input.GetButtonUp("Fire1"))
        {
            timeFiring = 0f; // Reset firing duration when the player stops firing
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
        Destroy(bullet, 2f); // Destroy bullet after 2 seconds to clean up
    }

    public float GetSpread()
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

    public void SetActiveWeapon(bool isActive)
    {
        isActiveWeapon = isActive;
        if (isActive)
        {
            weaponSwitchTime = Time.time; // Record the time the weapon was selected
            timeFiring = 0f; // Initialize the firing duration
        }
    }

    public void SetLastShotTime(float time)
    {
        lastShotTime = time;
    }

    public void SetHeat(float heat)
    {
        if (coolingSystem != null)
        {
            coolingSystem.currentHeat = heat;
        }
    }

    public float GetHeat()
    {
        if (coolingSystem != null)
        {
            return coolingSystem.currentHeat;
        }
        return 0f;
    }
}
