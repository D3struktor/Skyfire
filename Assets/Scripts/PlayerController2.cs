using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class Movemencik : MonoBehaviour
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
    public Text jetpackFuelText;
    public Text playerSpeedText;
    private bool isSliding = false;
    private bool isColliding = false; 

    PhotonView PV;
    void Awake()
    {
     rb = GetComponent<Rigidbody>();
     PV = GetComponent<PhotonView>();
    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        currentJetpackFuel = jetpackFuelMax;

        if(!PV.IsMine)
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
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
        if(!PV.IsMine)
            return;

        Look();
        HandleJetpack();
        Slide();
        Jump();

        jetpackFuelText.text = "Fuel: " + Mathf.Round(currentJetpackFuel).ToString();
        playerSpeedText.text = "Speed: " + Mathf.Round(GetPlayerSpeed()).ToString();
    }

    void FixedUpdate()
    {
        if (!isSliding)
        {
            Movement();
        }
        if(!PV.IsMine)
            return;

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
        rb.drag = Mathf.Lerp(rb.drag, 0, Time.deltaTime * 5); 
    }
    else
    {
        isSliding = false;
        rb.drag = Mathf.Lerp(rb.drag, 1, Time.deltaTime * 5); 
    }

    if (isSliding)
    {
        Vector3 slideDirection = rb.velocity.normalized;
        float initialSpeed = rb.velocity.magnitude;
        float slideSpeed = initialSpeed * slideSpeedFactor; 

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit))
        {
            float slopeFactor = Vector3.Dot(hit.normal, Vector3.up);
            if (slopeFactor > 0)
            {
                slideSpeed *= 1 + (0.125f * (1 - slopeFactor)); 
            }
            else
            {
                slideSpeed *= 1 + (slopeFactor * 4); 
            }
        }

        rb.velocity = slideDirection * slideSpeed; 
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