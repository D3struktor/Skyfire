using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;  // Required for Hashtable

public class PlayerController : MonoBehaviourPunCallbacks
{
    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private Rigidbody rb;

    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float sensitivity = 2.0f;

    // --- JETPACK ---
    [SerializeField] private float jetpackForceY = 30.0f;
    [SerializeField] private float jetpackForceX = 15.0f;
    [SerializeField] private float jetpackForceZ = 15.0f;
    [SerializeField] private float jetpackFuelMax = 100.0f;
    [SerializeField] private float jetpackFuelRegenRate = 5.0f;
    [SerializeField] private float jetpackFuelUsageRate = 10.0f;
    private float currentJetpackFuel;
    private bool canUseJetpack = true;

    // --- UI ---
    [SerializeField] private Image jetpackFuelImage; // Image for jetpack fuel bar
    [SerializeField] GameObject ui;
    [SerializeField] private Text playerSpeedText;
    [SerializeField] private Image speedImage; // Image for speed bar
    [SerializeField] private Image healthbarImage; // Image for health bar

    // --- SLIDE / SKI ---
    private bool isSliding = false;
    private bool isColliding = false;
    Vector3 lastAirVelocity;
    bool wasGroundedLast = false;
    float skiLandingCooldown = 0f; // anty-dubel
    [SerializeField] float skiLandingCooldownTime = 0.06f;
    bool justLandedSki = false; // dodaj jako pole w klasie

    // --- Landing guard ---
    float landingGuardTimer = 0f;
    [SerializeField] float landingGuardTime = 0.08f;   // protect speed for 80 ms after contact
    [SerializeField] float landingKeepFactor = 0.90f;  // keep at least 90% of air speed
    [SerializeField] float landingMaxTurnDeg = 35f;    // max direction turn per tick



    // --- smoothing ground normal ---
    Vector3 lastGroundNormal = Vector3.up;
    [SerializeField] float groundNormalSmooth = 0.35f;   // 0..1 (higher = reacts faster)
    [SerializeField] float groundProbeRadius = 0.25f;    // SphereCast
    [SerializeField] float groundProbeSide = 0.35f;      // side samples
    [SerializeField] float slideGroundGrace = 0.20f; // how many seconds we tolerate being airborne
    float timeSinceGrounded = 0f;
    Vector3 smoothedNormal = Vector3.up;
    [SerializeField] float normalSmooth = 0.5f; // 0..1 (higher = reacts faster)



    [Header("Air Gravity")]
    [SerializeField] float airGravityMultiplier = 1.9f;   // 1.6–2.4 gives a "Tribes feel"
    [SerializeField] float airTerminalDownSpeed = 120f;   // max falling speed (m/s)
    // [SerializeField] float airDrag = 0.0f;                // obniż na 0–0.005


    [Header("Sliding/Ski")]
    [SerializeField] private KeyCode slideKey = KeyCode.LeftShift;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 1.1f;
    [SerializeField] private float skiAcceleration = 20.0f;
    [SerializeField] private float minSkiSpeed = 10.0f;
    [SerializeField] private float groundDrag = 0.1f;
    [SerializeField] private float airDrag = 0.01f;
    [SerializeField] private float slidingDrag = 0.01f;
    [SerializeField] private float endDrag = 0.1f;
    [SerializeField] private float dragTransitionTime = 0.5f;
    [SerializeField] private float slideSpeedFactor = 1.5f;
    [SerializeField] private float minSlopeAngle = 5f;

    [SerializeField] private float maxSpeed = 200f; // speed cap and UI bar

    // === SKI LANDING – PARAMETRY KRZYWEJ ===
    [Header("Ski Landing (angle -> loss curve)")]
    [Tooltip("Maximum angle (deg) to slope tangent with full speed retention.")]
    [SerializeField] private float skiMaxFullRetainAngle = 30f;     // 0% loss up to 30°
    [Tooltip("Damping of the normal component after landing (0..1).")]
    [SerializeField, Range(0f, 1f)] private float skiNormalDamping = 0.05f;
    [Tooltip("Max tangential speed loss at 90° (e.g., 0.65 = 65% loss).")]
    [SerializeField, Range(0f, 1f)] private float skiMaxLossAt90 = 0.65f;
    [SerializeField] private AnimationCurve skiLossByAngle = null;   // loss (0..1) vs angle (0..90)
    [SerializeField] private bool rebuildLossCurveOnAwake = false; // check once to reset curve on start

    // --- Weapons ---
    public GameObject primaryWeaponPrefab;  // Primary weapon prefab
    public GameObject grenadeLauncherPrefab;  // Grenade launcher prefab
    public GameObject chaingunPrefab;  // Chaingun prefab
    public GameObject grapplerPrefab;  // Grappler prefab
    private GameObject currentWeapon;
    private DiscShooter discShooter;
    private GrenadeLauncher grenadeLauncher;
    private Chaingun chaingun; // Reference to the Chaingun script
    private int weaponSlot = 1;

    private PhotonView PV;
    private Camera playerCamera;

    // --- Health / etc. ---
    const float maxHealth = 100f;
    float currentHealth = maxHealth;
    float CurrentHealth;

    PlayerManager playerManager;
    private CrosshairController crosshairController; // Reference to CrosshairController

    private float lastShotTime = 0f;
    private float fireCooldown = 0.7f;

    private float storedHeat = 0f;
    private bool isMovementEnabled = true;

    public Color randomColor;
    public Renderer playerRenderer;

    private int maxDiscShooterAmmo = 10;
    private int maxGrenadeLauncherAmmo = 15;
    private int maxChaingunAmmo = 100;

