// ✅ GrappleGun.cs – PUN + FSM + SOFT TETHER (bez teleportów)
// LPM: strzał → trzymanie = skracanie → puszczenie = odczep

using UnityEngine;
using Photon.Pun;
using System.Collections;

[RequireComponent(typeof(PhotonView))]
public class GrappleGun : MonoBehaviourPun
{
    [Header("Refs")]
    public GameObject hookPrefab;          // prefab: PhotonView + Rigidbody + Collider (+ PhotonRigidbodyView/TransformView)
    public Transform firePoint;            // u ownera: FP socket; u zdalnych: np. ręka 3P
    public LineRenderer lineRenderer;      // rope FX (działa też u zdalnych)
    public Material ropeMaterial;
    public Rigidbody playerRb;             // RB gracza (autorytet tylko u ownera)

    [Header("Hook Params")]
    public float hookSpeed = 200f;
    public float maxHookDistance = 80f;    // zasięg haka

    [Header("Timing")]
    public float reelInDelay = 0.50f;      // opóźnienie zanim wolno skracać linkę
    public float releaseDelay = 0.15f;     // drobny delay na odklejenie (korutyny)

    [Header("Tether Tuning")]
    public float autoTensionAccel = 6f;    // delikatne, stałe przyciąganie gdy jest luz
    public float holdShortenSpeed = 6f;    // tempo skracania liny przy trzymaniu (m/s)
    public float slackRatio = 1.02f;       // startowy luz względem dystansu po zaczepieniu (np. 2%)
    public float tautEpsilon = 0.05f;      // próg uznania „lina napięta”
    public bool horizontalTensionOnGroundHook = true; // gdy hak niżej, ciągnij raczej horyzontalnie

    [Header("Audio")]
    public AudioClip hookFailSound;
    public AudioClip hookDetachSound;
    public AudioSource audioSource;

    // --- runtime ---
    private GameObject currentHook;
    private SpringJoint joint;
    private Vector3 hookPoint;
    private Camera cam;

    private bool canReelIn = false;        // po reelInDelay
    private bool pulling = false;          // trzymanie LPM = skracanie

    // Rope FX dla zdalnych (bez fizyki)
    private bool ropeActiveRemote = false;
    private Vector3 remoteHookPoint;

    private enum GrappleState { Idle, Fired, Latched, Pulling, Released }
    private GrappleState currentState = GrappleState.Idle;

    void Start()
    {
        if (playerRb == null) playerRb = GetComponentInParent<Rigidbody>();
        cam = Camera.main;

        // bezpieczna konfiguracja RB
        if (playerRb != null)
        {
            playerRb.interpolation = RigidbodyInterpolation.Interpolate;
            playerRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            playerRb.maxAngularVelocity = 50f;
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
            lineRenderer.textureMode = LineTextureMode.Tile;
            if (ropeMaterial) lineRenderer.material = ropeMaterial;
        }
    }

void Update()
{
    // zdalni – tylko rope FX
    if (!photonView.IsMine)
    {
        if (ropeActiveRemote && lineRenderer != null && firePoint != null)
            DrawRope(firePoint.position, remoteHookPoint);
        return;
    }

    switch (currentState)
    {
        case GrappleState.Idle:
            if (Input.GetMouseButtonDown(0))
                FireHook();
            break;

        case GrappleState.Fired:
            // nic – czekamy na OnHookLatched
            break;

        case GrappleState.Latched:
            // priorytet: szybki klik = odczep
            if (Input.GetMouseButtonDown(0))
            {
                pulling = false;
                ReleaseLocal();
                break;
            }

            // trzymanie = skracanie (bez odczepu)
            if (Input.GetMouseButton(0))
            {
                pulling = true;
                if (canReelIn) currentState = GrappleState.Pulling;
            }
            else
            {
                pulling = false; // tylko przestajemy skracać
            }
            break;

        case GrappleState.Pulling:
            // puszczenie LPM = przestań skracać, ale zostań zaczepiony
            if (!Input.GetMouseButton(0))
            {
                pulling = false;
                currentState = GrappleState.Latched;
            }
            // brak innych akcji – „pulling nic”
            break;

        case GrappleState.Released:
            pulling = false;
            break;
    }
}


