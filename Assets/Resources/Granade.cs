using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class Grenade : MonoBehaviourPunCallbacks
{
    public GameObject explosionEffect; // Efekt eksplozji
    public float blastRadius = 5f; // Promień wybuchu
    public float explosionForce = 700f; // Siła wybuchu
    public float explosionDelay = 5f; // Opóźnienie przed wybuchem granatu, jeśli nic nie dotknął
    public float collisionExplosionDelay = 3f; // Opóźnienie przed wybuchem granatu po kolizji
    public float speedThreshold = 100f; // Prędkość granatu, powyżej której wybucha natychmiast
    public float maxDamage = 100f; // Maksymalne obrażenia granatu
    public AudioClip explosionSound; // Dźwięk eksplozji
    public AudioClip bounceSound; // Dźwięk odbicia

    public float explosionSoundRange = 20f; // Zasięg słyszalności dźwięku eksplozji
    public float bounceSoundRange = 10f; // Zasięg słyszalności dźwięku odbicia
    public float minDistance = 1f; // Minimalna odległość dla pełnej głośności
    [SerializeField] private float ignoreCollisionTime = 0.2f; // Czas ignorowania kolizji z graczem

    private bool hasExploded = false; // Flaga, aby upewnić się, że wybuch jest synchronizowany tylko raz
    private float timeSinceLaunch;
    private Player owner;

    void Start()
    {
        Debug.Log("Grenade instantiated, will explode in " + explosionDelay + " seconds if nothing happens.");
        timeSinceLaunch = Time.time;
        Invoke("Explode", explosionDelay); // Ustawienie wybuchu po 5 sekundach

        // Get the PhotonView component
        PhotonView photonView = GetComponent<PhotonView>();

        // Get the owner of the grenade
        owner = photonView.Owner;
        Debug.Log("Grenade created by player: " + owner.NickName);

        // Temporarily ignore collisions with the owner
        PlayerController playerController = FindObjectsOfType<PlayerController>().FirstOrDefault(p => p.photonView.Owner == owner);
        if (playerController != null)
        {
            Collider ownerCollider = playerController.GetComponent<Collider>();
            if (ownerCollider != null)
            {
                Physics.IgnoreCollision(GetComponent<Collider>(), ownerCollider, true);
                Invoke("ResetCollision", ignoreCollisionTime); // Ignore collisions for the specified time
            }
        }
    }

    void ResetCollision()
    {
        PlayerController playerController = FindObjectsOfType<PlayerController>().FirstOrDefault(p => p.photonView.Owner == owner);
        if (playerController != null)
        {
            Collider ownerCollider = playerController.GetComponent<Collider>();
            if (ownerCollider != null)
            {
                Physics.IgnoreCollision(GetComponent<Collider>(), ownerCollider, false);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        PlayBounceSound();

        Debug.Log("Grenade collided with " + collision.gameObject.name + " at time: " + (Time.time - timeSinceLaunch) + " seconds.");

        // Check if the grenade collided with a player
        if (collision.gameObject.GetComponent<PlayerController>() != null)
        {
            Debug.Log("Grenade hit a player, exploding immediately.");
            Explode();
            return;
        }

        if (!hasExploded)
        {
            CancelInvoke("Explode");

            float grenadeSpeed = GetComponent<Rigidbody>().velocity.magnitude;
            Debug.Log("Grenade speed: " + grenadeSpeed);

            if (grenadeSpeed > speedThreshold)
            {
                Debug.Log("Grenade speed > " + speedThreshold + " units, grenade will explode immediately.");
                Explode();
            }
            else
            {
                float timeSinceCollision = Time.time - timeSinceLaunch;
                float remainingTime = collisionExplosionDelay - timeSinceCollision;

                if (remainingTime <= 0)
                {
                    Debug.Log("Time since launch is more than " + collisionExplosionDelay + " seconds, grenade will explode immediately.");
                    Explode();
                }
                else
                {
                    Debug.Log("Grenade collided, will explode in " + remainingTime + " seconds.");
                    Invoke("Explode", remainingTime);
                }
            }
        }
    }

    void Explode()
    {
        if (!hasExploded)
        {
            hasExploded = true;
            Debug.Log("Grenade exploded.");
            photonView.RPC("RPC_Explode", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_Explode()
    {
        // Tworzymy efekt eksplozji w miejscu zderzenia
        GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
        explosion.transform.localScale = new Vector3(2f, 2f, 2f); // Set the scale to 2,2,2

        // Usuwamy efekt eksplozji po 2 sekundach
        Destroy(explosion, 2f);

        // Odtwarzamy dźwięk eksplozji
        PlayExplosionSound();

        // Aplikujemy siłę eksplozji do obiektów w pobliżu
        Collider[] colliders = Physics.OverlapSphere(transform.position, blastRadius);
        foreach (var nearbyObject in colliders)
        {
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 explosionDirection = (nearbyObject.transform.position - transform.position).normalized;
                rb.AddExplosionForce(explosionForce, transform.position, blastRadius);
            }

            // Apply damage to player if applicable
            PlayerController player = nearbyObject.GetComponent<PlayerController>();
            if (player != null)
            {
                float damage = CalculateDamage(nearbyObject.transform.position);
                player.photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage, owner);
            }
        }

        // Zniszcz ten granat na wszystkich klientach lokalnie
        Destroy(gameObject);
    }

    void PlayExplosionSound()
    {
        if (explosionSound != null)
        {
            GameObject soundObject = new GameObject("ExplosionSound");
            soundObject.transform.position = transform.position; // Ustawienie pozycji obiektu dźwiękowego
            AudioSource audioSource = soundObject.AddComponent<AudioSource>();
            audioSource.clip = explosionSound;
            audioSource.spatialBlend = 1.0f; // Ensure the sound is 3D
            audioSource.maxDistance = explosionSoundRange; // Ustawienie zasięgu słyszalności
            audioSource.minDistance = minDistance; // Ustawienie minimalnej odległości dla pełnej głośności
            audioSource.Play();

            // Destroy the sound object after the clip finishes playing
            Destroy(soundObject, explosionSound.length);
        }
        else
        {
            Debug.LogError("Explosion sound not assigned.");
        }
    }

    void PlayBounceSound()
    {
        if (bounceSound != null)
        {
            GameObject bounceSoundObject = new GameObject("BounceSound");
            bounceSoundObject.transform.position = transform.position; // Ustawienie pozycji obiektu dźwiękowego
            AudioSource bounceAudioSource = bounceSoundObject.AddComponent<AudioSource>();
            bounceAudioSource.clip = bounceSound;
            bounceAudioSource.spatialBlend = 1.0f; // Ensure the sound is 3D
            bounceAudioSource.maxDistance = bounceSoundRange; // Ustawienie zasięgu słyszalności
            bounceAudioSource.minDistance = minDistance; // Ustawienie minimalnej odległości dla pełnej głośności
            bounceAudioSource.Play();

            // Destroy the sound object after the clip finishes playing
            Destroy(bounceSoundObject, bounceSound.length);
        }
        else
        {
            Debug.LogError("Bounce sound not assigned.");
        }
    }

    float CalculateDamage(Vector3 targetPosition)
    {
        float explosionDistance = Vector3.Distance(transform.position, targetPosition);
        float damage = Mathf.Clamp(maxDamage * (1 - explosionDistance / blastRadius), 1f, maxDamage);
        return damage;
    }
}
