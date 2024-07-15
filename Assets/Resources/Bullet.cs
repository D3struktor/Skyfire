using UnityEngine;
using Photon.Pun;

public class Bullet : MonoBehaviourPunCallbacks
{
    public float damage = 10f; // Obrażenia zadawane przez pocisk

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("OnCollisionEnter triggered"); // Debug na początek kolizji

        if (photonView.IsMine)
        {
            Debug.Log("PhotonView is mine"); // Debug sprawdzający, czy PhotonView jest mój

            // Debugowanie, aby sprawdzić, czy pocisk uderzył w coś
            Debug.Log("Bullet hit: " + collision.collider.name);

            // Sprawdzenie, czy gracz ma komponent PlayerController
            PlayerController player = collision.collider.GetComponent<PlayerController>();
            if (player != null)
            {
                // Debugowanie, aby sprawdzić, czy znaleziono gracza
                Debug.Log("Player hit: " + player.name);
                player.photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage, PhotonNetwork.LocalPlayer);
            }
            else
            {
                // Debugowanie, gdy nie znaleziono komponentu PlayerController
                Debug.Log("No PlayerController found on hit object: " + collision.collider.name);
            }

            // Destroy the bullet
            PhotonNetwork.Destroy(gameObject);
            Debug.Log("Bullet destroyed");
        }
        else
        {
            // Debugowanie, gdy PhotonView nie jest mój
            Debug.Log("PhotonView is not mine");
        }
    }
}
