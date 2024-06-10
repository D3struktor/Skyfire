using UnityEngine;
using Photon.Pun;

public class GrenadeLauncher : MonoBehaviourPunCallbacks
{
    public GameObject grenadePrefab; // Prefab for the grenade
    public Transform shootingPoint; // The point from which grenades are shot
    public float grenadeSpeed = 20f; // Speed of the grenade

    private bool isActiveWeapon = false;

    void Update()
    {
        if (!isActiveWeapon) return;

        Debug.Log("GrenadeLauncher is active");

        if (Input.GetButtonDown("Fire1"))
        {
            Debug.Log("Fire1 button pressed");
            ShootGrenade();
        }
    }

    public void SetActiveWeapon(bool active)
    {
        isActiveWeapon = active;
        Debug.Log("SetActiveWeapon called with value: " + active);
    }

    void ShootGrenade()
    {
        Debug.Log("ShootGrenade called");

        if (grenadePrefab == null || shootingPoint == null)
        {
            Debug.LogError("Grenade prefab or shooting point is not assigned.");
            return;
        }

        GameObject grenade = PhotonNetwork.Instantiate(grenadePrefab.name, shootingPoint.position, shootingPoint.rotation);
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = shootingPoint.forward * grenadeSpeed;
            Debug.Log("Grenade instantiated and velocity set");
        }
    }
}