    private int currentDiscShooterAmmo;
    private int currentGrenadeLauncherAmmo;
    private int currentChaingunAmmo;

    [SerializeField] private AudioClip slideSound;
    [SerializeField] private AudioClip jetpackSound;
    [SerializeField] private AudioClip Shot;
    [SerializeField] private AudioClip energyPackSound;
    private AudioSource audioSource;

    private bool isAlive = true; // Track if the player is alive
    private AmmoUI ammoUI;

    private Coroutine restoreHealthCoroutine;

    private bool isUsingEnergyPack = false;
    private float energyPackCooldown = 10f;
    private float energyPackDuration = 1.2f;
    private float energyPackFuelCost = 80f;
    private float energyPackTimer = 0f;
    private float energyPackCooldownTimer = 0f;
    private bool canUseEnergyPack = true;

    // MODEL!!!
    public GameObject model; // Referencja do obiektu model
    private Animator anim; // Referencja do Animatora

    private float mouseX;
    private float mouseY;

    [SerializeField] private Transform headPos;
    [SerializeField] Transform Cam = null;

    public GameObject ragdollPrefab; // Przypisz prefab ragdolla w inspektorze

    // medale
    private MedalDisplay medalDisplay;

    private float timeInAir = 0f;
    private float timeOnGround = 0f;
    private float cumulativeSpeed = 0f; // Sum of all speed samples
    private int speedSamples = 0;       // Number of speed samples taken

    // === STANY POMOCNICZE DO SKI LANDINGU ===
    private bool wasGroundedLastFixed = false;

    // ================== LIFECYCLE ==================

    void Awake()
    {
        if (skiLossByAngle == null || skiLossByAngle.length == 0 || rebuildLossCurveOnAwake)
{ 
    // Default loss curve (0..1) depending on angle (0..90°):
    // 0°     -> 0   (no loss)
    // skiMaxFullRetainAngle -> 0 (still no loss up to the threshold)
    // 60°    -> ~0.35 (small cost for sharp entry)
    // 90°    -> skiMaxLossAt90 (maximum loss)
    Keyframe k0  = new Keyframe(0f, 0f, 0f, 0f);
    Keyframe kA  = new Keyframe(skiMaxFullRetainAngle, 0f, 0f, 0f);
    Keyframe k60 = new Keyframe(60f, 0.35f, 0f, 0f);
    Keyframe k90 = new Keyframe(90f, skiMaxLossAt90, 0f, 0f);

    skiLossByAngle = new AnimationCurve(k0, kA, k60, k90);
    for (int i = 0; i < skiLossByAngle.length; i++)
        skiLossByAngle.SmoothTangents(i, 0.3f);

    // disable one-time rebuild
    rebuildLossCurveOnAwake = false;
}
        rb = GetComponent<Rigidbody>();
        PV = GetComponent<PhotonView>();
        playerCamera = GetComponentInChildren<Camera>();
        playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager>();
        crosshairController = FindObjectOfType<CrosshairController>();

        // Initialize AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Fizyka
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Default loss curve: 0°→0, 30°→0, 53.1301°→0.30, 90°→0.65
        if (skiLossByAngle == null || skiLossByAngle.length == 0)
        {
            skiLossByAngle = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(skiMaxFullRetainAngle, 0f, 0f, 0f),
                new Keyframe(53.1301f, 0.30f, 0.02f, 0.02f),
                new Keyframe(90f, skiMaxLossAt90, 0f, 0f)
            );
            for (int i = 0; i < skiLossByAngle.length; i++)
                skiLossByAngle.SmoothTangents(i, 0.3f);
        }
    }

    void Start()
    {
        // Ustawienie TagObject
        PV.Owner.TagObject = this;
        Debug.Log("TagObject ustawiony dla gracza: " + PV.Owner.NickName);

        Cursor.lockState = CursorLockMode.Locked;
        currentJetpackFuel = jetpackFuelMax;

        playerRenderer = GetComponent<Renderer>();
        playerRenderer.material.color = randomColor;

        model = transform.Find("model").gameObject; // Znajdź obiekt "model" w hierarchii
        anim = model.GetComponent<Animator>(); // Pobierz Animator z obiektu model

        // Kolor na submesh
        GameObject ch44 = model.transform.Find("Ch44").gameObject;
        Renderer ch44Renderer = ch44.GetComponent<Renderer>();
        Material[] materials = ch44Renderer.materials;
        if (materials.Length > 1)
        {
            materials[1].color = randomColor;
            ch44Renderer.materials = materials;
        }

        if (!PV.IsMine)
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
            Destroy(ui);
            if (jetpackFuelImage) jetpackFuelImage.gameObject.SetActive(false);
            if (playerSpeedText) playerSpeedText.gameObject.SetActive(false);
            if (speedImage) speedImage.gameObject.SetActive(false);
            if (healthbarImage) healthbarImage.gameObject.SetActive(false);
            return;
        }
        if (photonView.IsMine)
        {
            Debug.Log("PlayerController: Setting player color for local player.");
        }

        // Initialize weapon based on current player properties
        if (PV.Owner.CustomProperties.TryGetValue("itemIndex", out object itemIndex))
        {
            EquipWeapon((int)itemIndex);
            UpdateAmmoUI();
        }
        else
        {
            EquipWeapon(weaponSlot); // Equip default weapon if no property is found
            UpdateAmmoUI();
        }

        // Znajdź komponent AmmoUI w scenie
        ammoUI = FindObjectOfType<AmmoUI>();
        if (ammoUI == null)
        {
            Debug.LogError("Nie znaleziono komponentu AmmoUI.");
        }

        // Zaktualizuj UI na starcie
        UpdateAmmoUI();

        CurrentHealth = maxHealth;
        currentDiscShooterAmmo = maxDiscShooterAmmo;
        currentGrenadeLauncherAmmo = maxGrenadeLauncherAmmo;
        currentChaingunAmmo = maxChaingunAmmo;

        medalDisplay = FindObjectOfType<MedalDisplay>();
        if (medalDisplay != null)
        {
            Debug.Log("MedalDisplay przypisane poprawnie.");
        }
        else
        {
            Debug.Log("MedalDisplay nie zostało znalezione!");
        }
    }

    void Update()
    {
    if (!GroundBelow(out _))
    {
        lastAirVelocity = rb.velocity;
    }
        TrackAirAndGroundTime(); // Update air/ground time tracking
        TrackAverageSpeed();     // Track speed for average calculation

        // Przełącz `isMovementEnabled` przy użyciu klawisza Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isMovementEnabled = !isMovementEnabled;
            Debug.Log("Movement toggled: " + isMovementEnabled);
        }

        if (!isMovementEnabled || !PhotonView.Get(this).IsMine)
        {
            return;
        }

        // Logika ruchu i akcji gracza
        Look();
        HandleSlideInput(); // zamiast starego Slide()
        Jump();
        UpdateUI();
        HandleWeaponSwitch();
        HandleEnergyPack();
    }

