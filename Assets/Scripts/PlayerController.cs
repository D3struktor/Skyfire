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
    [SerializeField] private float slideSpeedFactor = 1.5f;
    [SerializeField] private float jetpackForceY = 30.0f;
    [SerializeField] private float jetpackForceX = 15.0f;
    [SerializeField] private float jetpackForceZ = 15.0f;
    [SerializeField] private float jetpackFuelMax = 100.0f;
    [SerializeField] private float jetpackFuelRegenRate = 5.0f;
    [SerializeField] private float jetpackFuelUsageRate = 10.0f;
    private float currentJetpackFuel;
    private bool canUseJetpack = true;
    [SerializeField] private Image jetpackFuelImage; // Image for jetpack fuel bar
    [SerializeField] GameObject ui;
    [SerializeField] private Text playerSpeedText;
    [SerializeField] private Image speedImage; // Image for speed bar
    [SerializeField] private Image healthbarImage; // Image for health bar
    private bool isSliding = false;
    private bool isColliding = false;
    [SerializeField] private float groundCheckDistance = 100.1f;
    [SerializeField] private float skiAcceleration = 20.0f; // Increased acceleration value
    [SerializeField] private float minSkiSpeed = 10.0f; // Increased minimum ski speed
    [SerializeField] private float groundDrag = 0.1f;
    [SerializeField] private float airDrag = 0.01f;
    [SerializeField] private float slidingDrag = 0.01f; // Very low drag during sliding
    [SerializeField] private float endDrag = 0.1f;
    [SerializeField] private float dragTransitionTime = 0.5f;
    [SerializeField] private float maxSpeed = 200f; // Maximum speed for the speed bar and player speed cap

    public GameObject primaryWeaponPrefab;  // Primary weapon prefab
    public GameObject grenadeLauncherPrefab;  // Grenade launcher prefab
    public GameObject chaingunPrefab;  // Chaingun prefab
    private GameObject currentWeapon;
    private DiscShooter discShooter;
    private GrenadeLauncher grenadeLauncher;
    private Chaingun chaingun; // Reference to the Chaingun script
    private int weaponSlot = 1;

    private PhotonView PV;
    private Camera playerCamera;

    const float maxHealth = 100f;
    float currentHealth = maxHealth;
    float CurrentHealth;

    PlayerManager playerManager;
    private CrosshairController crosshairController; // Referencja do CrosshairController

    private float lastShotTime = 0f; // Time when the last shot was fired
    private float fireCooldown = 0.7f; // Cooldown time between shots

    private float storedHeat = 0f; // Przechowywana wartość ciepła
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
    private float energyPackFuelCost = 80f; // 20 jednostek paliwa zużywane przez 2 sekundy
    private float energyPackTimer = 0f;
    private float energyPackCooldownTimer = 0f;
    private bool canUseEnergyPack = true;

    //MODEL!!!

    public GameObject model; // Referencja do obiektu model
    private Animator anim; // Referencja do Animatora

    private float mouseX;
    private float mouseY;
    private bool isGrounded = true;

    [SerializeField] private Transform headPos;
    [SerializeField] Transform Cam = null;
    // [SerializeField] private CapsuleCollider capsuleCollider;

    public GameObject ragdollPrefab; // Przypisz prefab ragdolla w inspektorze



    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        PV = GetComponent<PhotonView>();
        playerCamera = GetComponentInChildren<Camera>();
        playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager>();
        crosshairController = FindObjectOfType<CrosshairController>(); // Znajdź CrosshairController w scenie

        // Initialize AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        currentJetpackFuel = jetpackFuelMax;

        playerRenderer = GetComponent<Renderer>();
        playerRenderer.material.color = randomColor;

        model = transform.Find("model").gameObject; // Znajdź obiekt "model" w hierarchii
        anim = model.GetComponent<Animator>(); // Pobierz Animator z obiektu model
         // Pobieramy obiekt Ch44, który jest dzieckiem modelu
        GameObject ch44 = model.transform.Find("Ch44").gameObject;

        // Pobieramy renderer z obiektu Ch44
        Renderer ch44Renderer = ch44.GetComponent<Renderer>();

        // Pobieramy aktualne materiały przypisane do obiektu Ch44
        Material[] materials = ch44Renderer.materials;

        // Zmieniamy kolor materiału Element 1 (czyli drugiego materiału)
        materials[1].color = randomColor;

        // Przypisujemy zaktualizowaną tablicę materiałów z powrotem do renderera
        ch44Renderer.materials = materials;

        if (!PV.IsMine)
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
            Destroy(ui);
            jetpackFuelImage.gameObject.SetActive(false);
            playerSpeedText.gameObject.SetActive(false);
            speedImage.gameObject.SetActive(false);
            healthbarImage.gameObject.SetActive(false); // Hide health bar for other players
            return;
        }
        if (photonView.IsMine)
        {
            Debug.Log("PlayerController: Setting player color for local player.");
            // photonView.RPC("SetPlayerColor", RpcTarget.AllBuffered, Random.value, Random.value, Random.value);
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
    }

    void Update()
    {
        // Przełącz `isMovementEnabled` przy użyciu klawisza Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isMovementEnabled = !isMovementEnabled;
            Debug.Log("Movement toggled: " + isMovementEnabled);
        }

        // Jeśli ruch jest wyłączony, zatrzymaj wszystkie akcje
        if (!isMovementEnabled || !PhotonView.Get(this).IsMine)
        {
            return;
        }

        // Logika ruchu i akcji gracza
        Look();
        HandleJetpack();
        Slide();
        Jump();
        UpdateUI();
        HandleWeaponSwitch();
        HandleEnergyPack();
    }

    

    void Look()
    {
        pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity;
        pitch = Mathf.Clamp(pitch, -90.0f, 90.0f);
        yaw += Input.GetAxisRaw("Mouse X") * sensitivity;
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);

        Cam.position = headPos.position;
    }

    

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

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            weaponSlot -= (int)Mathf.Sign(scroll);
            if (weaponSlot < 1)
            {
                weaponSlot = 3; // Assuming you have 3 weapon slots
            }
            else if (weaponSlot > 3)
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

    // Wybór prefabu broni na podstawie slotu
    GameObject weaponPrefab = slot switch
    {
        1 => primaryWeaponPrefab,
        2 => grenadeLauncherPrefab,
        3 => chaingunPrefab,
        _ => null
    };

    if (weaponPrefab == null)
    {
        Debug.LogError("Weapon prefab is null. Make sure the weapon slot is set up correctly.");
        return;
    }

    // Tworzenie broni
    currentWeapon = PhotonNetwork.Instantiate(weaponPrefab.name, playerCamera.transform.position + playerCamera.transform.forward * 0.5f, playerCamera.transform.rotation);

    // Dołączenie broni do gracza lokalnie
    AttachWeaponToPlayer();

    // Synchronizacja WeaponIK
    WeaponIK weaponIK = GetComponentInChildren<WeaponIK>();
    weaponIK?.ChangeWeapon(currentWeapon.transform);

    // Aktualizacja komponentów broni
    ConfigureWeaponComponents(slot);
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

    UpdateAmmoUI();
}

