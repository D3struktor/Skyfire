using UnityEngine;
using Photon.Pun;

public class Grenade : MonoBehaviourPunCallbacks
{
    public GameObject explosionEffect; // Explosion effect prefab
    public float blastRadius = 5f; // Radius of the explosion
    public float explosionForce = 700f; // Force of the explosion
    public float explosionDelay = 2f; // Delay before the grenade explodes

    void Start()
    {
        Invoke("Explode", explosionDelay);
    }

    void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    void Explode()
    {
        if (photonView.IsMine)
        {
            photonView.RPC("RPC_Explode", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_Explode()
    {
        Instantiate(explosionEffect, transform.position, Quaternion.identity);

        Collider[] colliders = Physics.OverlapSphere(transform.position, blastRadius);
        foreach (var nearbyObject in colliders)
        {
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 explosionDirection = (nearbyObject.transform.position - transform.position).normalized;
                rb.AddExplosionForce(explosionForce, transform.position, blastRadius);
                rb.AddForce(explosionDirection * explosionForce);
            }

            // Apply damage to objects within the blast radius
            // You might have a Health component or similar to apply damage to
            // Health health = nearbyObject.GetComponent<Health>();
            // if (health != null)
            // {
            //     health.TakeDamage(damageAmount);
            // }
        }

        PhotonNetwork.Destroy(gameObject);
    }
}
