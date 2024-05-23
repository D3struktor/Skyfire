using UnityEngine;

public class DiscShooter : MonoBehaviour
{
    public GameObject discPrefab; // Prefab dysku
    public Transform shootingPoint; // Punkt, z którego będą wystrzeliwane dyski
    public float discSpeed = 60f; // Szybkość pocisku

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            ShootDisc();
        }
        Debug.DrawRay(shootingPoint.position, shootingPoint.forward * 10, Color.red, 2.0f);

    }

    void ShootDisc()
    {
        GameObject disc = Instantiate(discPrefab, shootingPoint.position, shootingPoint.rotation);
        Rigidbody rb = disc.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = shootingPoint.forward * discSpeed;
        }
    }
}
