using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

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
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Image jetpackFuelImage;
    [SerializeField] private Image speedImage;
    [SerializeField] private Image healthbarImage;
    [SerializeField] private Text playerSpeedText;
    [SerializeField] private GameObject ui;
    private bool isSliding = false;
    private bool isColliding = false;
    [SerializeField] private float groundCheckDistance = 1.1f;
    [SerializeField] private float skiAcceleration = 20.0f;
    [SerializeField] private float minSkiSpeed = 10.0f;
    [SerializeField] private float groundDrag = 0.1f;
    [SerializeField] private float airDrag = 0.01f;
    [SerializeField] private float slidingDrag = 0.01f;
    [SerializeField] private float endDrag = 0.1f;
    [SerializeField] private float dragTransitionTime = 0.5f;
    [SerializeField] private float maxSpeed = 200f;

    public GameObject primaryWeaponPrefab;
    public GameObject grenadeLauncherPrefab;
    private GameObject currentWeapon;
    private DiscShooter discShooter;
    private GrenadeLauncher grenadeLauncher;
    private int weaponSlot = 1;

    private PhotonView PV;
    private Camera playerCamera;

    const float maxHealth = 100f;
    float currentHealth = maxHealth;

    PlayerManager playerManager;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        PV = GetComponent<PhotonView>();
        playerCamera = GetComponentInChildren<Camera>();
        playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager>();
    }

    void Start()
    {
        if (!PV.IsMine)
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
            ui.SetActive(false); // Deactivate UI for other players
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        currentJetpackFuel = jetpackFuelMax;

        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.SetTimerText(timerText);
        }

        if (PV.Owner.CustomProperties.TryGetValue("itemIndex", out object itemIndex))
        {
            EquipWeapon((int)itemIndex);
        }
        else
        {
            EquipWeapon(weaponSlot);
        }

        ui.SetActive(true); // Activate UI for the local player
        UpdateUI();
    }

    void Update()
    {
        if (!PV.IsMine)
            return;

        Look();
        HandleJetpack();
        Slide();
        Jump();
        UpdateUI();
        HandleWeaponSwitch();
    }

    void Look()
    {
        pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity;
        pitch = Mathf.Clamp(pitch, -90.0f, 90.0f);
        yaw += Input.GetAxisRaw("Mouse X") * sensitivity;
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
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

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            weaponSlot -= (int)Mathf.Sign(scroll);
            if (weaponSlot < 1)
            {
                weaponSlot = 2;
            }
            else if (weaponSlot > 2)
            {
                weaponSlot = 1;
            }
            EquipWeapon(weaponSlot);
            UpdateWeaponProperty(weaponSlot);
        }
    }

    void EquipWeapon(int slot)
    {
        if (currentWeapon != null)
        {
            PhotonNetwork.Destroy(currentWeapon);
        }

        if (slot == 1)
        {
            currentWeapon = PhotonNetwork.Instantiate(primaryWeaponPrefab.name, Vector3.zero, Quaternion.identity, 0, new object[] { PV.ViewID });
            discShooter = currentWeapon.GetComponent<DiscShooter>();
            if (discShooter != null)
            {
                discShooter.SetActiveWeapon(true);
            }
        }
        else if (slot == 2)
        {
            currentWeapon = PhotonNetwork.Instantiate(grenadeLauncherPrefab.name, Vector3.zero, Quaternion.identity, 0, new object[] { PV.ViewID });
            grenadeLauncher = currentWeapon.GetComponent<GrenadeLauncher>();
            if (grenadeLauncher != null)
            {
                grenadeLauncher.SetActiveWeapon(true);
            }
        }

        if (currentWeapon != null)
        {
            AttachWeaponToPlayer();
        }
    }

    void AttachWeaponToPlayer()
    {
        if (currentWeapon != null)
        {
            currentWeapon.transform.SetParent(playerCamera.transform);
            currentWeapon.transform.localPosition = new Vector3(0.5f, -0.5f, 1f);
            currentWeapon.transform.localRotation = Quaternion.identity;
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
        if (PV.IsMine)
        {
            if (jetpackFuelImage != null)
            {
                jetpackFuelImage.fillAmount = currentJetpackFuel / jetpackFuelMax;
            }
            if (playerSpeedText != null)
            {
                playerSpeedText.text = "Speed: " + Mathf.Round(GetPlayerSpeed()).ToString();
            }
            if (speedImage != null)
            {
                speedImage.fillAmount = GetPlayerSpeed() / maxSpeed;
            }
            if (healthbarImage != null)
            {
                healthbarImage.fillAmount = currentHealth / maxHealth;
            }
        }
    }

    void FixedUpdate()
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

        rb.AddForce(Vector3.down * 50f);
    }

    void Movement()
    {
        if (isColliding)
        {
            Vector2 axis = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
            Vector3 forward = Camera.main.transform.forward * axis.x;
            Vector3 right = Camera.main.transform.right * axis.y;
            Vector3 wishDirection = (forward + right).normalized * walkSpeed;
            wishDirection.y = rb.velocity.y;
            rb.velocity = wishDirection;
        }
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
        if (canUseJetpack && currentJetpackFuel > 0 && Input.GetMouseButton(1))
        {
            Vector3 jetpackDirection = transform.forward * jetpackForceZ + transform.right * jetpackForceX + Vector3.up * jetpackForceY;
            rb.AddForce(jetpackDirection, ForceMode.Acceleration);
            UseJetpackFuel();
        }

        if (currentJetpackFuel <= 0)
        {
            canUseJetpack = false;
        }

        if (!canUseJetpack && currentJetpackFuel / jetpackFuelMax >= 0.1f)
        {
            canUseJetpack = true;
        }

        if (!Input.GetMouseButton(1) && currentJetpackFuel < jetpackFuelMax)
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
                rb.drag = 0f;
            }
        }
        else
        {
            if (isSliding)
            {
                StartCoroutine(StopSlidingAfterDelay(0.5f));
            }
        }
    }

    IEnumerator StopSlidingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        isSliding = false;
        rb.drag = endDrag;
        StartCoroutine(TransitionDrag(rb.drag, groundDrag, dragTransitionTime));
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
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance))
        {
            Vector3 slopeDirection = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
            float slopeFactor = Vector3.Dot(hit.normal, Vector3.up);
            float slideSpeed = rb.velocity.magnitude;

            if (slopeFactor > 0)
            {
                slideSpeed *= 1 + (slideSpeedFactor * (1 - slopeFactor));
            }
            else if (slopeFactor < 0)
            {
                slideSpeed *= 1 - (slideSpeedFactor * Mathf.Abs(slopeFactor));
            }

            slideSpeed = Mathf.Max(slideSpeed, minSkiSpeed);
            rb.velocity = slopeDirection * slideSpeed * 1.5f;

            Vector3 appliedForce = slopeDirection * skiAcceleration;
            rb.AddForce(appliedForce, ForceMode.Acceleration);
        }
    }

    void CapSpeed()
    {
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    float GetPlayerSpeed()
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
        if (rb != null)
        {
            isColliding = true;
            rb.drag = groundDrag;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (rb != null)
        {
            isColliding = false;
            rb.drag = airDrag;
        }
    }


    [PunRPC]
    void RPC_UpdateJetpackFuel(float fuel)
    {
        currentJetpackFuel = fuel;
        if (PV.IsMine && jetpackFuelImage != null) // Ensure only local player updates fuel UI
        {
            jetpackFuelImage.fillAmount = currentJetpackFuel / jetpackFuelMax;
        }
    }

    public void TakeDamage(float damage)
    {
        PV.RPC(nameof(RPC_TakeDamage), PV.Owner, damage);
    }

    [PunRPC]
    void RPC_TakeDamage(float damage, PhotonMessageInfo info)
    {
        currentHealth -= damage;

        if (PV.IsMine && healthbarImage != null) // Ensure only local player updates healthbar UI
        {
            healthbarImage.fillAmount = currentHealth / maxHealth;
        }

        if (currentHealth <= 0)
        {
            Die();
            PlayerManager.Find(info.Sender).GetKill();
        }
    }

    void Die()
    {
        if (PV.IsMine)
        {
            playerManager.Die();
            Debug.Log("Player died.");
        }
    }
}
