using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using UnityEngine.Audio;
using System.Collections;


public class Disc : MonoBehaviourPunCallbacks, IPunObservable
{
    public GameObject explosionEffect; // Explosion effect
    public float blastRadius = 15f;
    public float explosionForce = 500f;
    public float maxDamage = 100f; // Maksymalne obrażenia
    public AudioClip explosionSound; // Dźwięk eksplozji
    private AudioSource audioSource;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
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
    private Collider discCollider; // Deklaracja globalna

void Start()
{
    networkedPosition = transform.position;
    networkedRotation = transform.rotation;

    discCollider = GetComponent<Collider>();
    ownerCollider = FindOwnerCollider();

    if (ownerCollider != null && discCollider != null)
    {
        // Wywołanie MaintainIgnoreCollision z odpowiednimi argumentami
        StartCoroutine(MaintainIgnoreCollision(discCollider, ownerCollider));
    }
    else
    {
        Debug.LogWarning("Either ownerCollider or discCollider is null.");
    }

    // Get the PhotonView component
    PhotonView photonView = GetComponent<PhotonView>();

    // Get the owner of the projectile
    owner = photonView.Owner;
    Debug.Log("Projectile created by player: " + owner.NickName);

    // Ignorowanie kolizji
    IgnoreOwnerCollision();
}
Collider FindOwnerCollider()
{
    // Znajdź PlayerController właściciela
    PlayerController playerController = FindObjectsOfType<PlayerController>()
        .FirstOrDefault(p => p.photonView != null && p.photonView.Owner == GetComponent<PhotonView>().Owner);

    if (playerController != null)
    {
        Debug.Log("Owner collider found for player: " + playerController.photonView.Owner.NickName);
        return playerController.GetComponent<Collider>();
    }
    else
    {
        Debug.LogWarning("PlayerController not found for owner.");
        return null;
    }
}
IEnumerator ResetIgnoreCollision(Collider discCollider, Collider ownerCollider, float delay)
{
    yield return new WaitForSeconds(delay);
    if (discCollider != null && ownerCollider != null)
    {
        Physics.IgnoreCollision(discCollider, ownerCollider, false);
        Debug.Log("Collision re-enabled between disc and owner.");
    }
    else
    {
        Debug.LogWarning("Colliders are null while trying to re-enable collision.");
    }
}
IEnumerator EnableColliderAfterDelay(Collider collider, float delay)
{
    yield return new WaitForSeconds(delay);
    if (collider != null)
    {
        collider.enabled = true;
        Debug.Log("Collider pocisku został włączony.");
    }
}

void IgnoreOwnerCollision()
{
    // Znajdź właściciela na podstawie Photona
    PlayerController playerController = FindObjectsOfType<PlayerController>()
        .FirstOrDefault(p => p.photonView != null && p.photonView.Owner == owner);

    if (playerController != null)
    {
        ownerCollider = playerController.GetComponent<Collider>();
        if (ownerCollider != null)
        {
            Debug.Log("Ignoring collision with owner: " + owner.NickName);
            StartCoroutine(MaintainIgnoreCollision(discCollider, ownerCollider));
        }
        else
        {
            Debug.LogWarning("Collider not found for owner: " + owner.NickName);
        }
    }
    else
    {
        Debug.LogError($"PlayerController not found for owner: {owner.NickName}");
        Debug.Log($"Active PlayerControllers: {FindObjectsOfType<PlayerController>().Length}");
    }
}

IEnumerator MaintainIgnoreCollision(Collider discCollider, Collider ownerCollider)
{
    Debug.Log("MaintainIgnoreCollision started.");
    float timer = 0f;

    while (timer < 0.5f) // Czas ignorowania kolizji
    {
        if (discCollider != null && ownerCollider != null)
        {
            Physics.IgnoreCollision(discCollider, ownerCollider, true);
            Debug.Log("Ignoring collision between disc and owner.");
        }
        else
        {
            Debug.LogWarning("Colliders are null during MaintainIgnoreCollision.");
            yield break;
        }

        timer += Time.deltaTime;
        yield return null;
    }

    // Po upływie czasu przywróć kolizję
    if (discCollider != null && ownerCollider != null)
    {
        Physics.IgnoreCollision(discCollider, ownerCollider, false);
        Debug.Log("Collision re-enabled between disc and owner.");
    }
}



public void OnPhotonInstantiate(PhotonMessageInfo info)
{
    Debug.Log("OnPhotonInstantiate called.");
    if (info.Sender != null)
    {
        Debug.Log("Shooter = " + info.Sender.NickName);
    }
    else
    {
        Debug.LogWarning("Info.Sender is null.");
    }
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
