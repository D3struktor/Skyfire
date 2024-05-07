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
    [SerializeField] float slideSpeedFactor = 1.0f; // Współczynnik prędkości podczas ślizgania się na ziemi
    [SerializeField] float slideAirborneFactor = 0.95f; // Współczynnik przyspieszenia spadania w powietrzu
    [SerializeField] float slideFrictionFactor = 5.0f; // Współczynnik zmniejszający tarcie podczas ślizgania się

    private float currentJetpackFuel;
    public Text jetpackFuelText;

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
        Camera.main.transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
    }

    void Movement()
    {
        Vector2 axis = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal")) * walkSpeed;
        Vector3 forward = new Vector3(-Camera.main.transform.right.z, 0.0f, Camera.main.transform.right.x);
        Vector3 wishDirection = (forward * axis.x + Camera.main.transform.right * axis.y + Vector3.up * rb.velocity.y);
        rb.velocity = wishDirection;
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

        if (isGrounded)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                // Zastosuj efekt ślizgania się na ziemi
                Vector3 slideDirection = rb.velocity.normalized;
                float slideSpeed = rb.velocity.magnitude * slideSpeedFactor;
                rb.velocity = slideDirection * slideSpeed;

                // Zmniejsz tarcie wzdłuż płaszczyzny, po której się ślizga
                rb.AddForce(-rb.velocity.normalized * slideFrictionFactor, ForceMode.Acceleration);
            }
            else
            {
                // Gracz chodzi normalnie
                Movement();
            }
        }
        else
        {
            // Jeśli gracz jest w powietrzu, przyspiesz spadanie
            rb.velocity += Vector3.down * (1.0f - slideAirborneFactor) * Time.deltaTime;
        }
    }
}
