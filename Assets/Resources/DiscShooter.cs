using UnityEngine;
using Photon.Pun;

public class DiscShooter : MonoBehaviourPunCallbacks
{
    public GameObject discPrefab; // Prefab for the disc
    public Transform shootingPoint; // Point from which discs are shot
    public float discSpeed = 60f; // Disc speed

    private bool isActiveWeapon = false;

    void Update()
    {
        if (!isActiveWeapon) return;

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
        Debug.DrawRay(shootingPoint.position, shootingPoint.forward * 10, Color.red);
    }

    public void SetActiveWeapon(bool active)
    {
        isActiveWeapon = active;
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
        else
        {
            Debug.LogError("Rigidbody component not found on disc prefab.");
        }
    }
}
