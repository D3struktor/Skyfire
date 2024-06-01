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
    [SerializeField] private float jetpackForce = 10.0f;
    [SerializeField] private float jetpackFuelMax = 100.0f;
    [SerializeField] private float jetpackFuelRegenRate = 5.0f;
    [SerializeField] private float jetpackFuelUsageRate = 10.0f;
    private float currentJetpackFuel;
    [SerializeField] private Text jetpackFuelText;
    [SerializeField] private Text playerSpeedText;
    private bool isSliding = false;
    private bool isColliding = false;

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
    }

    void OnCollisionEnter(Collision collision)
    {
        isColliding = true;
    }

    void OnCollisionExit(Collision collision)
    {
        isColliding = false;
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
    }
    
    void UpdateUI()
    {
        // Aktualizacja UI tylko dla lokalnego gracza
        jetpackFuelText.text = "Fuel: " + Mathf.Round(currentJetpackFuel).ToString();
        playerSpeedText.text = "Speed: " + Mathf.Round(GetPlayerSpeed()).ToString();
    }

    void FixedUpdate()
    {
        if (!PV.IsMine)
            return;

        if (!isSliding)
        {
            Movement();
        }
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
        Vector2 axis = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
        Vector3 forward = Camera.main.transform.forward * axis.x;
        Vector3 right = Camera.main.transform.right * axis.y;
        Vector3 wishDirection = (forward + right).normalized * walkSpeed;
        wishDirection.y = rb.velocity.y; // Maintain vertical velocity
        rb.velocity = wishDirection;
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
            rb.AddForce(Vector3.up * jetpackForce, ForceMode.Acceleration);
            UseJetpackFuel();
        }

        if (!Input.GetMouseButton(1) && currentJetpackFuel < jetpackFuelMax)
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
        isSliding = true;
        rb.drag = 1;  // Zwiększone tarcie podczas ślizgu
    }
    else
    {
        isSliding = false;
        rb.drag = 5;  // Przywrócenie wyższego tarcia
    }

    if (isSliding)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit))
        {
            Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
            float initialSpeed = Mathf.Min(rb.velocity.magnitude, 10.0f);
            float slideSpeed = initialSpeed * slideSpeedFactor;
            float slopeFactor = Vector3.Dot(hit.normal, Vector3.up);

            if (slopeFactor > 0)
            {
                slideSpeed *= 1 + (0.05f * (1 - slopeFactor));
            }
            else
            {
                slideSpeed *= 1 + (slopeFactor * 2);
            }

            slideSpeed = Mathf.Min(slideSpeed, 10.0f);  // Maksymalna prędkość ślizgu
            rb.velocity = slopeDirection * slideSpeed;
        }
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
