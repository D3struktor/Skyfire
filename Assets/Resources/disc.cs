using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(PhotonView))]
public class Disc : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Explosion")]
    public GameObject explosionEffect;
    public float blastRadius = 15f;
    public float explosionForce = 500f;
    public float maxDamage = 100f;

    [Header("Audio")]
    public AudioClip explosionSound;
    public AudioClip flightLoopClip;                 // new: loop during flight
    [SerializeField] public UnityEngine.Audio.AudioMixerGroup sfxMixerGroup;
    public float soundMaxDistance = 200f;

    [Header("Flight loop tuning")]
    public float flightMinPitch = 0.85f;             // lowest pitch
    public float flightMaxPitch = 1.35f;             // highest pitch
    public float flightMaxSpeedReference = 80f;      // speed at which pitch ≈ max
    public float flightMinVolume = 0.08f;
    public float flightMaxVolume = 0.8f;
    public float flightFadeOutTime = 0.15f;          // fade-out before explosion
    public float dopplerLevel = 2.0f;                // Doppler effect (0–5)

    [Header("Safety windows")]
    public float ignoreCollisionTime = 0.25f;
    public float selfDamageGraceTime = 0.35f;

    [Header("Net sync (lightweight)")]
    public float viewerLerp = 0.55f;
    public float faceVelMinSpeed = 1f;

    private Rigidbody rb;
    private Collider discCollider;
    private bool hasExploded = false;

    private Player owner;
    private double spawnServerTime;

    // start snapshot
    private Vector3 startPos;
    private Vector3 startVel;
    private Quaternion startRot;

    // viewer-side state
    private Vector3 netPos;
    private Vector3 netVel;
    private double netTime;

    // owner colliders cache
    private PlayerController ownerPC;
    private Collider[] ownerColliders;

    // 🔊 flight loop
    private AudioSource flightAS;
    private bool flightFadingOut = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        discCollider = GetComponent<Collider>();
    }

    void Start()
    {
        owner = photonView.Owner;

        if (photonView.IsMine)
        {
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            startPos = transform.position;
            startRot = transform.rotation;
            startVel = rb.velocity;

            spawnServerTime = PhotonNetwork.Time;
            photonView.RPC(nameof(RPC_SetSpawnData), RpcTarget.Others, startPos, startRot, startVel, spawnServerTime);
        }
        else
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            discCollider.enabled = false;

            startPos = transform.position;
            startRot = transform.rotation;
            startVel = Vector3.zero;
            netPos = transform.position;
            netVel = Vector3.zero;
            netTime = PhotonNetwork.Time;
        }

        // start flight loop on EVERY client (spatial sound)
        SetupFlightLoop();

        // briefly ignore the owner on all clients
        StartCoroutine(TemporarilyIgnoreOwnerCollision());
    }

    void Update()
    {
        if (!photonView.IsMine && !hasExploded)
        {
            double dt = PhotonNetwork.Time - netTime;
            Vector3 predicted = netPos + netVel * (float)dt;
            transform.position = Vector3.Lerp(transform.position, predicted, viewerLerp);

            if (netVel.sqrMagnitude > faceVelMinSpeed * faceVelMinSpeed)
            {
                Quaternion face = Quaternion.LookRotation(netVel.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, face, 0.5f);
            }
        }

        // update pitch/volume based on speed
        UpdateFlightLoopParams();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!photonView.IsMine) return;
        if (hasExploded) return;
        if (IsOwnersCollider(collision.collider) && InGrace()) return;

        hasExploded = true;
        StartCoroutine(FadeOutFlightThenExplode(collision.contacts[0].point));
    }

    // ---------- Net ----------

    [PunRPC]
    void RPC_SetSpawnData(Vector3 sPos, Quaternion sRot, Vector3 sVel, double sTime)
    {
        startPos = sPos;
        startRot = sRot;
        startVel = sVel;
        spawnServerTime = sTime;

        transform.position = sPos;
        transform.rotation = sRot;

        netPos = sPos;
        netVel = sVel;
        netTime = sTime;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(rb ? rb.velocity : Vector3.zero);
            stream.SendNext(PhotonNetwork.Time);
        }
        else
        {
            netPos = (Vector3)stream.ReceiveNext();
            netVel = (Vector3)stream.ReceiveNext();
            netTime = (double)stream.ReceiveNext();
        }
    }

    // ---------- Explosion ----------

    IEnumerator FadeOutFlightThenExplode(Vector3 point)
    {
        // fade out the loop
        if (flightAS && flightAS.isPlaying && !flightFadingOut)
        {
            flightFadingOut = true;
            float t = 0f;
            float startVol = flightAS.volume;
            while (t < flightFadeOutTime)
            {
                t += Time.deltaTime;
                flightAS.volume = Mathf.Lerp(startVol, 0f, t / flightFadeOutTime);
                yield return null;
            }
            flightAS.Stop();
        }

        PlayExplosionSound();
        photonView.RPC(nameof(RPC_Explode), RpcTarget.All, point);
    }

    [PunRPC]
    void RPC_Explode(Vector3 explosionPosition)
    {
        transform.position = explosionPosition;

        if (explosionEffect)
        {
            var fx = Instantiate(explosionEffect, explosionPosition, Quaternion.identity);
            Destroy(fx, 2f);
        }

        var hits = Physics.OverlapSphere(explosionPosition, blastRadius);
        foreach (var col in hits)
        {
            if (col.attachedRigidbody != null)
                col.attachedRigidbody.AddExplosionForce(explosionForce, explosionPosition, blastRadius);

            var pc = col.GetComponent<PlayerController>();
            if (pc != null)
            {
                if (pc.photonView.Owner == owner && InGrace())
                {
                    ApplyExplosionForce(pc, explosionPosition); // rocket jump without damage
                    continue;
                }

                float dmg = CalculateDamage(col.transform.position, explosionPosition, pc);
                pc.photonView.RPC("RPC_TakeDamage", pc.photonView.Owner, dmg, owner);
                ApplyExplosionForce(pc, explosionPosition);
            }
        }

        if (photonView.IsMine)
            PhotonNetwork.Destroy(gameObject);
    }

    float CalculateDamage(Vector3 targetPos, Vector3 explosionPos, PlayerController pc)
    {
        float dist = Vector3.Distance(explosionPos, targetPos);
        if (dist >= blastRadius) return 0f;

        if (Physics.Linecast(explosionPos, targetPos, out RaycastHit hit))
        {
            if (hit.collider.gameObject != pc.gameObject)
                return Mathf.Max(maxDamage * 0.3f, 5f);
        }

        if (dist < blastRadius * 0.1f) return maxDamage;
        return Mathf.Max(maxDamage * (1f - dist / blastRadius), 5f);
    }

    void ApplyExplosionForce(PlayerController pc, Vector3 explosionPos)
    {
        var prb = pc.GetComponent<Rigidbody>();
        if (!prb) return;

        Vector3 dir = (pc.transform.position - explosionPos).normalized;
        float mul = 1f - (Vector3.Distance(pc.transform.position, explosionPos) / blastRadius);
        prb.AddForce(dir * explosionForce * mul * 2f, ForceMode.Impulse);
    }

    // ---------- Owner collision ignore ----------

    private IEnumerator TemporarilyIgnoreOwnerCollision()
    {
        ownerPC = FindObjectsOfType<PlayerController>()
                 .FirstOrDefault(p => p.photonView.Owner == owner);

        if (!ownerPC) yield break;

        ownerColliders = ownerPC.GetComponentsInChildren<Collider>();
        foreach (var c in ownerColliders)
            if (c) Physics.IgnoreCollision(discCollider, c, true);

        yield return new WaitForSeconds(ignoreCollisionTime);

        foreach (var c in ownerColliders)
            if (c) Physics.IgnoreCollision(discCollider, c, false);
    }

    private bool IsOwnersCollider(Collider col)
    {
        if (!ownerPC) return false;
        if (ownerColliders == null) ownerColliders = ownerPC.GetComponentsInChildren<Collider>();
        return ownerColliders != null && ownerColliders.Any(c => c == col);
    }

    private bool InGrace()
    {
        return PhotonNetwork.Time - spawnServerTime < selfDamageGraceTime;
    }

    // ---------- Audio helpers ----------

    void SetupFlightLoop()
    {
        if (!flightLoopClip) return;

        flightAS = gameObject.AddComponent<AudioSource>();
        flightAS.clip = flightLoopClip;
        flightAS.loop = true;
        flightAS.playOnAwake = false;

        flightAS.spatialBlend = 1f;                        // 3D
        flightAS.maxDistance = soundMaxDistance;
        flightAS.rolloffMode = AudioRolloffMode.Linear;
        flightAS.dopplerLevel = dopplerLevel;              // Doppler effect
        flightAS.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic; // use Rigidbody velocity
        flightAS.outputAudioMixerGroup = sfxMixerGroup;

        // slight random phase to avoid "chorus" phasing of many discs
        flightAS.time = Random.Range(0f, flightAS.clip.length);

        // start with minimal volume — Update sets target
        flightAS.volume = flightMinVolume;
        flightAS.pitch = flightMinPitch;
        flightAS.Play();
    }

    void UpdateFlightLoopParams()
    {
        if (!flightAS || !flightAS.isPlaying) return;

        // take real speed (rb for owner, netVel for viewers)
        float speed = photonView.IsMine ? rb.velocity.magnitude : netVel.magnitude;

        // pitch rises with speed (clamped 0..1)
        float k = Mathf.Clamp01(speed / Mathf.Max(0.001f, flightMaxSpeedReference));
        float targetPitch = Mathf.Lerp(flightMinPitch, flightMaxPitch, k);
        float targetVol   = Mathf.Lerp(flightMinVolume, flightMaxVolume, k);

        // slight smoothing
        flightAS.pitch = Mathf.Lerp(flightAS.pitch, targetPitch, 0.2f);
        flightAS.volume = Mathf.Lerp(flightAS.volume, targetVol, 0.2f);
    }

    void PlayExplosionSound()
    {
        if (!explosionSound) return;

        GameObject so = new GameObject("ExplosionSound");
        var a = so.AddComponent<AudioSource>();
        a.clip = explosionSound;
        a.spatialBlend = 1f;
        a.maxDistance = soundMaxDistance;
        a.rolloffMode = AudioRolloffMode.Linear;
        a.transform.position = transform.position;
        a.outputAudioMixerGroup = sfxMixerGroup;
        a.Play();
        Destroy(so, explosionSound.length);
    }
}
