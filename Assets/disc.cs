using UnityEngine;

public class Disc : MonoBehaviour
{
    public GameObject explosionEffect; // Efekt eksplozji
    public float blastRadius = 15f;
    public float explosionForce = 500f;

    void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    void Explode()
    {
        // Tworzymy efekt eksplozji w miejscu zderzenia
        Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // Aplikujemy siłę eksplozji do obiektów w pobliżu
        Collider[] colliders = Physics.OverlapSphere(transform.position, blastRadius);
        foreach (var nearbyObject in colliders)
        {
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, blastRadius);
            }
        }

        // Zniszcz ten dysk
        Destroy(gameObject);
    }
}
