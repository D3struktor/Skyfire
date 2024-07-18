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

    private bool isActiveWeapon = false;
    private float lastShotTime = 0f; // Time when the last shot was fired
    private float lastWeaponSwitchTime = 0f; // Time when the weapon was last switched
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (!isActiveWeapon) return;

        if (Time.time < lastWeaponSwitchTime + weaponSwitchDelay) return;

        if (Input.GetButtonDown("Fire1") && Time.time >= lastShotTime + fireCooldown)
        {
            ShootGrenade();
            lastShotTime = Time.time; // Update the last shot time
        }
    }

    public void SetActiveWeapon(bool active)
    {
        if (active)
        {
            isActiveWeapon = true;
            lastWeaponSwitchTime = Time.time;
        }
        else
        {
            isActiveWeapon = false;
        }
    }

    public void SetLastShotTime(float time)
    {
        lastShotTime = time;
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
}
