using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DiscShooter : MonoBehaviourPunCallbacks
{
    public GameObject discPrefab; // Prefab for the disc
    public Transform shootingPoint; // Point from which discs are shot
    public float discSpeed = 60f; // Disc speed
    public float fireCooldown = 0.7f; // Cooldown time between shots
    public AudioClip shootSound; // Sound clip to play when shooting

    private bool isActiveWeapon = false;
    private float lastShotTime = 0f; // Time when the last shot was fired
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

        if (Input.GetButtonDown("Fire1") && Time.time - lastShotTime >= fireCooldown)
        {
            if (PhotonNetwork.InRoom)
            {
                ShootDisc();
                lastShotTime = Time.time; // Update the last shot time
            }
            else
            {
                Debug.LogError("Cannot instantiate before the client joined/created a room.");
            }
        }
        Debug.DrawRay(shootingPoint.position, shootingPoint.forward * 10, Color.red);
    }

    public void SetActiveWeapon(bool active)
    {
        isActiveWeapon = active;
    }

    public void SetLastShotTime(float time)
    {
        lastShotTime = time;
    }

    void ShootDisc()
    {
        if (discPrefab == null || shootingPoint == null)
        {
            Debug.LogError("Disc prefab or shooting point is not assigned.");
            return;
        }

        GameObject disc = PhotonNetwork.Instantiate(discPrefab.name, shootingPoint.position, shootingPoint.rotation);

        Rigidbody rb = disc.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = shootingPoint.forward * discSpeed;
        }
        else
        {
            Debug.LogError("Rigidbody component not found on disc prefab.");
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
