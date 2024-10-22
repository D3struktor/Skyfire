using UnityEngine;
using Photon.Pun;
using System.Linq;
using Photon.Realtime;

public class Bullet : MonoBehaviourPunCallbacks
{
    public float damage = 10f; // Damage dealt by the bullet
    [SerializeField] private float ignoreCollisionTime = 0.2f; // Time to ignore collisions with the shooter

    private Player owner;

    void Start()
    {
        // Get the owner of the bullet
        owner = photonView.Owner;
        Debug.Log("Bullet created by player: " + owner.NickName);

        // Temporarily ignore collisions with the shooter
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
        Debug.Log("OnCollisionEnter triggered"); // Debug on collision start

        if (photonView.IsMine)
        {
            Debug.Log("PhotonView is mine"); // Debug to check if PhotonView is mine

            // Debug to check if the bullet hit something
            Debug.Log("Bullet hit: " + collision.collider.name);

            // Check if the hit object has a PlayerController component
            PlayerController player = collision.collider.GetComponent<PlayerController>();
            if (player != null)
            {
                // Check if the hit player is the same as the owner of the bullet
                if (player.photonView.Owner == owner)
                {
                    // Ignore damage if it's the owner of the bullet (self-hit)
                    Debug.Log("Ignoring self-hit on player: " + player.name);
                }
                else
                {
                    // Apply damage to the hit player
                    Debug.Log("Player hit: " + player.name);
                    player.photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage, PhotonNetwork.LocalPlayer);
                }
            }
            else
            {
                // Debug if no PlayerController component was found
                Debug.Log("No PlayerController found on hit object: " + collision.collider.name);
            }

            // Destroy the bullet
            PhotonNetwork.Destroy(gameObject);
            Debug.Log("Bullet destroyed");
        }
        else
        {
            // Debug if PhotonView is not mine
            Debug.Log("PhotonView is not mine");
        }
    }
}
