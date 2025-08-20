using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using UnityEngine.Audio;
using System.Collections;

public class Grenade : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{
    [Header("Efekt i Audio")]
    public GameObject explosionEffect;
    public AudioClip explosionSound;
    public AudioClip bounceSound;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    public float explosionSoundRange = 200f;
    public float bounceSoundRange = 50f;
    public float minDistance = 1f;

    [Header("Wybuch i Obrażenia")]
    public float blastRadius = 8f;
    public float explosionForce = 900f;
    public float maxDamage = 100f;

    [Header("Timing jak w Tribes")]
    public float armTime = 0.25f;                 // czas uzbrojenia – przed nim nie detonuje na impakcie
    public float airFuseTime = 2.3f;              // bezpiecznik w powietrzu (jeśli nic nie dotknęło)
    public float impactFuseMin = 0.8f;            // krótki bezpiecznik po pierwszym odbiciu
    public float impactFuseMax = 1.2f;
    public float highSpeedDetonate = 25f;         // szybka detonacja przy bardzo szybkim impakcie (po uzbrojeniu)

    [Header("Ochrona właściciela")]
    [SerializeField] private float ignoreCollisionTime = 0.2f; // ignorowanie kolizji z właścicielem
    public float ownerProtectTime = 0.2f;                       // brak obrażeń dla właściciela po starcie

    [Header("Trajektoria / Fizyka")]
    public bool useCustomGravity = false;
    public Vector3 customGravity = new Vector3(0, -9.81f, 0);
    public float spinRandomTorque = 3f;           // lekki losowy spin

    private Rigidbody rb;
    private bool hasExploded = false;
    private float spawnTime;
    private float firstCollisionTime = -1f;

    private Player owner;
    private int ownerActorNumber = -1;

    // dane z InstantiationData
    private Vector3 initialVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;

        // Bouncy physic material dla „tribesowego” odbicia (jeśli collider nie ma przypisanego)
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
        // Odczytujemy startową prędkość i właściciela z InstantiationData (ustawione w launcherze)
        var data = photonView.InstantiationData;
        if (data != null && data.Length >= 2)
        {
            initialVelocity = (Vector3)data[0];
            ownerActorNumber = (int)data[1];
        }

        owner = PhotonNetwork.CurrentRoom != null && ownerActorNumber != -1
            ? PhotonNetwork.CurrentRoom.GetPlayer(ownerActorNumber)
            : photonView.Owner;

        // Ustaw startową prędkość i spin na wszystkich klientach
        if (rb != null)
        {
            rb.velocity = initialVelocity;
            rb.AddTorque(Random.insideUnitSphere * spinRandomTorque, ForceMode.VelocityChange);
        }

        // Startowy bezpiecznik w powietrzu
        Invoke(nameof(Explode), airFuseTime);

        // Krótko ignorujemy kolizje z właścicielem lokalnie u niego
        if (PhotonNetwork.LocalPlayer == owner)
            StartCoroutine(TemporarilyIgnoreOwnerCollision());
    }

    void Start()
    {
        // Jeżeli z jakiegoś powodu nie dostaliśmy InstantiationData (fallback)
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

        // Trafienie gracza?
        var hitPlayer = collision.collider.GetComponentInParent<PlayerController>();
        if (hitPlayer != null)
        {
            // jeśli to właściciel i wciąż w okienku ochrony – nie detonujemy
            if (hitPlayer.photonView != null &&
                hitPlayer.photonView.Owner != null &&
                hitPlayer.photonView.Owner == owner &&
                t < ownerProtectTime)
            {
                return;
            }

            // Detonacja na graczu tylko po uzbrojeniu
            if (t >= armTime)
            {
                Explode();
                return;
            }
        }

        // Detonacja natychmiast przy bardzo szybkim impakcie (po uzbrojeniu)
        if (rb != null && rb.velocity.magnitude >= highSpeedDetonate && t >= armTime)
        {
            Explode();
            return;
        }

        // Po pierwszym odbiciu ustawiamy krótki fuse i kasujemy „air fuse”
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
        // efekt
        if (explosionEffect)
        {
            var explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(explosion, 2f);
        }

        PlayExplosionSound();

        // siła i obrażenia
        Collider[] colliders = Physics.OverlapSphere(transform.position, blastRadius);
        foreach (var col in colliders)
        {
            var body = col.attachedRigidbody;
            if (body != null)
                body.AddExplosionForce(explosionForce, transform.position, blastRadius, 0f, ForceMode.Impulse);

            var player = col.GetComponentInParent<PlayerController>();
            if (player != null && player.photonView != null)
            {
                // ochrona właściciela przez ułamek sekundy
                bool isOwner = (owner != null && player.photonView.Owner == owner);
                if (isOwner && Time.time - spawnTime < ownerProtectTime) continue;

                float dmg = CalculateDamage(player.transform.position);
                player.photonView.RPC("RPC_TakeDamage", RpcTarget.All, dmg, owner);
            }
        }

        // poprawne niszczenie obiektu sieciowego
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
