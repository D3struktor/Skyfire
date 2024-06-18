using UnityEngine;
using Photon.Pun;

public class GrenadeLauncher : MonoBehaviourPunCallbacks
{
    public GameObject grenadePrefab; // Prefab for the grenade
    public Transform shootingPoint; // The point from which grenades are shot
    public float grenadeSpeed = 20f; // Initial speed of the grenade
    public float grenadeGravity = 9.81f; // Gravity applied to the grenade
    public float grenadeDrag = 1f; // Drag for the grenade
    public float grenadeAngularDrag = 5f; // Angular drag for the grenade

    private bool isActiveWeapon = false;

    void Update()
    {
        if (!isActiveWeapon) return;

        if (Input.GetButtonDown("Fire1"))
        {
            ShootGrenade();
        }
    }

    public void SetActiveWeapon(bool active)
    {
        isActiveWeapon = active;
    }

    void ShootGrenade()
    {
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
            rb.useGravity = true;
            rb.mass = 1f; // Adjust mass as needed
            rb.drag = grenadeDrag; // Apply drag
            rb.angularDrag = grenadeAngularDrag; // Apply angular drag
        }
        else
        {
            Debug.LogError("Rigidbody component not found on grenade prefab.");
        }
    }
}
