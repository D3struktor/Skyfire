using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideControl : MonoBehaviour {

    public Camera cam;
    public float Speed = 1f;
    public float RotatingSpeed = 1f;
    public float AdditionalGravity = 0.5f;
    public float LandingAccelerationRatio = 0.5f;

    public bool reverse = false;

    [HideInInspector] public Rigidbody rb;
    float height;

    public bool aerial;

    [HideInInspector] public Quaternion PhysicsRotation;
    [HideInInspector] public Quaternion VelocityRotation;
    [HideInInspector] public Quaternion InputRotation;
    [HideInInspector] public Quaternion ComputedRotation;

    // Use this for initialization
    void Start() {
        Initialization();
    }

    // Update is called once per frame
    void Update() {
        CheckPhysics();

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
            Vector2 direction = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            SliderMove(direction);
        }
    }

    void CheckPhysics() {
        Ray ray = new Ray(transform.position, -transform.up);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1.05f * height)) {
            if (aerial) {
                VelocityOnLanding();
            }
            aerial = false;
        } else {
            aerial = true;
            rb.velocity += Vector3.down * AdditionalGravity;
        }
    }

    void VelocityOnLanding() {
        float magn_vel = rb.velocity.magnitude;
        Vector3 new_vel = rb.velocity;
        new_vel.y = 0;
        new_vel = new_vel.normalized * magn_vel;

        rb.velocity += LandingAccelerationRatio * new_vel;
    }

    void SliderMove(Vector2 inputs) {
        PhysicsRotation = aerial ? Quaternion.identity : GetPhysicsRotation(); // Rotation according to ground normal
        VelocityRotation = GetVelocityRot();
        InputRotation = Quaternion.identity;
        ComputedRotation = Quaternion.identity;

        if (inputs.magnitude > 0.1f) {
            Vector3 adapted_direction = CamToPlayer(inputs);
            Vector3 planar_direction = transform.forward;
            planar_direction.y = 0;
            InputRotation = Quaternion.FromToRotation(planar_direction, adapted_direction);

            if (!aerial) {
                Vector3 Direction = InputRotation * transform.forward * Speed;
                rb.AddForce(Direction);
            }
        }

        ComputedRotation = PhysicsRotation * VelocityRotation * transform.rotation;
        transform.rotation = Quaternion.Lerp(transform.rotation, ComputedRotation, RotatingSpeed * Time.deltaTime);
    }

    Quaternion GetVelocityRot() {
        Vector3 vel = rb.velocity;
        if (vel.magnitude > 0.2f) {
            vel.y = 0;
            Vector3 dir = transform.forward;
            dir.y = 0;
            Quaternion vel_rot = Quaternion.FromToRotation(dir.normalized, vel.normalized);
            return vel_rot;
        } else
            return Quaternion.identity;
    }

    Quaternion GetPhysicsRotation() {
        Vector3 target_vec = Vector3.up;
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1.05f * height)) {
            target_vec = hit.normal;
        }

        return Quaternion.FromToRotation(transform.up, target_vec);
    }

    Vector3 CamToPlayer(Vector2 d) {
        Vector3 direction = cam.transform.forward * d.y + cam.transform.right * d.x;
        direction.y = 0; // Ensure movement is horizontal
        return direction.normalized;
    }

    void Initialization() {
        rb = GetComponent<Rigidbody>();
        height = GetComponent<Collider>().bounds.size.y / 2f;
    }
}
