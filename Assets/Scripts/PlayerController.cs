using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

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
    [SerializeField] private Text jetpackFuelText;
    [SerializeField] private Text playerSpeedText;
    private bool isSliding = false;
    private bool isColliding = false;
    [SerializeField] private float groundCheckDistance = 1.1f;
    [SerializeField] private float skiAcceleration = 10.0f;
    [SerializeField] private float minSkiSpeed = 5.0f;
    [SerializeField] private float groundDrag = 0.1f;
    [SerializeField] private float endDrag = 0.1f;
    [SerializeField] private float bounceSpeedThreshold = 10f;
    [SerializeField] private float bounceFactor = 0.5f;
    [SerializeField] private float dragTransitionTime = 0.5f;

    public GameObject primaryWeaponPrefab;  // Prefab broni głównej
    public GameObject grenadeLauncherPrefab;  // Prefab granatnika
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
            jetpackFuelText.gameObject.SetActive(false);
            playerSpeedText.gameObject.SetActive(false);
            return;
        }

        EquipWeapon(weaponSlot); // Wyposaż domyślną broń (DiscShooter)
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
            PhotonNetwork.Destroy(currentWeapon); // Zniszcz aktualną broń w sieci
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

        // Ustaw broń jako dziecko kamery i zresetuj jej lokalną pozycję i rotację
        Transform cameraTransform = transform.Find("Camera");
        if (cameraTransform != null)
        {
            currentWeapon.transform.SetParent(cameraTransform);
            currentWeapon.transform.localPosition = new Vector3(0.5f, -0.5f, 1f); // Dostosuj w razie potrzeby
            currentWeapon.transform.localRotation = Quaternion.identity; // Zresetuj rotację
        }
        else
        {
            Debug.LogError("Nie znaleziono transformacji kamery. Upewnij się, że kamera jest dzieckiem PlayerController.");
        }
    }

    void UpdateUI()
    {
        if (jetpackFuelText != null)
        {
            jetpackFuelText.text = "Fuel: " + Mathf.Round(currentJetpackFuel).ToString();
        }
        if (playerSpeedText != null)
        {
            playerSpeedText.text = "Speed: " + Mathf.Round(GetPlayerSpeed()).ToString();
        }
    }

    void FixedUpdate()
    {
        if (!PV.IsMine)
            return;

        Movement();
        if (isSliding)
        {
            ApplySlidingPhysics();
        }

        HandleJetpack();
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
            wishDirection.y = rb.velocity.y; // Zachowaj pionową prędkość na ziemi
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
        if (Input.GetMouseButton(1) && currentJetpackFuel > 0)
        {
            Vector3 jetpackDirection = transform.forward * jetpackForceZ + transform.right * jetpackForceX + Vector3.up * jetpackForceY;
            rb.AddForce(jetpackDirection, ForceMode.Acceleration);
            UseJetpackFuel();
        }
        else if (currentJetpackFuel < jetpackFuelMax)
        {
            currentJetpackFuel += Time.deltaTime * jetpackFuelRegenRate;
            currentJetpackFuel = Mathf.Clamp(currentJetpackFuel, 0, jetpackFuelMax);
        }
    }

    void UseJetpackFuel()
    {
        currentJetpackFuel -= jetpackFuelUsageRate * Time.deltaTime;
        currentJetpackFuel = Mathf.Clamp(currentJetpackFuel, 0, jetpackFuelMax);
    }

    void Slide()
    {
        if (Input.GetKey(KeyCode.LeftShift) && isColliding)
        {
            if (!isSliding)
            {
                isSliding = true;
                rb.drag = 0f; // Usuń opór podczas ślizgu
            }
        }
        else
        {
            if (isSliding)
            {
                StartCoroutine(StopSlidingAfterDelay(0.5f)); // Opóźnienie zatrzymania ślizgu o 0.5 sekundy
            }
        }
    }

    IEnumerator StopSlidingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        isSliding = false;
        rb.drag = endDrag;
        StartCoroutine(TransitionDrag(rb.drag, groundDrag, dragTransitionTime)); // Płynnie przejść opór
                
        // Logika odbijania
        if (GetPlayerSpeed() > bounceSpeedThreshold)
        {
            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * -bounceFactor, rb.velocity.z);
        }
        else
        {
            rb.velocity = new Vector3(rb.velocity.x * 0.5f, rb.velocity.y, rb.velocity.z * 0.5f);
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
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance))
        {
            // Dostosuj pozycję gracza do nachylenia ziemi
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

            slideSpeed = Mathf.Max(slideSpeed, minSkiSpeed); // Zapewnij minimalną prędkość
            rb.velocity = slopeDirection * slideSpeed;

            // Zastosuj dodatkową siłę, aby utrzymać lub zwiększyć prędkość podczas jazdy na nartach
            rb.AddForce(slopeDirection * skiAcceleration, ForceMode.Acceleration);
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
}