    void FixedUpdate()
    {
        if (!photonView.IsMine) return;
        if (joint == null) return;
        if (currentState != GrappleState.Latched && currentState != GrappleState.Pulling) return;

        Vector3 toHook = hookPoint - playerRb.position;
        float dist = toHook.magnitude;
        if (dist < 1e-4f) return;

        Vector3 dir = toHook / dist;

        // === SOFT TETHER (sprężyna + tłumienie), bez teleportów ===

        // [A] Sprężyna + tłumienie tylko gdy przekroczony „nominal” (maxDistance)
        if (dist > joint.maxDistance)
        {
            float extension = dist - joint.maxDistance;

            // k: twardość linki (80–200), c: tłumienie krytyczne
            float k = 120f;
            float m = playerRb.mass;
            float c = 2f * Mathf.Sqrt(k * m);

            // v>0: do haka, v<0: od haka – tłumimy tylko od haka
            float v = Vector3.Dot(playerRb.velocity, dir);
            float accel = (k * extension + Mathf.Max(-v, 0f) * c) / m;
            accel = Mathf.Clamp(accel, 0f, 160f);

            playerRb.AddForce(dir * accel, ForceMode.Acceleration); // przyciąga DO haka
        }

        // [B] Lekkie auto-napięcie gdy jest luz (dla „żyjącej” liny)
        if (dist < joint.maxDistance - tautEpsilon)
        {
            Vector3 tensionDir = dir;
            if (horizontalTensionOnGroundHook && hookPoint.y < playerRb.position.y - 0.2f)
            {
                Vector3 flat = Vector3.ProjectOnPlane(dir, Vector3.up);
                if (flat.sqrMagnitude > 1e-6f) tensionDir = flat.normalized;
            }
            playerRb.AddForce(tensionDir * autoTensionAccel, ForceMode.Acceleration);
        }

        // [C] Skracanie przy trzymaniu (bez turbo-ssania)
        if (pulling && canReelIn)
        {
            joint.maxDistance = Mathf.Max(joint.minDistance + 0.05f,
                                          joint.maxDistance - holdShortenSpeed * Time.fixedDeltaTime);

            // tnij tylko ruch OD haka (żeby nie „gumkowało” przy skracaniu)
            float vAway = Vector3.Dot(playerRb.velocity, dir); // <0: od haka
            if (vAway < 0f) playerRb.velocity -= dir * vAway;
        }
    }

    // --- INPUT (owner only) ---

    void FireHook()
    {
        if (hookPrefab == null) { Debug.LogError("❌ Brakuje hookPrefab!"); return; }
        if (currentHook != null) PhotonNetwork.Destroy(currentHook); // pozwala strzelić ponownie po release

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 dir = (cam != null ? cam.transform.forward : transform.forward);

        currentHook = PhotonNetwork.Instantiate(hookPrefab.name, origin, Quaternion.LookRotation(dir));

        // snappy prędkość
        var rb = currentHook.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 biased = (dir.normalized + Vector3.down * 0.15f).normalized;
            rb.velocity = biased * hookSpeed;
        }

        // ignoruj kolizje z właścicielem
        var hookCol = currentHook.GetComponent<Collider>();
        foreach (var col in GetComponentsInChildren<Collider>())
            if (hookCol && col) Physics.IgnoreCollision(hookCol, col);

        // init hook (owner id + zasięg)
        var hook = currentHook.GetComponent<GrappleHook>();
        if (hook != null) hook.Init(photonView.ViewID, maxHookDistance);

        // FX u właściciela
        if (lineRenderer != null) { lineRenderer.enabled = true; lineRenderer.positionCount = 2; }

