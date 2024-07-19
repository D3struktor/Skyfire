using UnityEngine;
using Photon.Pun;

public class Chaingun : MonoBehaviourPunCallbacks
{
    public float fireRate = 0.1f; // Częstotliwość strzelania (w sekundach)
    public GameObject bulletPrefab;
    public Transform firePoint;
    public ParticleSystem muzzleFlash;
    public float bulletSpeed = 100f;
    public GameObject rotatingPart; // Obiekt rotujący
    public float rotationSpeed = 1000f; // Prędkość rotacji obiektu

    public float maxSpread = 0.5f; // Maksymalny rozrzut, zmniejszony dla większej precyzji
    public float minSpread = 0.1f; // Minimalny rozrzut, zmniejszony dla większej precyzji

    private float nextTimeToFire = 0f;
    private CoolingSystem coolingSystem;
    private AudioSource audioSource; // Dodajemy AudioSource
    public AudioClip shootSound; // Dodajemy pole dla klipu dźwiękowego

    private float lastShotTime; // Zmienna przechowująca czas ostatniego strzału
    private bool isActiveWeapon = false; // Flaga aktywności broni
    private float weaponSwitchTime; // Czas, kiedy broń została wybrana

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
                    nextTimeToFire = Time.time + fireRate;
                    Shoot();
                    coolingSystem.IncreaseHeat();
                }
            }
            RotateBarrel();
        }
    }

    void Shoot()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
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
            weaponSwitchTime = Time.time; // Zapisujemy czas, kiedy broń została wybrana
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
