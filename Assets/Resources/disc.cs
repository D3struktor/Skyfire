using UnityEngine;
using Photon.Pun;

public class Disc : MonoBehaviourPunCallbacks, IPunObservable
{
    public GameObject explosionEffect; // Efekt eksplozji
    public float blastRadius = 15f;
    public float explosionForce = 500f;

    private Vector3 networkedPosition;
    private Quaternion networkedRotation;
    private float distance;
    private float angle;

    void Start()
    {
        networkedPosition = transform.position;
        networkedRotation = transform.rotation;
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
