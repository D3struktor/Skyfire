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

    public float maxSpread = 5f; // Maksymalny rozrzut
    public float minSpread = 1f; // Minimalny rozrzut

    private float nextTimeToFire = 0f;
    private CoolingSystem coolingSystem;

    private float lastShotTime; // Zmienna przechowująca czas ostatniego strzału

    void Start()
    {
        coolingSystem = GetComponent<CoolingSystem>();
        if (muzzleFlash == null)
        {
            Debug.LogError("Muzzle flash particle system is not assigned.");
        }
    }

    void Update()
    {
        if (photonView.IsMine && Input.GetButton("Fire1"))
        {
            if (Time.time >= nextTimeToFire)
            {
                if (coolingSystem.currentHeat < coolingSystem.maxHeat)
                {
                    nextTimeToFire = Time.time + fireRate;
                    Shoot();
                    coolingSystem.IncreaseHeat();
                    // Dodatkowy wzrost ciepła
                    coolingSystem.currentHeat += 10f; // Dodane ręcznie zwiększenie ciepła
                }
            }
            RotateBarrel();
        }

        // Debugowanie currentHeat
        Debug.Log("Current Heat: " + coolingSystem.currentHeat);
    }

    void Shoot()
    {
        photonView.RPC("RPC_Shoot", RpcTarget.All, firePoint.position, firePoint.rotation);
        lastShotTime = Time.time; // Zaktualizuj czas ostatniego strzału
    }

    [PunRPC]
    void RPC_Shoot(Vector3 firePosition, Quaternion fireRotation)
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        GameObject bullet = Instantiate(bulletPrefab, firePosition, fireRotation);
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
        gameObject.SetActive(isActive);
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
