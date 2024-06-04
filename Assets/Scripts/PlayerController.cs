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
    [SerializeField] private float additionalGravity = 20.0f; 
    private float currentJetpackFuel;
    [SerializeField] private Text jetpackFuelText;
    [SerializeField] private Text playerSpeedText;
    private bool isSliding = false;
    private bool isColliding = false;
    [SerializeField] private float groundCheckDistance = 1.1f; // Distance for ground check raycast
    [SerializeField] private float groundOffset = 0.5f; // Safe offset from the ground
    [SerializeField] private float skiAcceleration = 10.0f; // Additional acceleration while skiing
    [SerializeField] private float minSkiSpeed = 5.0f; // Minimum speed while skiing

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
        if (isColliding)
        {
            Vector2 axis = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
            Vector3 forward = Camera.main.transform.forward * axis.x;
            Vector3 right = Camera.main.transform.right * axis.y;
            Vector3 wishDirection = (forward + right).normalized * walkSpeed;
            wishDirection.y = rb.velocity.y; // Maintain vertical velocity when on the ground
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
                rb.drag = 0f; // Remove drag while sliding
            }

            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance))
            {
                // Adjust player position to follow the ground's slope
                Vector3 slopeDirection = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
                float slopeFactor = Vector3.Dot(hit.normal, Vector3.up);
                float slideSpeed = rb.velocity.magnitude;

                if (slopeFactor > 0)
                {
                    slideSpeed *= 1 + (0.5f * (1 - slopeFactor)); 
                }
                else if (slopeFactor < 0)
                {
                    slideSpeed *= 1 - (0.5f * Mathf.Abs(slopeFactor)); 
                }

                slideSpeed = Mathf.Max(slideSpeed, minSkiSpeed); // Ensure minimum speed
                rb.velocity = slopeDirection * slideSpeed;

                // Smoothly adjust position to stick to the ground
                Vector3 targetPosition = new Vector3(transform.position.x, hit.point.y + groundOffset, transform.position.z);
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime * 10f);

                // Apply additional force to maintain or increase speed while skiing
                rb.AddForce(slopeDirection * skiAcceleration, ForceMode.Acceleration);
            }

            rb.AddForce(Vector3.down * additionalGravity, ForceMode.Acceleration);
        }
        else
        {
            isSliding = false;
            rb.drag = 0f; // Reset drag to normal when not sliding
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
