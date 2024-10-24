using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FpsController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 10f;
    [SerializeField] float CameraSens = 5f;
    [SerializeField] float clampRotation = 80f;
    [SerializeField] Transform Cam = null;
    [SerializeField] Transform GroundCheckPos = null;
    [SerializeField] LayerMask Groundlayer = new LayerMask();
    [SerializeField] Transform headPos = default;

    [Header("Jump Settings")]
    [SerializeField] float JumpForce = 5f;
    [Space(20)]
    FpsControllerInput fpsControllerInput;
    Rigidbody rb;
    float mousex;
    public bool IsGrounded;
    [Range(.001f, 1f)]
    [SerializeField] float groundCheckRadius = .1f;

    FpsAnimationController animationController;
    bool Jump;

    private void Awake()
    {
        fpsControllerInput = GetComponent<FpsControllerInput>();
        animationController = GetComponent<FpsAnimationController>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        LookAround();
        if (fpsControllerInput.Jump() && isGrounded())
        {
            Jump = true;
        }
    }

    private void FixedUpdate()
    {
        IsGrounded = isGrounded();
        Movement();
        jump();
    }

    private void Movement()
    {
        if (fpsControllerInput.GetMoveAxis().magnitude != 0)
        {
            rb.velocity = transform.forward * fpsControllerInput.GetMoveAxis().y * moveSpeed + transform.right * fpsControllerInput.GetMoveAxis().x * moveSpeed;
        }
        else
        {
            rb.velocity = Vector3.up * rb.velocity.y;
        }

        animationController.PlayerAnimation(fpsControllerInput.GetMoveAxis());
    }

    void LookAround()
    {
        Cam.position = headPos.position;
        mousex -= fpsControllerInput.GetMouseAxis().y;
        Cam.localRotation = Quaternion.Euler(Mathf.Clamp(mousex * CameraSens, -clampRotation, clampRotation - 8f), 0, 0);

        transform.Rotate(transform.up, fpsControllerInput.GetMouseAxis().x * CameraSens);
    }

    void jump()
    {
        if (Jump)
            applyJump();
    }

    void applyJump()
    {
        rb.AddForce(transform.up * JumpForce * 100f, ForceMode.Impulse);
        Jump = false;
    }

    bool isGrounded()
    {
        return Physics.CheckSphere(GroundCheckPos.position, groundCheckRadius, Groundlayer);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(GroundCheckPos.position, groundCheckRadius);
    }
}
