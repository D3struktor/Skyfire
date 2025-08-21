using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using UnityEngine.Audio;
using System.Collections;

public class Grenade : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{
    [Header("Effects and Audio")]
    public GameObject explosionEffect;
    public AudioClip explosionSound;
    public AudioClip bounceSound;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    public float explosionSoundRange = 200f;
    public float bounceSoundRange = 50f;
    public float minDistance = 1f;

    [Header("Explosion and Damage")]
    public float blastRadius = 8f;
    public float explosionForce = 900f;
    public float maxDamage = 100f;

    [Header("Tribes-style Timing")]
    public float armTime = 0.25f;                 // arming time – before this it won't detonate on impact
    public float airFuseTime = 2.3f;              // air fuse if nothing is hit
    public float impactFuseMin = 0.8f;            // short fuse after the first bounce
    public float impactFuseMax = 1.2f;
    public float highSpeedDetonate = 25f;         // fast detonation on very high impact speed (after arming)

    [Header("Owner Protection")]
    [SerializeField] private float ignoreCollisionTime = 0.2f; // ignore collisions with the owner
    public float ownerProtectTime = 0.2f;                       // no damage to the owner shortly after spawn

    [Header("Trajectory / Physics")]
    public bool useCustomGravity = false;
    public Vector3 customGravity = new Vector3(0, -9.81f, 0);
    public float spinRandomTorque = 3f;           // light random spin

    private Rigidbody rb;
    private bool hasExploded = false;
    private float spawnTime;
    private float firstCollisionTime = -1f;

    private Player owner;
    private int ownerActorNumber = -1;

    // data from InstantiationData
    private Vector3 initialVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;

        // Bouncy physics material for a Tribes-like bounce (if no material is assigned)
        var col = GetComponent<Collider>();
        if (col != null && (col.material == null || col.material.bounciness < 0.4f))
        {
            var mat = new PhysicMaterial("GrenadeBounce")
            {
                bounciness = 0.55f,
                bounceCombine = PhysicMaterialCombine.Maximum,
                frictionCombine = PhysicMaterialCombine.Minimum,
                dynamicFriction = 0f,
                staticFriction = 0f
            };
            col.material = mat;
        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // Read starting velocity and owner from InstantiationData (set in the launcher)
        var data = photonView.InstantiationData;
        if (data != null && data.Length >= 2)
        {
            initialVelocity = (Vector3)data[0];
            ownerActorNumber = (int)data[1];
        }

        owner = PhotonNetwork.CurrentRoom != null && ownerActorNumber != -1
            ? PhotonNetwork.CurrentRoom.GetPlayer(ownerActorNumber)
            : photonView.Owner;

        // Set initial velocity and spin on all clients
        if (rb != null)
        {
            rb.velocity = initialVelocity;
            rb.AddTorque(Random.insideUnitSphere * spinRandomTorque, ForceMode.VelocityChange);
        }

        // Initial air fuse
        Invoke(nameof(Explode), airFuseTime);

        // Briefly ignore collisions with the owner locally for them
        if (PhotonNetwork.LocalPlayer == owner)
            StartCoroutine(TemporarilyIgnoreOwnerCollision());
    }

    void Start()
    {
        // Fallback if InstantiationData was not received
        if (owner == null) owner = photonView.Owner;
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (useCustomGravity && rb != null)
            rb.AddForce(customGravity, ForceMode.Acceleration);
    }

    private IEnumerator TemporarilyIgnoreOwnerCollision()
    {
        Collider grenadeCollider = GetComponent<Collider>();
        var playerController = FindObjectsOfType<PlayerController>()
            .FirstOrDefault(p => p.photonView != null && p.photonView.Owner == owner);

        if (playerController != null && grenadeCollider != null)
        {
            var playerColliders = playerController.GetComponentsInChildren<Collider>();
            foreach (var c in playerColliders) Physics.IgnoreCollision(grenadeCollider, c, true);
            yield return new WaitForSeconds(ignoreCollisionTime);
            foreach (var c in playerColliders) Physics.IgnoreCollision(grenadeCollider, c, false);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        PlayBounceSound();

        float t = Time.time - spawnTime;

        // Did we hit a player?
        var hitPlayer = collision.collider.GetComponentInParent<PlayerController>();
        if (hitPlayer != null)
        {
            // if it is the owner and still in the protection window – do not detonate
            if (hitPlayer.photonView != null &&
                hitPlayer.photonView.Owner != null &&
                hitPlayer.photonView.Owner == owner &&
                t < ownerProtectTime)
            {
                return;
            }

            // Detonate on a player only after arming
            if (t >= armTime)
            {
                Explode();
                return;
            }
        }

        // Detonate immediately on very fast impact (after arming)
        if (rb != null && rb.velocity.magnitude >= highSpeedDetonate && t >= armTime)
        {
            Explode();
            return;
        }

        // After the first bounce set a short fuse and cancel the air fuse
        if (firstCollisionTime < 0f)
        {
            firstCollisionTime = Time.time;
            CancelInvoke(nameof(Explode));
            float fuse = Random.Range(impactFuseMin, impactFuseMax);
            Invoke(nameof(Explode), fuse);
        }
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        photonView.RPC(nameof(RPC_Explode), RpcTarget.All);
    }

    [PunRPC]
    void RPC_Explode()
    {
        // visual effect
        if (explosionEffect)
        {
            var explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(explosion, 2f);
        }

        PlayExplosionSound();

        // force and damage
        Collider[] colliders = Physics.OverlapSphere(transform.position, blastRadius);
        foreach (var col in colliders)
        {
            var body = col.attachedRigidbody;
            if (body != null)
                body.AddExplosionForce(explosionForce, transform.position, blastRadius, 0f, ForceMode.Impulse);

            var player = col.GetComponentInParent<PlayerController>();
            if (player != null && player.photonView != null)
            {
                // protect the owner for a fraction of a second
                bool isOwner = (owner != null && player.photonView.Owner == owner);
                if (isOwner && Time.time - spawnTime < ownerProtectTime) continue;

                float dmg = CalculateDamage(player.transform.position);
                player.photonView.RPC("RPC_TakeDamage", RpcTarget.All, dmg, owner);
            }
        }

        // proper destruction of the network object
        if (photonView.IsMine) PhotonNetwork.Destroy(gameObject);
        else Destroy(gameObject);
    }

    void PlayExplosionSound()
    {
        if (explosionSound == null) return;

        GameObject go = new GameObject("ExplosionSound");
        go.transform.position = transform.position;

        var src = go.AddComponent<AudioSource>();
        src.clip = explosionSound;
        src.spatialBlend = 1f;
        src.maxDistance = explosionSoundRange;
        src.minDistance = minDistance;
        src.outputAudioMixerGroup = sfxMixerGroup;
        src.Play();

        Destroy(go, explosionSound.length);
    }

    void PlayBounceSound()
    {
        if (bounceSound == null) return;

        GameObject go = new GameObject("BounceSound");
        go.transform.position = transform.position;

        var src = go.AddComponent<AudioSource>();
        src.clip = bounceSound;
        src.spatialBlend = 1f;
        src.maxDistance = bounceSoundRange;
        src.minDistance = minDistance;
        src.outputAudioMixerGroup = sfxMixerGroup;
        src.Play();

        Destroy(go, bounceSound.length);
    }

    float CalculateDamage(Vector3 targetPosition)
    {
        float d = Vector3.Distance(transform.position, targetPosition);
        return Mathf.Clamp(maxDamage * (1f - d / blastRadius), 1f, maxDamage);
    }
}
