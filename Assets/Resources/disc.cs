using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;


public class Disc : MonoBehaviourPunCallbacks, IPunObservable
{
    public GameObject explosionEffect; // Explosion effect
    public float blastRadius = 15f;
    public float explosionForce = 500f;
    public float maxDamage = 100f; // Maksymalne obrażenia
    public AudioClip explosionSound; // Dźwięk eksplozji
    public float soundMaxDistance = 200f; // Maksymalna odległość słyszalności dźwięku
    public float soundFullVolumeDistance = 100f; // Odległość, przy której dźwięk jest w 100% głośności
    public float ignoreCollisionTime = 0.2f; // Time to ignore collision with the player

    private Vector3 networkedPosition;
    private Quaternion networkedRotation;
    private float distance;
    private float angle;
    private bool hasExploded = false; // To ensure explosion happens only once

    private Player owner;
    private Collider ownerCollider;

    void Start()
    {
        networkedPosition = transform.position;
        networkedRotation = transform.rotation;

        // Get the PhotonView component
        PhotonView photonView = GetComponent<PhotonView>();

        // Get the owner of the projectile
        owner = photonView.Owner;
        Debug.Log("Projectile created by player: " + owner.NickName);

        // Find the owner's collider and temporarily ignore collision
        PlayerController playerController = FindObjectsOfType<PlayerController>().FirstOrDefault(p => p.photonView.Owner == owner);
        if (playerController != null)
        {
            ownerCollider = playerController.GetComponent<Collider>();
            if (ownerCollider != null)
            {
                Physics.IgnoreCollision(GetComponent<Collider>(), ownerCollider, true);
                Invoke("ResetCollision", ignoreCollisionTime);
            }
        }
    }

    void ResetCollision()
    {
        if (ownerCollider != null)
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), ownerCollider, false);
        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Debug.Log("Shooter = " + info.Sender);
    }

    void Update()
    {
        if (!photonView.IsMine)
        {
            // Interpolate position and rotation
            transform.position = Vector3.Lerp(transform.position, networkedPosition, Time.deltaTime * 10);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkedRotation, Time.deltaTime * 10);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return; // Prevent multiple explosions
        hasExploded = true;

        // Trigger the explosion on all clients at the exact collision point
        photonView.RPC("RPC_Explode", RpcTarget.All, collision.contacts[0].point);
    }

    [PunRPC]
    void RPC_Explode(Vector3 explosionPosition)
    {
        // Move the disc to the explosion position for consistent visuals
        transform.position = explosionPosition;

        // Create explosion effect
        GameObject explosion = Instantiate(explosionEffect, explosionPosition, Quaternion.identity);
        explosion.transform.localScale = new Vector3(2, 2, 2);

        // Destroy the explosion effect after 2 seconds
        Destroy(explosion, 2f);

        // Play explosion sound
        PlayExplosionSound();

        // Apply explosion force to nearby objects
        Collider[] colliders = Physics.OverlapSphere(explosionPosition, blastRadius);
        foreach (var nearbyObject in colliders)
        {
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Calculate direction from explosion center to the object
                Vector3 explosionDirection = (nearbyObject.transform.position - explosionPosition).normalized;
                rb.AddExplosionForce(explosionForce, explosionPosition, blastRadius);
                // Optional: Apply additional force in the direction of the explosion
                rb.AddForce(explosionDirection * explosionForce);

                // Apply damage to player if applicable
                PlayerController player = nearbyObject.GetComponent<PlayerController>();
                if (player != null)
                {
                    float damage = CalculateDamage(nearbyObject.transform.position, explosionPosition);
                    if (player.photonView.Owner == owner)
                    {
                        damage *= 0.5f; // Reduce damage by 50% for the owner
                    }
                    player.photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage, owner);

                    // Apply force to the player
                    player.GetComponent<Rigidbody>().AddForce(explosionDirection * explosionForce * 2, ForceMode.Impulse);
                }
            }
        }

        // Destroy the disc locally on all clients
        Destroy(gameObject);

        // Only the owner should try to destroy the object over the network
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    void PlayExplosionSound()
    {
        if (explosionSound != null)
        {
            GameObject soundObject = new GameObject("ExplosionSound");
            AudioSource audioSource = soundObject.AddComponent<AudioSource>();
            audioSource.clip = explosionSound;
            audioSource.spatialBlend = 1.0f; // Ensure the sound is 3D
            audioSource.maxDistance = soundMaxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.transform.position = transform.position;
            audioSource.Play();

            // Destroy the sound object after the clip finishes playing
            Destroy(soundObject, explosionSound.length);
        }
        else
        {
            Debug.LogError("Explosion sound not assigned.");
        }
    }

    float CalculateDamage(Vector3 targetPosition, Vector3 explosionPosition)
    {
        float explosionDistance = Vector3.Distance(explosionPosition, targetPosition);
        float damage = Mathf.Clamp(maxDamage * (1 - explosionDistance / blastRadius), 1f, maxDamage);
        return damage;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send data to other players
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // Receive data from other players
            networkedPosition = (Vector3)stream.ReceiveNext();
            networkedRotation = (Quaternion)stream.ReceiveNext();

            distance = Vector3.Distance(transform.position, networkedPosition);
            angle = Quaternion.Angle(transform.rotation, networkedRotation);
        }
    }
}
