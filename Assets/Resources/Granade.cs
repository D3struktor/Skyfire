using UnityEngine;
using Photon.Pun;

public class Grenade : MonoBehaviourPunCallbacks
{
    public GameObject explosionEffect; // Efekt eksplozji
    public float blastRadius = 5f; // Promień wybuchu
    public float explosionForce = 700f; // Siła wybuchu
    public float explosionDelay = 5f; // Opóźnienie przed wybuchem granatu, jeśli nic nie dotknął
    public float collisionExplosionDelay = 3f; // Opóźnienie przed wybuchem granatu po kolizji
    public float speedThreshold = 100f; // Prędkość granatu, powyżej której wybucha natychmiast

    private bool hasExploded = false; // Flaga, aby upewnić się, że wybuch jest synchronizowany tylko raz
    private float timeSinceLaunch;

    void Start()
    {
        Debug.Log("Grenade instantiated, will explode in " + explosionDelay + " seconds if nothing happens.");
        timeSinceLaunch = Time.time;
        Invoke("Explode", explosionDelay); // Ustawienie wybuchu po 5 sekundach
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Grenade collided with " + collision.gameObject.name + " at time: " + (Time.time - timeSinceLaunch) + " seconds.");
        if (!hasExploded)
        {
            CancelInvoke("Explode");

            float grenadeSpeed = GetComponent<Rigidbody>().velocity.magnitude;
            Debug.Log("Grenade speed: " + grenadeSpeed);

            if (grenadeSpeed > speedThreshold)
            {
                Debug.Log("Grenade speed > " + speedThreshold + " units, grenade will explode immediately.");
                Explode();
            }
            else
            {
                float timeSinceCollision = Time.time - timeSinceLaunch;
                float remainingTime = collisionExplosionDelay - timeSinceCollision;

                if (remainingTime <= 0)
                {
                    Debug.Log("Time since launch is more than " + collisionExplosionDelay + " seconds, grenade will explode immediately.");
                    Explode();
                }
                else
                {
                    Debug.Log("Grenade collided, will explode in " + remainingTime + " seconds.");
                    Invoke("Explode", remainingTime);
                }
            }
        }
    }

    void Explode()
    {
        if (!hasExploded)
        {
            hasExploded = true;
            Debug.Log("Grenade exploded.");
            photonView.RPC("RPC_Explode", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_Explode()
    {
        // Tworzymy efekt eksplozji w miejscu zderzenia
        GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // Usuwamy efekt eksplozji po 2 sekundach
        Destroy(explosion, 2f);

        // Aplikujemy siłę eksplozji do obiektów w pobliżu
        Collider[] colliders = Physics.OverlapSphere(transform.position, blastRadius);
        foreach (var nearbyObject in colliders)
        {
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 explosionDirection = (nearbyObject.transform.position - transform.position).normalized;
                rb.AddExplosionForce(explosionForce, transform.position, blastRadius);
            }
        }

        // Zniszcz ten granat na wszystkich klientach lokalnie
        Destroy(gameObject);
    }
}