void FixedUpdate()
{
    if (!isMovementEnabled) return;
    if (!PV.IsMine) return;

    // --- AIR -> GROUND przechwycenie do SKI LANDING ---
    if (!wasGroundedLastFixed && isColliding)
    {
        if (GroundBelow(out RaycastHit hit))
        {
            TryApplySkiLanding(hit.normal); // ustaw flagę/guard, nie ruszaj velocity
        }
    }
    wasGroundedLastFixed = isColliding;

    if (Input.GetKey(slideKey) || isSliding)
    {
        ApplySlidingPhysics();
    }
    else
    {
        Movement();
    }

    HandleJetpack();
    ApplyExtraAirGravity();
    CapSpeed();

    // housekeeping
    if (!isColliding) lastAirVelocity = rb.velocity;
    wasGroundedLast = isColliding;
    if (skiLandingCooldown > 0f) skiLandingCooldown -= Time.fixedDeltaTime;
    if (landingGuardTimer > 0f)  landingGuardTimer  -= Time.fixedDeltaTime;
}


    // ================== RUCH / KAMERA ==================

    void Look()
    {
        pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity;
        pitch = Mathf.Clamp(pitch, -84.0f, 90.0f);
        yaw += Input.GetAxisRaw("Mouse X") * sensitivity;
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);

        Cam.position = headPos.position;
    }

void Movement()
{
    // surowy input (bez wygładzania)
    float ix = Input.GetAxisRaw("Horizontal");
    float iz = Input.GetAxisRaw("Vertical");

    Vector3 wishDir = (transform.forward * iz + transform.right * ix);

    // GROUND: pełny arcade snap, ale z minimalnym blendem przy zatrzymaniu
    if (isColliding && !isSliding)
    {
        Vector3 targetPlanar = wishDir.sqrMagnitude > 1e-4f
            ? wishDir.normalized * walkSpeed
            : Vector3.zero;

        Vector3 v = rb.velocity;
        v.x = Mathf.Lerp(v.x, targetPlanar.x, 0.25f); // lekki blend dla smooth stop
        v.z = Mathf.Lerp(v.z, targetPlanar.z, 0.25f);
        rb.velocity = new Vector3(v.x, v.y, v.z);

        rb.drag = groundDrag;

        UpdateAnimations(new Vector2(iz, ix));
        return;
    }

    // AIR: lekka kontrola
    if (wishDir.sqrMagnitude > 1e-6f)
    {
        Vector3 airWish = wishDir.normalized;
        rb.AddForce(airWish * (walkSpeed * 0.45f), ForceMode.Acceleration);
    }
}


  

        void ApplyExtraAirGravity()
    {
        // tylko w powietrzu i kiedy NIE jetujesz
        if (!isColliding && !IsJetting())
        {
            // add extra gravity as acceleration (independent of mass)
            float k = Mathf.Max(1f, airGravityMultiplier);
            rb.AddForce(Physics.gravity * (k - 1f), ForceMode.Acceleration);

            // limit downward terminal velocity
            Vector3 v = rb.velocity;
            if (v.y < -airTerminalDownSpeed)
            {
                v.y = -airTerminalDownSpeed;
                rb.velocity = v;
            }
        }
    }


    private void UpdateAnimations(Vector2 axis)
    {
        if (!anim) return;
          anim.SetFloat("xmove", axis.y); // forward/backward
          anim.SetFloat("ymove", axis.x); // right/left
    }

    void Jump()
    {
        if (Input.GetKey(KeyCode.Space) && isColliding && currentJetpackFuel > 0)
        {
            rb.AddForce(Vector3.up * 3.0f, ForceMode.Impulse);
        }
    }

    // ================== JETPACK ==================

