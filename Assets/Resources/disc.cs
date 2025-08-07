using UnityEngine;
using Photon.Pun;
using System.Collections;
using UnityEngine.Audio;  
using Photon.Realtime;
using System.Linq;   


public class Disc : MonoBehaviourPunCallbacks
{
    public GameObject explosionEffect;
    public float blastRadius = 15f;
    public float explosionForce = 500f;
    public float maxDamage = 100f;
    public AudioClip explosionSound;
    public float ignoreCollisionTime = 0.2f;
    [SerializeField] public AudioMixerGroup sfxMixerGroup;
    public float soundMaxDistance = 200f;

    private Rigidbody rb;
    private bool hasExploded = false;
    private Vector3 initialVelocity;
    private Player owner;
    private Collider ownerCollider;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        owner = photonView.Owner;

        if (photonView.IsMine)
        {
            initialVelocity = rb.velocity;
            photonView.RPC("SyncInitialVelocity", RpcTarget.Others, initialVelocity);
        }
        if (PhotonNetwork.LocalPlayer == owner)
        {
            StartCoroutine(TemporarilyIgnoreOwnerCollision());
        }

    }

    private IEnumerator TemporarilyIgnoreOwnerCollision()
{
    Collider discCollider = GetComponent<Collider>();
    PlayerController playerController = FindObjectsOfType<PlayerController>()
        .FirstOrDefault(p => p.photonView.Owner == owner);

    if (playerController != null)
    {
        Collider[] playerColliders = playerController.GetComponentsInChildren<Collider>();
        foreach (var col in playerColliders)
        {
            Physics.IgnoreCollision(discCollider, col, true);
        }

        yield return new WaitForSeconds(ignoreCollisionTime);

        foreach (var col in playerColliders)
        {
            Physics.IgnoreCollision(discCollider, col, false);
        }
    }
}


    [PunRPC]
    private void SyncInitialVelocity(Vector3 velocity)
    {
        rb.velocity = velocity;
        initialVelocity = velocity;
    }

    // private Collider FindOwnerCollider()
    // {
    //     PlayerController playerController = FindObjectsOfType<PlayerController>()
    //         .FirstOrDefault(p => p.photonView.Owner == owner);

    //     return playerController?.GetComponent<Collider>();
    // }

    // IEnumerator EnableCollisionAfterDelay(Collider discCollider, Collider ownerCollider, float delay)
    // {
    //     yield return new WaitForSeconds(delay);
    //     if (discCollider && ownerCollider)
    //     {
    //         Physics.IgnoreCollision(discCollider, ownerCollider, false);
    //     }
    // }

    void Update()
    {
        if (!photonView.IsMine)
        {
            transform.position += initialVelocity * Time.deltaTime;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        hasExploded = true;

        // 🔥 **Odtwarzamy dźwięk eksplozji NATYCHMIAST po kolizji**
        PlayExplosionSound();

        // 🔥 **Synchronizujemy eksplozję na wszystkich klientach**
        photonView.RPC("RPC_Explode", RpcTarget.All, collision.contacts[0].point);
    }

    [PunRPC]
    void RPC_Explode(Vector3 explosionPosition)
    {
        transform.position = explosionPosition;

        // Efekt wybuchu
        GameObject explosion = Instantiate(explosionEffect, explosionPosition, Quaternion.identity);
        Destroy(explosion, 2f);

        // Obrażenia i siła wybuchu
        Collider[] colliders = Physics.OverlapSphere(explosionPosition, blastRadius);
        foreach (var obj in colliders)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, explosionPosition, blastRadius);
            }

            PlayerController player = obj.GetComponent<PlayerController>();
            if (player != null)
            {
                float damage = CalculateDamage(obj.transform.position, explosionPosition, player);
                player.photonView.RPC("RPC_TakeDamage", player.photonView.Owner, damage, owner);

                ApplyExplosionForce(player, explosionPosition);
            }
        }

        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    float CalculateDamage(Vector3 targetPosition, Vector3 explosionPosition, PlayerController player)
    {
        float explosionDistance = Vector3.Distance(explosionPosition, targetPosition);

        if (explosionDistance >= blastRadius)
            return 0f;

        // Sprawdzenie LOS (czy jest przeszkoda)
        if (Physics.Linecast(explosionPosition, targetPosition, out RaycastHit hit))
        {
            if (hit.collider.gameObject != player.gameObject)
            {
                return Mathf.Max(maxDamage * 0.3f, 5f);
            }
        }

        if (explosionDistance < blastRadius * 0.1f)
            return maxDamage;

        return Mathf.Max(maxDamage * (1 - explosionDistance / blastRadius), 5f);
    }

    void ApplyExplosionForce(PlayerController player, Vector3 explosionPosition)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 explosionDirection = (player.transform.position - explosionPosition).normalized;
        float forceMultiplier = 1 - (Vector3.Distance(player.transform.position, explosionPosition) / blastRadius);

        rb.AddForce(explosionDirection * explosionForce * forceMultiplier * 2f, ForceMode.Impulse);
    }

    // 🔥 **Twoja wersja PlayExplosionSound**
    void PlayExplosionSound()
    {
        if (explosionSound != null)
        {
            GameObject soundObject = new GameObject("ExplosionSound");
            AudioSource audioSource = soundObject.AddComponent<AudioSource>();
            audioSource.clip = explosionSound;
            audioSource.spatialBlend = 1.0f; // Ustawienie dźwięku na 3D
            audioSource.maxDistance = soundMaxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.transform.position = transform.position;

            // Ustawienie grupy miksera dla AudioSource
            audioSource.outputAudioMixerGroup = sfxMixerGroup;

            audioSource.Play();

            // Zniszcz obiekt dźwiękowy po zakończeniu odtwarzania
            Destroy(soundObject, explosionSound.length);
        }
        else
        {
            Debug.LogError("Explosion sound not assigned.");
        }
    }
}
