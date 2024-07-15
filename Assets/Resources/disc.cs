using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Disc : MonoBehaviourPunCallbacks, IPunObservable
{
    public GameObject explosionEffect; // Explosion effect
    public float blastRadius = 15f;
    public float explosionForce = 500f;
    public float maxDamage = 100f; // Maksymalne obrażenia

    private Vector3 networkedPosition;
    private Quaternion networkedRotation;
    private float distance;
    private float angle;
    private bool hasExploded = false; // To ensure explosion happens only once

    private Player owner; 

    void Start()
    {
        networkedPosition = transform.position;
        networkedRotation = transform.rotation;
        
        // Get the PhotonView component
        PhotonView photonView = GetComponent<PhotonView>();

        // Get the owner of the projectile
        owner = photonView.Owner;
        Debug.Log("Projectile created by player: " + owner.NickName);
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

        // Trigger the explosion on all clients
        photonView.RPC("RPC_Explode", RpcTarget.All);
    }

    [PunRPC]
    void RPC_Explode()
    {
        // Create explosion effect
        GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // Destroy the explosion effect after 2 seconds
        Destroy(explosion, 2f);

        // Apply explosion force to nearby objects
        Collider[] colliders = Physics.OverlapSphere(transform.position, blastRadius);
        foreach (var nearbyObject in colliders)
        {
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Calculate direction from explosion center to the object
                Vector3 explosionDirection = (nearbyObject.transform.position - transform.position).normalized;
                rb.AddExplosionForce(explosionForce, transform.position, blastRadius);
                // Optional: Apply additional force in the direction of the explosion
                rb.AddForce(explosionDirection * explosionForce);

                // Apply damage to player if applicable
                PlayerController player = nearbyObject.GetComponent<PlayerController>();
                if (player != null)
                {
                    float damage = CalculateDamage(nearbyObject.transform.position);
                    player.photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage, owner);
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

    float CalculateDamage(Vector3 targetPosition)
    {
        float explosionDistance = Vector3.Distance(transform.position, targetPosition);
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