void HandleJetpack()
{
    bool jetOn = IsJetting();

    if (jetOn)
    {
          // NEW: control horizontal jetpack thrust with player input
        float ix = Input.GetAxisRaw("Horizontal");
        float iz = Input.GetAxisRaw("Vertical");

          // horizontal force follows input (left/right and forward/backward)
        Vector3 horiz =
            transform.right   * (ix * jetpackForceX) +
            transform.forward * (iz * jetpackForceZ);

        // siła pionowa jak wcześniej
        Vector3 jetpackDirection = horiz + Vector3.up * jetpackForceY;

        rb.AddForce(jetpackDirection, ForceMode.Acceleration);
        UseJetpackFuel();

        if (!audioSource.isPlaying)
        {
            audioSource.clip = jetpackSound;
            audioSource.volume = 0.6f;
            audioSource.Play();
        }
    }
    else
    {
        if (audioSource.clip == jetpackSound && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    if (currentJetpackFuel <= 0) canUseJetpack = false;
    if (!canUseJetpack && currentJetpackFuel / jetpackFuelMax >= 0.1f) canUseJetpack = true;
    if (!jetOn && currentJetpackFuel < jetpackFuelMax) RegenerateJetpackFuel();
}

    bool IsJetting()
    {
        bool jetOn = canUseJetpack && currentJetpackFuel > 0 && (Input.GetMouseButton(1) || Input.GetKey(KeyCode.Space));
        return jetOn;
    }

    void UseJetpackFuel()
    {
        currentJetpackFuel -= jetpackFuelUsageRate * Time.deltaTime;
        currentJetpackFuel = Mathf.Clamp(currentJetpackFuel, 0, jetpackFuelMax);
    }

    void RegenerateJetpackFuel()
    {
        currentJetpackFuel += Time.deltaTime * jetpackFuelRegenRate;
        currentJetpackFuel = Mathf.Clamp(currentJetpackFuel, 0, jetpackFuelMax);
    }

    // ================== SLIDING ==================

    private void HandleSlideInput()
    {
        bool shiftHeld = Input.GetKey(slideKey);

        // licznik "ile czasu bez gruntu"
        if (isColliding || GroundBelow(out _)) timeSinceGrounded = 0f;
        else timeSinceGrounded += Time.deltaTime;

        // Start slide: wystarczy że trzymasz Shift (nie uzależniaj od isColliding)
        if (shiftHeld && !isSliding)
            StartSlide();

        // Stop slide: tylko gdy puścisz Shift LUB jesteś długo w powietrzu
        if ((!shiftHeld || timeSinceGrounded > slideGroundGrace) && isSliding)
            StartCoroutine(StopSlidingAfterDelay(0.15f));
    }


    private void StartSlide()
    {
        isSliding = true;
        rb.drag = slidingDrag;
        if (anim) anim.SetBool("isSliding", true);

        if (slideSound != null && audioSource != null)
        {
            audioSource.clip = slideSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    IEnumerator StopSlidingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        isSliding = false;
        if (anim) anim.SetBool("isSliding", false);

          // smooth drag return
        float t = 0f;
        float start = rb.drag;
        while (t < dragTransitionTime)
        {
            rb.drag = Mathf.Lerp(start, groundDrag, t / dragTransitionTime);
            t += Time.deltaTime;
            yield return null;
        }
        rb.drag = groundDrag;

        if (audioSource != null && audioSource.isPlaying && audioSource.clip == slideSound)
        {
            audioSource.Stop();
        }
    }

void ApplySlidingPhysics()
{
    // tylko gdy trzymasz slide
    if (!Input.GetKey(slideKey)) return;
    if (!GroundBelow(out RaycastHit hit)) return;

    float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
    if (slopeAngle < minSlopeAngle) return;

    Vector3 n = hit.normal.normalized;

    Vector3 v = rb.velocity;
    float v0 = v.magnitude;

      // Always declare to avoid errors
    Vector3 vt = Vector3.ProjectOnPlane(v, n); 
    float magB = v0;

    // Jeśli właśnie wylądowaliśmy — pominąć blendowanie
    if (justLandedSki)
    {
        justLandedSki = false; // zresetuj na następną ramkę
    }
    else
    {
        Vector3 vn = Vector3.Project(v, n);
        float keepNormal = 0.20f;
        Vector3 vProj = vt + vn * keepNormal;

        Vector3 dir0 = v.normalized;
        Vector3 dir1 = (vProj.sqrMagnitude > 1e-8f ? vProj.normalized : dir0);
        float dirBlend = 0.15f;
        Vector3 dirB = Vector3.Slerp(dir0, dir1, dirBlend);

        float magBlend = 0.15f;
        magB = Mathf.Lerp(v0, vProj.magnitude, magBlend);

        rb.velocity = dirB * magB;
    }

    // --- Downhill flow ---
    Vector3 along = Vector3.ProjectOnPlane(Physics.gravity, n);
    if (Vector3.Dot(along, rb.velocity) < 0f)
        along *= 0.9f;

    Vector3 tangent = (vt.sqrMagnitude > 1e-6f ? vt.normalized
                      : Vector3.ProjectOnPlane(transform.forward, n).normalized);

    // Minimalna prędkość na stoku
    float vNow = rb.velocity.magnitude;
    if (vNow < minSkiSpeed)
    {
        Vector3 kick = (along.sqrMagnitude > 1e-8f ? along.normalized : tangent);
        rb.AddForce(kick * (minSkiSpeed - vNow + 0.5f), ForceMode.Acceleration);
    }

    // Pchanie po stoku
    rb.AddForce(along * (1.0f + slideSpeedFactor), ForceMode.Acceleration);

    // Sterowanie boczne
    float steer = Input.GetAxisRaw("Horizontal");
    Vector3 right = Vector3.Cross(n, tangent).normalized;
    rb.AddForce(right * (steer * skiAcceleration * 0.6f), ForceMode.Acceleration);

      // Anti-drop — don't let speed fall by more than 15%
    float v1 = rb.velocity.magnitude;
    if (v1 < v0 * 0.85f)
    {
        rb.velocity = rb.velocity.normalized * (v0 * 0.85f);
    }
}


    // --- SAFE VELOCITY APPLY: ogranicza jednorazowy spadek i skręt kierunku ---
    void SafeSetVelocity(Vector3 vBefore, Vector3 vAfter, float keepFactor, float maxTurnDeg)
    {
        float v0 = vBefore.magnitude;
        if (v0 < 1e-4f) { rb.velocity = vAfter; return; }

        float v1 = vAfter.magnitude;
        float minKeep = v0 * keepFactor;                 // np. 0.90
        if (v1 < minKeep) v1 = minKeep;

        Vector3 d0 = vBefore.normalized;
        Vector3 d1 = (vAfter.sqrMagnitude > 1e-8f) ? vAfter.normalized : d0;
        Vector3 dSafe = Vector3.RotateTowards(d0, d1, maxTurnDeg * Mathf.Deg2Rad, Mathf.Infinity);

        rb.velocity = dSafe * v1;
    }



    // === SKI LANDING: wywoływane w FixedUpdate przy AIR->GROUND ===
void TryApplySkiLandingAtContact(Vector3 groundNormal, Vector3 preImpactVelocity)
{
    if (!Input.GetKey(slideKey)) return;    // musi być ski
    if (IsJetting()) return;                // nie może być jet

    Vector3 vAir = preImpactVelocity.sqrMagnitude > 1e-6f ? preImpactVelocity : rb.velocity;
    if (vAir.sqrMagnitude < 1e-6f) return;

    Vector3 n  = groundNormal.normalized;
    Vector3 vt = Vector3.ProjectOnPlane(vAir, n);
    if (vt.sqrMagnitude < 1e-6f) vt = Vector3.ProjectOnPlane(vAir + transform.forward * 0.01f, n);
    Vector3 vn = Vector3.Project(vAir, n);

    float angle = Vector3.Angle(vAir, vt.normalized);
    float loss  = Mathf.Clamp01(skiLossByAngle.Evaluate(Mathf.Clamp(angle, 0f, 90f)));
    float retain = 1f - loss;

    float slopeAngle = Vector3.Angle(n, Vector3.up);
    float normalDamp = (slopeAngle < 10f) ? 0f : skiNormalDamping;

    Vector3 candidate = vt.normalized * (vt.magnitude * retain) + vn * normalDamp;

    // TWARDY guard: zachowaj % vAir i ogranicz skręt
    SafeSetVelocity(vAir, candidate, landingKeepFactor, landingMaxTurnDeg);

    // włącz slide i niski drag natychmiast
    if (!isSliding) StartSlide(); else rb.drag = slidingDrag;

    // włącz okno guardu na kilka klatek
    landingGuardTimer = landingGuardTime;
}



void TryApplySkiLanding(Vector3 groundNormal)
{
    if (!Input.GetKey(slideKey)) return;                 // ski musi być wciśnięte
    if (IsJetting()) return;                             // nie podczas jet

    float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
    if (slopeAngle < minSlopeAngle) return;

    // użyj prędkości z powietrza tylko diagnostycznie
    Vector3 vIn = lastAirVelocity;

    // włącz "okno ochronne" – przez kilka klatek sliding ma 1. priorytet, a Movement nic nie nadpisze
    justLandedSki   = true;
    landingGuardTimer = landingGuardTime;
    // niczego nie zmieniamy w rb.velocity tutaj!
}

    // private bool GroundBelow(out RaycastHit hit)
    // {
    //     Vector3 origin = transform.position + Vector3.up * 0.1f;
    //     return Physics.Raycast(origin, Vector3.down, out hit, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    // }

    // bool GroundBelow(out RaycastHit hit)
    // {
    //     Vector3 origin = transform.position + Vector3.up * 0.2f;
    //     float radius = 0.25f;             // mała kula
    //     return Physics.SphereCast(origin, radius, Vector3.down, out hit,
    //         groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    // }
    bool GroundBelow(out RaycastHit bestHit)
{
    Vector3 origin = transform.position + Vector3.up * 0.2f;

    // 1) SphereCast centralny
    bool any = Physics.SphereCast(origin, groundProbeRadius, Vector3.down, out bestHit,
                                  groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    if (!any) return false;

      // 2) Two side samples -> average the normal to avoid sharp edges
    Vector3 n = bestHit.normal;
    RaycastHit hL, hR;
    Vector3 right = Vector3.Cross(Vector3.up, transform.forward).normalized;
    Vector3 oL = origin + right * groundProbeSide;
    Vector3 oR = origin - right * groundProbeSide;

    if (Physics.Raycast(oL, Vector3.down, out hL, groundCheckDistance + 0.5f, groundMask, QueryTriggerInteraction.Ignore))
        n += hL.normal;
    if (Physics.Raycast(oR, Vector3.down, out hR, groundCheckDistance + 0.5f, groundMask, QueryTriggerInteraction.Ignore))
        n += hR.normal;

    n.Normalize();

      // 3) Smooth over time (eliminate normal "jitter")
    lastGroundNormal = Vector3.Slerp(lastGroundNormal, n, groundNormalSmooth);

      // Replace the normal in the returned hit
    bestHit.normal = lastGroundNormal;
    return true;
}



    // ================== CAP / SPEED / UI ==================

    void CapSpeed()
    {
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    public float GetPlayerSpeed()
    {
        Vector2 horizontalSpeed = new Vector2(rb.velocity.x, rb.velocity.z);
        if (horizontalSpeed.magnitude == 0)
        {
            return Mathf.Abs(rb.velocity.y);
        }
        else
        {
            return horizontalSpeed.magnitude;
        }
    }

    void UpdateUI()
    {
        if (jetpackFuelImage != null)
        {
            jetpackFuelImage.fillAmount = currentJetpackFuel / jetpackFuelMax; // Update jetpack fuel bar
        }
        if (playerSpeedText != null)
        {
            playerSpeedText.text = "Speed: " + Mathf.Round(GetPlayerSpeed()).ToString();
        }
        if (speedImage != null)
        {
            speedImage.fillAmount = GetPlayerSpeed() / maxSpeed; // Update speed bar
        }
        if (healthbarImage != null)
        {
            healthbarImage.fillAmount = currentHealth / maxHealth; // Update health bar
        }
    }

      // ================== COLLISIONS ==================

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundMask) == 0) return;

        isColliding = true;
        if (anim) anim.SetBool("isColliding", true);

          // drag: low if ski is held or already sliding
        rb.drag = (isSliding || Input.GetKey(slideKey)) ? slidingDrag : groundDrag;

          // Immediate SKI LANDING (based on air velocity)
        if (Input.GetKey(slideKey) && !IsJetting())
        {
            Vector3 n = collision.GetContact(0).normal;
            TryApplySkiLandingAtContact(n, lastAirVelocity);
        }
    }




    void OnCollisionStay(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundMask) == 0) return;
        isColliding = true;

        if (!isSliding) rb.drag = groundDrag;

          if (landingGuardTimer > 0f && (isSliding || Input.GetKey(slideKey)))
          {
              Vector3 n = collision.GetContact(0).normal;
              Vector3 vAir = lastAirVelocity.sqrMagnitude > 1e-6f ? lastAirVelocity : rb.velocity;
              Vector3 vt = Vector3.ProjectOnPlane(vAir, n);
              if (vt.sqrMagnitude < 1e-6f) vt = Vector3.ProjectOnPlane(vAir + transform.forward * 0.01f, n);
              Vector3 vn = Vector3.Project(vAir, n);

              float angle = Vector3.Angle(vAir, vt.normalized);
              float loss  = Mathf.Clamp01(skiLossByAngle.Evaluate(Mathf.Clamp(angle, 0f, 90f)));
              float retain = 1f - loss;

              float slopeAngle = Vector3.Angle(n, Vector3.up);
              float normalDamp = (slopeAngle < 10f) ? 0f : skiNormalDamping;

              Vector3 candidate = vt.normalized * (vt.magnitude * retain) + vn * normalDamp;

              SafeSetVelocity(vAir, candidate, landingKeepFactor, landingMaxTurnDeg);
          }
    }


    void OnCollisionExit(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundMask) == 0) return;

        isColliding = false;
        if (anim) anim.SetBool("isColliding", false);

        rb.drag = airDrag;
    }

      // ================== PHOTON: PROPERTIES / WEAPON UI ==================

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
          // (merged from your two methods)
        if (changedProps.ContainsKey("itemIndex") && targetPlayer == PV.Owner)
        {
            if (!PV.IsMine)
            {
                EquipWeapon((int)changedProps["itemIndex"]);
            }
        }
    }

    [PunRPC]
    void RPC_UpdateJetpackFuel(float fuel)
    {
        currentJetpackFuel = fuel;
        if (jetpackFuelImage != null)
        {
            jetpackFuelImage.fillAmount = currentJetpackFuel / jetpackFuelMax;
        }
    }

    // ================== BROŃ ==================

    void HandleWeaponSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            weaponSlot = 1;
            EquipWeapon(weaponSlot);
            UpdateWeaponProperty(weaponSlot);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            weaponSlot = 2;
            EquipWeapon(weaponSlot);
            UpdateWeaponProperty(weaponSlot);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            weaponSlot = 3;
            EquipWeapon(weaponSlot);
            UpdateWeaponProperty(weaponSlot);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            weaponSlot = 4;
            EquipWeapon(weaponSlot);
            UpdateWeaponProperty(weaponSlot);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            weaponSlot -= (int)Mathf.Sign(scroll);
            if (weaponSlot < 1)
            {
                weaponSlot = 4; // Assuming you have 4 sloty
            }
            else if (weaponSlot > 4)
            {
                weaponSlot = 1;
            }
            EquipWeapon(weaponSlot);
            UpdateWeaponProperty(weaponSlot);
        }
    }

    void EquipWeapon(int slot)
    {
        if (!photonView.IsMine) return;

        // Zniszcz istniejącą broń
        if (currentWeapon != null)
        {
            PhotonNetwork.Destroy(currentWeapon);
        }

          // Choose weapon prefab based on slot
        GameObject weaponPrefab = slot switch
        {
            1 => primaryWeaponPrefab,
            2 => grenadeLauncherPrefab,
            3 => chaingunPrefab,
            4 => grapplerPrefab,
            _ => null
        };

        if (weaponPrefab == null)
        {
            Debug.LogError("Weapon prefab is null. Make sure the weapon slot is set up correctly.");
            return;
        }

          // Create weapon
        currentWeapon = PhotonNetwork.Instantiate(weaponPrefab.name, playerCamera.transform.position + playerCamera.transform.forward * 0.5f, playerCamera.transform.rotation);

          // Attach weapon to the local player
        AttachWeaponToPlayer();

          // Synchronize WeaponIK
        WeaponIK weaponIK = GetComponentInChildren<WeaponIK>();
        weaponIK?.ChangeWeapon(currentWeapon.transform);

          // Update weapon components
        ConfigureWeaponComponents(slot);
        UpdateAmmoUI();
    }

    void AttachWeaponToPlayer()
    {
        if (currentWeapon != null)
        {
            if (photonView.IsMine)
            {
                currentWeapon.transform.SetParent(playerCamera.transform);
                currentWeapon.transform.localPosition = new Vector3(0.35f, -0.3f, 0.25f);
                currentWeapon.transform.localRotation = Quaternion.identity;
            }
            else
            {
                currentWeapon.transform.SetParent(transform);
            }
        }
        else
        {
            Debug.LogError("Current weapon is null. Make sure the weapon is instantiated properly.");
        }
    }

    [PunRPC]
    void RPC_AttachWeaponToPlayer(int weaponViewID)
    {
        GameObject networkWeapon = PhotonView.Find(weaponViewID)?.gameObject;

        if (networkWeapon != null)
        {
            networkWeapon.transform.SetParent(playerCamera.transform);
            networkWeapon.transform.localPosition = new Vector3(0.35f, -0.4f, 0.3f);
            networkWeapon.transform.localRotation = Quaternion.identity;

            WeaponIK weaponIK = GetComponentInChildren<WeaponIK>();
            if (weaponIK != null)
            {
                weaponIK.ChangeWeapon(networkWeapon.transform);
            }
        }
        else
        {
            Debug.LogError("Failed to find network weapon. Make sure it was instantiated correctly.");
        }
    }

    void ConfigureWeaponComponents(int slot)
    {
        if (slot == 1)
        {
            discShooter = currentWeapon.GetComponent<DiscShooter>();
            if (discShooter != null)
            {
                discShooter.SetActiveWeapon(true);
                discShooter.SetLastShotTime(lastShotTime);
            }
            if (crosshairController != null)
            {
                crosshairController.SetChaingun(null);
                Debug.Log("Primary weapon equipped, crosshair disabled");
            }
        }
        else if (slot == 2)
        {
            grenadeLauncher = currentWeapon.GetComponent<GrenadeLauncher>();
            if (grenadeLauncher != null)
            {
                grenadeLauncher.SetActiveWeapon(true);
                grenadeLauncher.SetLastShotTime(lastShotTime);
            }
            if (crosshairController != null)
            {
                crosshairController.SetChaingun(null);
                Debug.Log("Grenade launcher equipped, crosshair disabled");
            }
        }
        else if (slot == 3)
        {
            chaingun = currentWeapon.GetComponent<Chaingun>();
            if (chaingun != null)
            {
                chaingun.SetActiveWeapon(true);
                chaingun.SetLastShotTime(lastShotTime);
                chaingun.SetHeat(storedHeat);
                if (crosshairController != null)
                {
                    crosshairController.SetChaingun(chaingun);
                }
            }
        }
        else if (slot == 4)
        {
            GrappleGun grappler = currentWeapon.GetComponent<GrappleGun>();
            if (grappler != null)
            {
                Debug.Log("Grappler equipped.");
            }
            else
            {
                Debug.LogWarning("GrapplerGun component not found on equipped weapon!");
            }
        }

        UpdateAmmoUI();
    }

    void UpdateWeaponProperty(int slot)
    {
        if (PV.IsMine)
        {
            ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
            hash.Add("itemIndex", slot);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
    }

    // ================== DAMAGE / DEATH ==================

    public void TakeDamage(float damage)
    {
        PV.RPC(nameof(RPC_TakeDamage), PV.Owner, damage);
    }

    [PunRPC]
    public void RPC_TakeDamage(float damage, Player killer, PhotonMessageInfo info)
    {
        currentHealth -= damage;
        Debug.Log($"RPC_TakeDamage: Gracz {PhotonNetwork.LocalPlayer.NickName} otrzymał obrażenia: {damage}. Aktualne zdrowie: {currentHealth}. Trafił: {killer?.NickName ?? "brak"}");

        if (killer != null && killer.IsLocal && killer != PV.Owner)
        {
            Debug.Log($"Gracz {killer.NickName} jest lokalnym graczem i trafił gracza {PhotonNetwork.LocalPlayer.NickName}");

            PlayerController killerController = killer.TagObject as PlayerController;
            if (killerController != null)
            {
                Debug.Log($"PlayerController znaleziony u gracza trafiającego. weaponSlot: {killerController.weaponSlot}");

                if (killerController.weaponSlot == 1) // DiscShooter
                {
                    Debug.Log("Wyświetlanie medalu DiscShootera przy trafieniu");
                    killerController.medalDisplay?.ShowDiscShooterMedal();
                }
                else if (killerController.weaponSlot == 2) // Granatnik
                {
                    Debug.Log("Wyświetlanie medalu granatnika przy trafieniu");
                    killerController.medalDisplay?.ShowGrenadeMedal();
                }
            }
            else
            {
                Debug.LogWarning("Nie znaleziono PlayerController dla killera.");
            }
        }
        else if (killer == PV.Owner)
        {
            Debug.Log("Gracz trafił samego siebie – pominięto wyświetlanie medalu.");
        }

        if (currentHealth <= 0)
        {
            Debug.Log($"Player {PhotonNetwork.LocalPlayer.NickName} died. Killer: {killer?.NickName ?? "none"}.");
            Die();
            playerManager.RecordDeath(killer);
        }

        if (healthbarImage != null)
        {
            healthbarImage.fillAmount = currentHealth / maxHealth;
        }

        if (Shot != null && audioSource != null)
        {
            audioSource.PlayOneShot(Shot);
        }
    }

    void Die()
    {
        if (!isAlive) return;
        isAlive = false;

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null && photonView.IsMine)
        {
            GameObject ragdollInstance = PhotonNetwork.Instantiate(ragdollPrefab.name, transform.position, transform.rotation);

            RagdollActivator ragdollActivator = ragdollInstance.GetComponent<RagdollActivator>();
            if (ragdollActivator != null)
            {
                ragdollActivator.ActivateRagdoll();
            }
        }

        if (playerManager != null)
        {
            playerManager.Die();
            DropHealthAmmoPickup();
            Debug.Log("Player died.");
        }

        PhotonNetwork.Destroy(gameObject);
    }

    private IEnumerator DestroyRagdollAfterDelay(GameObject ragdoll, float delay)
    {
        yield return new WaitForSeconds(delay);
        PhotonNetwork.Destroy(ragdoll);
    }

    // ================== INNE ==================

    public void EnableMovement(bool enable)
    {
        isMovementEnabled = enable;
    }

    [PunRPC]
    public void SetPlayerColor(float r, float g, float b)
    {
        Color newColor = new Color(r, g, b);
        playerRenderer.material.color = newColor;
        randomColor = newColor;
        Debug.Log("PlayerController: Set player color to " + newColor);
    }

    void UpdateAmmoUI()
    {
        if (ammoUI != null)
        {
            string weaponType = "";

            if (discShooter != null && discShooter.isActiveWeapon)
            {
                weaponType = "DiscShooter";
            }
            else if (grenadeLauncher != null && grenadeLauncher.isActiveWeapon)
            {
                weaponType = "GrenadeLauncher";
            }
            else if (chaingun != null && chaingun.isActiveWeapon)
            {
                weaponType = "Chaingun";
            }

            if (!string.IsNullOrEmpty(weaponType))
            {
                ammoUI.SetCurrentWeapon(weaponType);
            }
        }
    }

    public void RestoreHealthOverTime(float restorePercentage, float duration)
    {
        if (restoreHealthCoroutine != null)
        {
            StopCoroutine(restoreHealthCoroutine);
        }

        restoreHealthCoroutine = StartCoroutine(RestoreHealthCoroutine(restorePercentage, duration));
    }

    private IEnumerator RestoreHealthCoroutine(float restorePercentage, float duration)
    {
        float totalHealthToRestore = maxHealth * restorePercentage;
        float healthPerSecond = totalHealthToRestore / duration;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float healthToRestoreThisFrame = healthPerSecond * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth + healthToRestoreThisFrame, maxHealth);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    void DropHealthAmmoPickup()
    {
        Vector3 dropPosition = transform.position;

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Instantiate("HealthAmmoPickup", dropPosition, Quaternion.identity);
        }
    }

    void HandleEnergyPack()
    {
        if (!canUseEnergyPack)
        {
            energyPackCooldownTimer -= Time.deltaTime;
            if (energyPackCooldownTimer <= 0f)
            {
                canUseEnergyPack = true;
                Debug.Log("Energy Pack gotowy do użycia.");
            }
        }

        if (Input.GetKeyDown(KeyCode.E) && !isColliding && canUseEnergyPack && currentJetpackFuel >= energyPackFuelCost)
        {
            isUsingEnergyPack = true;
            energyPackTimer = energyPackDuration;
            canUseEnergyPack = false;
            energyPackCooldownTimer = energyPackCooldown;
            audioSource.PlayOneShot(energyPackSound);
        }

        if (isUsingEnergyPack)
        {
            energyPackTimer -= Time.deltaTime;

            float fuelUsagePerSecond = energyPackFuelCost / energyPackDuration;
            currentJetpackFuel -= fuelUsagePerSecond * Time.deltaTime;
            currentJetpackFuel = Mathf.Clamp(currentJetpackFuel, 0, jetpackFuelMax);

            if (jetpackFuelImage != null)
            {
                jetpackFuelImage.fillAmount = currentJetpackFuel / jetpackFuelMax;
            }

            if (PV.IsMine)
            {
                Vector3 boostDirection = playerCamera.transform.forward;
                rb.AddForce(boostDirection * jetpackForceZ / 2.25f, ForceMode.Impulse);
            }

            if (energyPackTimer <= 0f || currentJetpackFuel <= 0f)
            {
                isUsingEnergyPack = false;
                Debug.Log("Energy Pack wyłączony.");
            }
        }
    }

    // ====== STATYSTYKI ======

    private void TrackAirAndGroundTime()
    {
        if (isColliding)
        {
            timeOnGround += Time.deltaTime;
        }
        else
        {
            timeInAir += Time.deltaTime;
        }
    }

    private void TrackAverageSpeed()
    {
        if (playerSpeedText != null)
        {
            string[] lines = playerSpeedText.text.Split('\n');
            foreach (string line in lines)
            {
                if (line.StartsWith("Speed: "))
                {
                    string speedText = line.Replace("Speed: ", "").Trim();
                    if (float.TryParse(speedText, out float currentSpeed))
                    {
                        cumulativeSpeed += currentSpeed;
                        speedSamples++;
                    }
                    else
                    {
                  Debug.LogWarning($"Unable to parse speed from text: {speedText}");
                    }
                    return;
                }
            }

              Debug.LogWarning("Could not find a line with 'Speed:' in playerSpeedText.");
        }
        else
        {
            Debug.LogError("Player speed text is not assigned!");
        }
    }

    public float AverageSpeed
    {
        get
        {
            if (speedSamples > 0)
            {
                return cumulativeSpeed / speedSamples;
            }
            else
            {
                  Debug.LogWarning("No speed samples. Average speed is 0.");
                return 0f;
            }
        }
    }

    public float TimeInAir => timeInAir;
    public float TimeOnGround => timeOnGround;
}