void AttachWeaponToPlayer()
{
    if (currentWeapon != null)
    {
        if (photonView.IsMine)
        {
            // Ustawienie broni jako dziecka kamery lokalnie, aby podążała za nią
            currentWeapon.transform.SetParent(playerCamera.transform);
            currentWeapon.transform.localPosition = new Vector3(0.35f, -0.4f, 0.3f);
            currentWeapon.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // Dla innych graczy broń pozostaje dzieckiem PlayerController, ale nie będzie przymocowana do ich kamery
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
    // Znajdź broń na podstawie ViewID
    GameObject networkWeapon = PhotonView.Find(weaponViewID)?.gameObject;

    if (networkWeapon != null)
    {
        // Dołącz broń do kamery gracza
        networkWeapon.transform.SetParent(playerCamera.transform);
        networkWeapon.transform.localPosition = new Vector3(0.35f, -0.4f, 0.3f);
        networkWeapon.transform.localRotation = Quaternion.identity;

        // Konfiguracja WeaponIK
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



    void UpdateWeaponProperty(int slot)
    {
        if (PV.IsMine)
        {
            ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
            hash.Add("itemIndex", slot);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
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
            if (!canUseEnergyPack && playerSpeedText != null)
    {
        playerSpeedText.text += "\nEnergy Pack Cooldown: " + Mathf.Ceil(energyPackCooldownTimer).ToString() + "s";
    }
    }

    void FixedUpdate()
    {if (isMovementEnabled)
        {
        if (!PV.IsMine)
            return;

        if (!isSliding)
        {
            Movement();
        }

        if (isSliding)
        {
            ApplySlidingPhysics();
        }

        HandleJetpack();
        CapSpeed();

        // Apply additional gravity force
        rb.AddForce(Vector3.down * 50f); // Adjust the value as needed
        }
    }


void Movement()
{
    if (isColliding)
    {
        Vector2 axis = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
        
        // Używamy transform.forward i transform.right, zamiast forward kamery
        Vector3 forward = transform.forward * axis.x;
        Vector3 right = transform.right * axis.y;
        Vector3 wishDirection = (forward + right).normalized * walkSpeed;

        wishDirection.y = rb.velocity.y; // Zachowaj pionową prędkość (grawitację)
        rb.velocity = wishDirection;

        // Aktualizacja animacji na podstawie wartości ruchu
        UpdateAnimations(axis);
    }
}

    private void UpdateAnimations(Vector2 axis)
{
    // Przekazywanie wartości do Animatora (odpowiednie dla xmove i ymove)
    anim.SetFloat("xmove", axis.y); // Ruch w osi pionowej (przód/tył)
    anim.SetFloat("ymove", axis.x); // Ruch w osi poziomej (prawo/lewo)
}

    void Jump()
    {
        if (Input.GetKey(KeyCode.Space) && isColliding && currentJetpackFuel > 0)
        {
            rb.AddForce(Vector3.up * 3.0f, ForceMode.Impulse);
        }
    }

    void HandleJetpack()
    {
        if (canUseJetpack && currentJetpackFuel > 0 && Input.GetMouseButton(1)|| Input.GetKey(KeyCode.Space))
        {
            Vector3 jetpackDirection = transform.forward * jetpackForceZ + transform.right * jetpackForceX + Vector3.up * jetpackForceY;
            rb.AddForce(jetpackDirection, ForceMode.Acceleration);
            UseJetpackFuel();

            if (!audioSource.isPlaying)
            {
            audioSource.clip = jetpackSound; // Set the audio clip to jetpack sound
            audioSource.volume = 0.6f; // Set volume to 60%
            audioSource.Play();
            }
        }
            else
    {
        // Stop the sound when not using the jetpack
        if (audioSource.clip == jetpackSound && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }


        if (currentJetpackFuel <= 0)
        {
            canUseJetpack = false;
        }

        if (!canUseJetpack && currentJetpackFuel / jetpackFuelMax >= 0.1f)
        {
            canUseJetpack = true;
        }

         if (!Input.GetMouseButton(1) && !Input.GetKey(KeyCode.Space) && currentJetpackFuel < jetpackFuelMax)
        {
            RegenerateJetpackFuel();
        }
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

    void Slide()
    {
        if (Input.GetKey(KeyCode.LeftShift) && isColliding)
        {
            if (!isSliding)
            {
                isSliding = true;
                rb.drag = 0f; // Set drag to low value during sliding
                anim.SetBool("isSliding", true);

                // Play sliding sound
                if (slideSound != null && audioSource != null)
                {
                    audioSource.clip = slideSound;
                    audioSource.loop = true; // Loop the sliding sound
                    audioSource.Play();
                }
            }
        }
        else
        {
            if (isSliding)
            {
                StartCoroutine(StopSlidingAfterDelay(0.2f)); // Delay stopping slide by 0.2 seconds
            }
        }
    }

    IEnumerator StopSlidingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        isSliding = false;
        anim.SetBool("isSliding", false);
        rb.drag = endDrag;
        StartCoroutine(TransitionDrag(rb.drag, groundDrag, dragTransitionTime)); // Smoothly transition drag
        
        // Stop sliding sound
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    IEnumerator TransitionDrag(float startDrag, float endDrag, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            rb.drag = Mathf.Lerp(startDrag, endDrag, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    void ApplySlidingPhysics()
    {
        // Debug.Log("Apply Slide !");
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance))
        {
            Vector3 slopeDirection = Vector3.ProjectOnPlane(rb.velocity, hit.normal).normalized;
            float slopeFactor = Vector3.Dot(hit.normal, transform.up);
            float slideSpeed = 1;//rb.velocity.magnitude; // ???

            // Debugging slope direction and factor
            // Debug.Log("Slope Direction: " + slopeDirection);
            // Debug.Log("Slope Factor: " + slopeFactor);

            if (slopeFactor > 0)
            {
                slideSpeed *= 1 + (slideSpeedFactor * (1 - slopeFactor)); 
            }
            else if (slopeFactor < 0)
            {
                slideSpeed *= 1 - (slideSpeedFactor * Mathf.Abs(slopeFactor)); 
            }

            slideSpeed = Mathf.Max(slideSpeed, minSkiSpeed); // Ensure minimum speed
            rb.AddForce( slopeDirection * slideSpeed * 1.3f, ForceMode.Acceleration); // Increase sliding speed by 1.5 times

            // Apply additional force to maintain or increase speed while skiing
            Vector3 appliedForce = slopeDirection * skiAcceleration;
            rb.AddForce(appliedForce, ForceMode.Acceleration);

            // Debugging applied force
            // Debug.Log("Applied Force: " + appliedForce);
        }
    }

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

    void OnCollisionEnter(Collision collision)
    {
        // Debug.Log("Player colistion.");
        isColliding = true;
        if (rb != null) // Check if rb is still valid
        {
            rb.drag = groundDrag; // Set drag to groundDrag on collision with ground
            anim.SetBool("isColliding", true);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        isColliding = false;
        if (rb != null) // Check if rb is still valid
        {
            isColliding = false;
            rb.drag = airDrag; // Set drag to airDrag on leaving ground collision
            anim.SetBool("isColliding", false);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("itemIndex") && !PV.IsMine && targetPlayer == PV.Owner)
        {
            EquipWeapon((int)changedProps["itemIndex"]);
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

    public void TakeDamage(float damage)
    {
        PV.RPC(nameof(RPC_TakeDamage), PV.Owner, damage);
    }

    [PunRPC]
    public void RPC_TakeDamage(float damage, Player killer, PhotonMessageInfo info)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            // Call Die first to handle destruction and respawning
            Die();

            // Record the death for the local player and attribute the kill to the correct player
            playerManager.RecordDeath(killer);
        }

        // Update health UI here
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
    // Tworzenie instancji ragdolla na pozycji gracza
    GameObject ragdollInstance = PhotonNetwork.Instantiate(ragdollPrefab.name, transform.position, transform.rotation);
    ragdollInstance.transform.localScale = new Vector3(6f, 6f, 6f);

    // Aktywacja fizyki w ragdollu
    RagdollActivator ragdollActivator = ragdollInstance.GetComponent<RagdollActivator>();
    if (ragdollActivator != null)
    {
        ragdollActivator.ActivateRagdoll();
    }

    // Zniszcz ragdolla po 3 sekundach
    StartCoroutine(DestroyRagdollAfterDelay(ragdollInstance, 3f));

    // Powiadomienie managera o śmierci gracza
    if (playerManager != null)
    {
        playerManager.Die();
        DropHealthAmmoPickup();
        Debug.Log("Player died.");
    }

    // Natychmiastowe zniszczenie głównego obiektu gracza
    PhotonNetwork.Destroy(gameObject);
}

private IEnumerator DestroyRagdollAfterDelay(GameObject ragdoll, float delay)
{
    yield return new WaitForSeconds(delay);
    PhotonNetwork.Destroy(ragdoll); // Użyj PhotonNetwork.Destroy, jeśli chcesz zsynchronizować zniszczenie ragdolla w sieci
}






    
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

            // Sprawdź, która broń jest aktualnie aktywna
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

            // Zaktualizuj UI z nazwą aktywnej broni
            if (!string.IsNullOrEmpty(weaponType))
            {
                ammoUI.SetCurrentWeapon(weaponType); 
            }
        }
    }
       public void RestoreHealthOverTime(float restorePercentage, float duration)
    {
        // Zatrzymaj istniejącą korutynę, jeśli jest aktywna
        if (restoreHealthCoroutine != null)
        {
            StopCoroutine(restoreHealthCoroutine);
        }

        // Rozpocznij nową korutynę odnawiania zdrowia
        restoreHealthCoroutine = StartCoroutine(RestoreHealthCoroutine(restorePercentage, duration));
    }

    // Korutyna, która odnawia zdrowie przez określony czas
    private IEnumerator RestoreHealthCoroutine(float restorePercentage, float duration)
    {
        float totalHealthToRestore = maxHealth * restorePercentage;  // Całkowite zdrowie do odnowienia
        float healthPerSecond = totalHealthToRestore / duration;      // Zdrowie odnawiane na sekundę
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float healthToRestoreThisFrame = healthPerSecond * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth + healthToRestoreThisFrame, maxHealth);  // Aktualizacja zdrowia

            Debug.Log("Zdrowie odnowione: " + currentHealth + "/" + maxHealth);

            elapsedTime += Time.deltaTime;  // Zwiększamy czas
            yield return null;  // Czekamy do następnej klatki
        }

        // Upewniamy się, że po zakończeniu odnowienia zdrowie nie przekroczy maksymalnej wartości
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        Debug.Log("Ostateczne zdrowie: " + currentHealth + "/" + maxHealth);
    }

     // Odnawianie zdrowia
    // public void RestoreHealth(float restorePercentage)
    // {
    //     float healthToRestore = maxHealth * restorePercentage;
    //     currentHealth = Mathf.Min(currentHealth + healthToRestore, maxHealth); // Zapobiega przekroczeniu maksymalnej wartości
    //     Debug.Log("Zdrowie odnowione: " + currentHealth + "/" + maxHealth);
    // }

    // // Odnawianie amunicji
    // public void RestoreAmmo(float restorePercentage)
    // {
    //     // Obliczamy, ile amunicji odnowić dla każdej broni
    //     int discShooterAmmoToRestore = Mathf.RoundToInt(maxDiscShooterAmmo * restorePercentage);
    //     int grenadeLauncherAmmoToRestore = Mathf.RoundToInt(maxGrenadeLauncherAmmo * restorePercentage);
    //     int chaingunAmmoToRestore = Mathf.RoundToInt(maxChaingunAmmo * restorePercentage);

    //     // Odnawiamy amunicję, ale nie przekraczamy maksymalnych wartości
    //     currentDiscShooterAmmo = Mathf.Min(currentDiscShooterAmmo + discShooterAmmoToRestore, maxDiscShooterAmmo);
    //     currentGrenadeLauncherAmmo = Mathf.Min(currentGrenadeLauncherAmmo + grenadeLauncherAmmoToRestore, maxGrenadeLauncherAmmo);
    //     currentChaingunAmmo = Mathf.Min(currentChaingunAmmo + chaingunAmmoToRestore, maxChaingunAmmo);

    //     Debug.Log("Amunicja odnowiona: DiscShooter: " + currentDiscShooterAmmo + "/" + maxDiscShooterAmmo +
    //               ", GrenadeLauncher: " + currentGrenadeLauncherAmmo + "/" + maxGrenadeLauncherAmmo +
    //               ", Chaingun: " + currentChaingunAmmo + "/" + maxChaingunAmmo);
    // }
    void DropHealthAmmoPickup()
    {
        // Ustal miejsce, gdzie ma spaść pickup (np. w miejscu gracza)
        Vector3 dropPosition = transform.position;
        
        // Stwórz pickup
        if (PhotonNetwork.IsMasterClient)
        {
        PhotonNetwork.Instantiate("HealthAmmoPickup", dropPosition, Quaternion.identity);
        }
    }
void HandleEnergyPack()
{
    // Cooldown Energy Packa
    if (!canUseEnergyPack)
    {
        energyPackCooldownTimer -= Time.deltaTime;
        if (energyPackCooldownTimer <= 0f)
        {
            canUseEnergyPack = true;
            Debug.Log("Energy Pack gotowy do użycia.");
        }
    }

    // Użycie Energy Packa, jeśli gracz jest w powietrzu i Energy Pack jest dostępny
    if (Input.GetKeyDown(KeyCode.E) && !isColliding && canUseEnergyPack && currentJetpackFuel >= energyPackFuelCost)
    {
        isUsingEnergyPack = true;
        energyPackTimer = energyPackDuration;
        canUseEnergyPack = false;
        energyPackCooldownTimer = energyPackCooldown;

        Debug.Log("Energy Pack aktywowany.");

        // Odtwórz dźwięk Energy Packa
        if (audioSource != null && energyPackSound != null)
        {
            audioSource.clip = energyPackSound;
            audioSource.loop = false;  // Energy Pack dźwięk nie musi się zapętlać
            audioSource.Play();
        }
    }

    // Jeśli Energy Pack jest używany, odliczaj czas trwania i zużywaj paliwo
    if (isUsingEnergyPack)
    {
        energyPackTimer -= Time.deltaTime;

        // Zużywanie paliwa stopniowo przez czas działania Energy Packa
        float fuelUsagePerSecond = energyPackFuelCost / energyPackDuration;
        currentJetpackFuel -= fuelUsagePerSecond * Time.deltaTime;
        currentJetpackFuel = Mathf.Clamp(currentJetpackFuel, 0, jetpackFuelMax);

        // Aktualizacja UI, aby pokazać zużycie paliwa
        if (jetpackFuelImage != null)
        {
            jetpackFuelImage.fillAmount = currentJetpackFuel / jetpackFuelMax;
        }

        // Dodanie impulsu w kierunku, w którym patrzy gracz (tylko jeśli to jest nasz obiekt)
        if (PV.IsMine)
        {
            Vector3 boostDirection = playerCamera.transform.forward;
            rb.AddForce(boostDirection * jetpackForceZ/2, ForceMode.Impulse);
            Debug.Log("Siła dodana jako impuls: " + boostDirection * jetpackForceZ);
        }

        // Zakończ działanie Energy Packa, gdy czas się skończy lub braknie paliwa
        if (energyPackTimer <= 0f || currentJetpackFuel <= 0f)
        {
            isUsingEnergyPack = false;
            Debug.Log("Energy Pack wyłączony.");
        }
    }
}

}

