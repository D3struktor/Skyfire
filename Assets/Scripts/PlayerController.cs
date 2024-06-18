using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class PlayerController : MonoBehaviour
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
    private bool isSliding = false;
    private bool isColliding = false;
    [SerializeField] private float groundCheckDistance = 1.1f;
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
    private GameObject currentWeapon;
    private DiscShooter discShooter;
    private GrenadeLauncher grenadeLauncher;
    private int weaponSlot = 1;

    private PhotonView PV;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        PV = GetComponent<PhotonView>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        currentJetpackFuel = jetpackFuelMax;

        if (!PV.IsMine)
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
            Destroy(ui);
            jetpackFuelImage.gameObject.SetActive(false);
            playerSpeedText.gameObject.SetActive(false);
            speedImage.gameObject.SetActive(false);
            return;
        }

        EquipWeapon(weaponSlot); // Equip the default weapon (DiscShooter)
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

void HandleWeaponSwitch()
{
    if (Input.GetKeyDown(KeyCode.Alpha1))
    {
        weaponSlot = 1;
        EquipWeapon(weaponSlot);
    }
    if (Input.GetKeyDown(KeyCode.Alpha2))
    {
        weaponSlot = 2;
        EquipWeapon(weaponSlot);
    }

    float scroll = Input.GetAxis("Mouse ScrollWheel");
    if (scroll != 0)
    {
        weaponSlot -= (int)Mathf.Sign(scroll);
        if (weaponSlot < 1)
        {
            weaponSlot = 2; // Assuming you have 2 weapon slots
        }
        else if (weaponSlot > 2)
        {
            weaponSlot = 1;
        }
        EquipWeapon(weaponSlot);
    }
}


    void EquipWeapon(int slot)
    {
        if (currentWeapon != null)
        {
            if (currentWeapon.GetComponent<DiscShooter>() != null)
            {
                currentWeapon.GetComponent<DiscShooter>().SetActiveWeapon(false);
            }
            else if (currentWeapon.GetComponent<GrenadeLauncher>() != null)
            {
                currentWeapon.GetComponent<GrenadeLauncher>().SetActiveWeapon(false);
            }
            PhotonNetwork.Destroy(currentWeapon); // Destroy the current weapon in the network
        }

        if (slot == 1)
        {
            currentWeapon = PhotonNetwork.Instantiate(primaryWeaponPrefab.name, transform.position, Quaternion.identity);
            discShooter = currentWeapon.GetComponent<DiscShooter>();
            if (discShooter != null)
            {
                discShooter.SetActiveWeapon(true);
            }
        }
        else if (slot == 2)
        {
            currentWeapon = PhotonNetwork.Instantiate(grenadeLauncherPrefab.name, transform.position, Quaternion.identity);
            grenadeLauncher = currentWeapon.GetComponent<GrenadeLauncher>();
            if (grenadeLauncher != null)
            {
                grenadeLauncher.SetActiveWeapon(true);
            }
        }

        // Set the weapon as a child of the camera and reset its local position and rotation
        Transform cameraTransform = transform.Find("Camera");
        if (cameraTransform != null)
        {
            currentWeapon.transform.SetParent(cameraTransform);
            currentWeapon.transform.localPosition = new Vector3(0.5f, -0.5f, 1f); // Adjust as needed
            currentWeapon.transform.localRotation = Quaternion.identity; // Reset rotation
        }
        else
        {
            Debug.LogError("Camera transform not found. Make sure the camera is a child of the PlayerController.");
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

        // Apply additional gravity force
        rb.AddForce(Vector3.down * 20f); // Adjust the value as needed
    }

    void Look()
    {
        pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity;
        pitch = Mathf.Clamp(pitch, -90.0f, 90.0f);
        yaw += Input.GetAxisRaw("Mouse X") * sensitivity;
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        Camera.main.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }

    void Movement()
    {
        if (isColliding)
        {
            Vector2 axis = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
            Vector3 forward = Camera.main.transform.forward * axis.x;
            Vector3 right = Camera.main.transform.right * axis.y;
            Vector3 wishDirection = (forward + right).normalized * walkSpeed;
            wishDirection.y = rb.velocity.y; // Maintain vertical velocity on the ground
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
                rb.drag = slidingDrag; // Set drag to low value during sliding
            }
        }
        else
        {
            if (isSliding)
            {
                StartCoroutine(StopSlidingAfterDelay(0.5f)); // Delay stopping slide by 0.5 seconds
            }
        }
    }

    IEnumerator StopSlidingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        isSliding = false;
        rb.drag = endDrag;
        StartCoroutine(TransitionDrag(rb.drag, groundDrag, dragTransitionTime)); // Smoothly transition drag
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
            // Adjust player position to follow the ground slope
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

            slideSpeed = Mathf.Max(slideSpeed, minSkiSpeed); // Ensure minimum speed
            rb.velocity = slopeDirection * slideSpeed * 1.5f; // Increase sliding speed by 1.5 times

            // Apply additional force to maintain or increase speed while skiing
            rb.AddForce(slopeDirection * skiAcceleration, ForceMode.Acceleration);
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
        isColliding = true;
        rb.drag = groundDrag; // Set drag to groundDrag on collision with ground
    }

    void OnCollisionExit(Collision collision)
    {
        isColliding = false;
        rb.drag = airDrag; // Set drag to airDrag on leaving ground collision
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
}
