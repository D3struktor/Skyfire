using UnityEngine;
using Photon.Pun;

public class GrappleGun : MonoBehaviourPun
{
    [Header("Prefabs and comps")]
    public GameObject hookPrefab;
    public Transform firePoint;
    public LineRenderer lineRenderer;
    public Material ropeMaterial;
    public Rigidbody playerRb;

    [Header("Hook Param")]
    public float hookSpeed = 200f; // Szybszy hak
    public float maxHookDistance = 80f; // Ograniczenie zasięgu
    public float pullForce = 25f;
    public float shortenSpeed = 20f;

    [Header("Elastic Pendulum")]
    public float stretchForceMultiplier = 4f; // Im bardziej napięta linka, tym mocniejsze przyciąganie
    public float forceClamp = 150f;

    [Header("GroundCheck")]
    public float groundCheckDistance = 1.2f;
    public LayerMask groundLayerMask = -1;

    [Header("Sounds")]
    public AudioClip hookFailSound;
    public AudioSource audioSource; // Podłącz AudioSource do broni


    // --- Stan wewnętrzny ---
    private GameObject currentHook;
    private SpringJoint joint;
    private Vector3 hookPoint;
    private bool isGrappling = false;
    private bool hookAttached = false;
    private Camera cam;

    // --- Obsługa kliknięć ---
    private bool hasFired = false;
    private bool awaitingInput = false;
    private float mouseDownTime;
    private float clickThreshold = 0.2f;

    void Start()
    {
        if (!photonView.IsMine) return;

        cam = Camera.main;

        if (playerRb == null)
        {
            playerRb = GetComponentInParent<Rigidbody>();
        }

        if (playerRb == null)
        {
            Debug.LogError("❌ Brakuje Rigidbody na graczu! Podłącz go ręcznie.");
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        if (Input.GetMouseButtonDown(0))
        {
            mouseDownTime = Time.time;

            if (!hookAttached && !hasFired)
            {
                hasFired = true;
                awaitingInput = false;

                Vector3 origin = firePoint.position;
                Vector3 dir = cam.transform.forward;

                photonView.RPC("ShootHook", RpcTarget.All, origin, dir);
            }
        }

        if (hasFired && !awaitingInput && Input.GetMouseButtonUp(0))
        {
            awaitingInput = true;
        }

        if (hookAttached && awaitingInput && Input.GetMouseButtonUp(0))
        {
            float heldTime = Time.time - mouseDownTime;

            if (heldTime < clickThreshold)
            {
                photonView.RPC("ReleaseHook", RpcTarget.All);
                ResetStates();
            }
        }
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine || !isGrappling || hookPoint == Vector3.zero || playerRb == null || joint == null)
            return;

        // Zwinięcie linki przy trzymaniu
        if (Input.GetMouseButton(0) && awaitingInput)
        {
            joint.maxDistance = Mathf.Max(joint.minDistance + 1f, joint.maxDistance - shortenSpeed * Time.fixedDeltaTime);
        }

        Vector3 toHook = hookPoint - playerRb.position;
        float currentDistance = toHook.magnitude;

        Vector3 groundCheckOrigin = playerRb.position + Vector3.up * 0.1f;
        bool isGrounded = Physics.Raycast(groundCheckOrigin, Vector3.down, groundCheckDistance, groundLayerMask);

        float verticalFactor = Mathf.Clamp01(toHook.normalized.y);
        float forceMultiplier = isGrounded ? 0.3f : 1f;

        // Dynamiczna siła: im bardziej napięta linka, tym mocniej ciągnie
        float stretchRatio = Mathf.Clamp01((currentDistance - joint.maxDistance) / joint.maxDistance);
        float dynamicForce = pullForce * (1f + stretchRatio * stretchForceMultiplier);
        dynamicForce = Mathf.Clamp(dynamicForce, 0f, forceClamp);

        playerRb.AddForce(toHook.normalized * dynamicForce * forceMultiplier * (1f + verticalFactor), ForceMode.Acceleration);
    }

    [PunRPC]
    void ShootHook(Vector3 position, Vector3 direction)
    {
        if (hookPrefab == null)
        {
            Debug.LogError("❌ Brakuje hookPrefab!");
            return;
        }

        if (currentHook != null)
        {
            PhotonNetwork.Destroy(currentHook);
        }

        currentHook = PhotonNetwork.Instantiate(hookPrefab.name, position, Quaternion.LookRotation(direction));
        if (photonView.IsMine)
        {
            StartCoroutine(HookFailTimeout());
        }


        Rigidbody rb = currentHook.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("❌ Hook prefab nie ma Rigidbody.");
            return;
        }
        Vector3 biasedDirection = (direction.normalized + Vector3.down * 0.15f).normalized;
        rb.useGravity = true; 
        rb.AddForce(direction.normalized * hookSpeed, ForceMode.Impulse);


        // Ignorowanie kolizji z graczem
        Collider[] playerColliders = GetComponentsInChildren<Collider>();
        Collider hookCollider = currentHook.GetComponent<Collider>();
        foreach (var col in playerColliders)
        {
            if (hookCollider != null && col != null)
                Physics.IgnoreCollision(hookCollider, col);
        }

        GrappleHook hookScript = currentHook.GetComponent<GrappleHook>();
        if (hookScript == null)
        {
            Debug.LogError("❌ Brak skryptu GrappleHook.");
            return;
        }

        hookScript.Init(photonView.ViewID, maxHookDistance);
        hookAttached = true;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.material = ropeMaterial;
        }
    }

    public void AttachHook(Vector3 point)
    {
        isGrappling = true;
        hookPoint = point;

        joint = playerRb.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = hookPoint;

        float distance = Vector3.Distance(playerRb.position, hookPoint);
        joint.maxDistance = distance;
        joint.minDistance = distance * 0.1f;
        joint.spring = 0f;
        joint.damper = 2f;
        joint.massScale = 4.5f;
    }

    [PunRPC]
    void ReleaseHook()
    {
        isGrappling = false;
        hookAttached = false;
        hookPoint = Vector3.zero;

        if (joint) Destroy(joint);
        if (currentHook) PhotonNetwork.Destroy(currentHook);
        if (lineRenderer != null) lineRenderer.positionCount = 0;

        ResetStates();
    }

    void LateUpdate()
    {
        if (currentHook != null && hookAttached)
        {
            DrawRope();
        }
        else
        {
            if (lineRenderer != null)
                lineRenderer.positionCount = 0;
        }
    }

    void DrawRope()
    {
        if (lineRenderer == null || currentHook == null) return;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, firePoint.position);
        lineRenderer.SetPosition(1, currentHook.transform.position);

        float ropeLength = Vector3.Distance(firePoint.position, currentHook.transform.position);
        lineRenderer.material.mainTextureScale = new Vector2(ropeLength, 1);
    }

    void ResetStates()
    {
        hasFired = false;
        awaitingInput = false;
    }
        
    System.Collections.IEnumerator HookFailTimeout()
    {
        float timeout = maxHookDistance / hookSpeed + 0.5f; // zapas
        yield return new WaitForSeconds(timeout);

        if (!isGrappling)
        {
            if (audioSource != null && hookFailSound != null)
            {
                audioSource.PlayOneShot(hookFailSound);
            }

            ResetStates();
            hookAttached = false;

            if (currentHook != null)
            {
                PhotonNetwork.Destroy(currentHook);
            }
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
            }
        }
    }

}
