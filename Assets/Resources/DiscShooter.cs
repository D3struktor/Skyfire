using UnityEngine;
using Photon.Pun;

public class DiscShooter : MonoBehaviourPunCallbacks
{
    public GameObject discPrefab; // Prefab dysku
    public Transform shootingPoint; // Punkt, z którego będą wystrzeliwane dyski
    public float discSpeed = 60f; // Szybkość pocisku

    private bool isActiveWeapon = false;

    void Update()
    {
        if (!isActiveWeapon) return;

        Debug.Log("DiscShooter is active");

        if (Input.GetButtonDown("Fire1"))
        {
            Debug.Log("Fire1 button pressed");

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

    public void SetActiveWeapon(bool active)
    {
        isActiveWeapon = active;
        Debug.Log("SetActiveWeapon called with value: " + active);
    }

    void ShootDisc()
    {
        Debug.Log("ShootDisc called");

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
            Debug.Log("Disc instantiated and velocity set");
        }
    }
}
