using UnityEngine;
using Photon.Pun;

public class DiscShooter : MonoBehaviourPunCallbacks
{
    public GameObject discPrefab; // Prefab dysku
    public Transform shootingPoint; // Punkt, z którego będą wystrzeliwane dyski
    public float discSpeed = 60f; // Szybkość pocisku

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            if (PhotonNetwork.InRoom)
            {
                ShootDisc();
            }
            else
            {
                Debug.LogError("Cannot instantiate before the client joined/created a room.");
            }
        }
        Debug.DrawRay(shootingPoint.position, shootingPoint.forward * 10, Color.red, 2.0f);
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
    }
}
