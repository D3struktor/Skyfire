using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Movemencik : MonoBehaviour
{
    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private Rigidbody rb;
    [SerializeField] float walkSpeed = 0.0f, sensitivity = 2.0f;
    [SerializeField] float jetpackForce = 10.0f;
    [SerializeField] float jetpackFuelMax = 100.0f;
    [SerializeField] float jetpackFuelRegenRate = 5.0f;
    [SerializeField] float jetpackFuelUsageRate = 10.0f;
    [SerializeField] float slideSpeedFactor = 1.0f;
    private float currentJetpackFuel;
    public Text jetpackFuelText;
    public Text playerSpeedText;

    private bool isSliding = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        rb = GetComponent<Rigidbody>();
        currentJetpackFuel = jetpackFuelMax;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Space) && Physics.Raycast(rb.transform.position, Vector3.down, 1 + 0.001f))
            rb.velocity = new Vector3(rb.velocity.x, 5.0f, rb.velocity.z);
        Look();
        HandleJetpack();
        Slide();

        jetpackFuelText.text = "Fuel: " + Mathf.Round(currentJetpackFuel).ToString();
        playerSpeedText.text = "Speed: " + Mathf.Round(GetPlayerSpeed()).ToString();
    }

    private void FixedUpdate()
    {
        Movement();
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
        if (!isSliding)
        {
            Vector2 axis = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
            Vector3 forward = Camera.main.transform.forward * axis.x;
            Vector3 right = Camera.main.transform.right * axis.y;
            Vector3 wishDirection = (forward + right).normalized * walkSpeed;
            wishDirection.y = rb.velocity.y; // Maintain vertical velocity
            rb.velocity = wishDirection;
        }
    }

    void HandleJetpack()
    {
        if (Input.GetMouseButton(1) && currentJetpackFuel > 0)
        {
            rb.AddForce(Vector3.up * jetpackForce, ForceMode.Acceleration);
            currentJetpackFuel -= Time.deltaTime * jetpackFuelUsageRate;
            currentJetpackFuel = Mathf.Clamp(currentJetpackFuel, 0, jetpackFuelMax);
        }

        if (currentJetpackFuel < jetpackFuelMax)
        {
            currentJetpackFuel += Time.deltaTime * jetpackFuelRegenRate;
            currentJetpackFuel = Mathf.Clamp(currentJetpackFuel, 0, jetpackFuelMax);
        }

        if (currentJetpackFuel < 0.02f)
        {
            currentJetpackFuel = 0;
        }
    }

    void Slide()
    {
        bool isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.1f);

        if (Input.GetKey(KeyCode.LeftShift))
        {
            isSliding = true;
            rb.drag = 0; // Zresetuj tarcie, aby nie było tarcia podczas ślizgania się
        }
        else
        {
            isSliding = false;
            rb.drag = 1; // Ustaw standardowe tarcie, gdy nie ma ślizgania się
        }

        if (isSliding)
        {
            Vector3 slideDirection = rb.velocity.normalized;
            float slideSpeed = rb.velocity.magnitude * slideSpeedFactor;
            rb.velocity = slideDirection * slideSpeed;
        }
    }

    float GetPlayerSpeed()
    {
        float speed = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
        return speed;
    }
}