        canReelIn = false;
        pulling = false;
        currentState = GrappleState.Fired;
        StartCoroutine(HookFailTimeout());
    }

    void ReleaseLocal()
    {
        photonView.RPC(nameof(RPC_ReleaseHook), RpcTarget.All);
        if (audioSource && hookDetachSound) audioSource.PlayOneShot(hookDetachSound);
        currentState = GrappleState.Released;
    }

    // --- EVENT od GrappleHook po RPC_Latched (wołane na każdym kliencie) ---

    public void OnHookLatched(Vector3 point)
    {
        // zapis dla rope FX u wszystkich
        remoteHookPoint = point;
        ropeActiveRemote = true;

        // zdalni – tylko FX
        if (!photonView.IsMine)
        {
            if (lineRenderer != null) { lineRenderer.enabled = true; lineRenderer.positionCount = 2; }
            return;
        }

        // owner – fizyka + opóźnienie reelingu
        hookPoint = point;
        StartCoroutine(OwnerLatchDelayAndJoint());
    }

    IEnumerator OwnerLatchDelayAndJoint()
    {
        yield return new WaitForSeconds(reelInDelay);
        canReelIn = true;
        currentState = GrappleState.Latched;

        // joint bez sprężyny, z lekkim luzem
        joint = playerRb.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = hookPoint;

        float d = Vector3.Distance(playerRb.position, hookPoint);
        joint.maxDistance = d * Mathf.Max(1.0f, slackRatio); // np. 1.02
        joint.minDistance = Mathf.Max(0.1f, d * 0.10f);

        joint.spring = 0f;
        joint.damper = 0f;
        joint.massScale = 1f;
    }

    [PunRPC]
    void RPC_ReleaseHook()
    {
        // owner niszczy hak
        if (photonView.IsMine)
        {
            if (currentHook != null)
            {
                var gh = currentHook.GetComponent<GrappleHook>();
                if (gh != null) gh.Release();              // parent=null + PhotonNetwork.Destroy
                else PhotonNetwork.Destroy(currentHook);
            }
        }

        // sprzątanie na wszystkich
        if (joint != null && photonView.IsMine) Destroy(joint);

        ropeActiveRemote = false;
        if (lineRenderer != null) { lineRenderer.positionCount = 0; lineRenderer.enabled = false; }

        canReelIn = false;
        pulling = false;
        hookPoint = Vector3.zero;
        currentHook = null;
        StopAllCoroutines();

        currentState = GrappleState.Idle; // od razu możesz strzelać znowu
    }

    // --- VISUALS ---

    void LateUpdate()
    {
        if (lineRenderer == null) return;

        if (photonView.IsMine)
        {
            if (currentHook != null)
                DrawRope(firePoint.position, currentHook.transform.position);
            else if (currentState == GrappleState.Latched || currentState == GrappleState.Pulling)
                DrawRope(firePoint.position, hookPoint);
        }
        else if (ropeActiveRemote)
        {
            DrawRope(firePoint.position, remoteHookPoint);
        }
        else
        {
            if (lineRenderer.enabled) { lineRenderer.positionCount = 0; lineRenderer.enabled = false; }
        }
    }

    void DrawRope(Vector3 a, Vector3 b)
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, a);
        lineRenderer.SetPosition(1, b);

        float ropeLength = Vector3.Distance(a, b);
        if (lineRenderer.material) lineRenderer.material.mainTextureScale = new Vector2(ropeLength, 1);
    }

    IEnumerator HookFailTimeout()
    {
        float timeout = maxHookDistance / Mathf.Max(1f, hookSpeed) + 0.5f;
        yield return new WaitForSeconds(timeout);

        if (currentState == GrappleState.Fired)
        {
            if (audioSource && hookFailSound) audioSource.PlayOneShot(hookFailSound);
            ReleaseLocal();
        }
    }
}
