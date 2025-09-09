using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView), typeof(Rigidbody), typeof(Collider))]
public class GrappleHook : MonoBehaviourPun
{
    private int ownerViewID;
    private bool attached = false;
    private float maxRange = 60f;
    private Vector3 launchPosition;
    private Transform hookedObject;

    [Tooltip("Layers the hook should NOT latch onto (e.g., Player, Projectiles)")]
    public LayerMask ignoreLayers;

    public void Init(int viewID, float maxDistance)
    {
        ownerViewID = viewID;
        maxRange = maxDistance;
        launchPosition = transform.position;
    }

    void Update()
    {
        if (!attached && Vector3.Distance(transform.position, launchPosition) > maxRange)
        {
            if (photonView.IsMine) PhotonNetwork.Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (attached) return;

        // Collision filter (equivalent of ShouldHit)
        if (((1 << collision.gameObject.layer) & ignoreLayers) != 0)
            return;

        attached = true;

        // Attach to a dynamic object (equivalent of SetBase)
        hookedObject = collision.transform;
        transform.SetParent(hookedObject, true);

        // Freeze the hook locally
        FreezeHook();

        // Inform EVERYONE (buffered) about the latch point and object ID
        int targetId = -1;
        var targetPv = hookedObject.GetComponentInParent<PhotonView>();
        if (targetPv != null) targetId = targetPv.ViewID;

        photonView.RPC(nameof(RPC_Latched), RpcTarget.AllBuffered, transform.position, targetId, ownerViewID);
    }

    [PunRPC]
    void RPC_Latched(Vector3 point, int attachedToID, int shooterViewID)
    {
        // Set the parent for other clients as well (if the object has a PhotonView)
        if (attachedToID != -1)
        {
            var target = PhotonView.Find(attachedToID);
            if (target != null)
                transform.SetParent(target.transform, true);
        }

        // Find the owner's grappler and notify it about the latch.
        var shooter = PhotonView.Find(shooterViewID);
        if (shooter != null)
        {
            var gun = shooter.GetComponentInChildren<GrappleGun>();
            if (gun != null)
                gun.OnHookLatched(point); // Physics only on owner; others only rope FX
        }
    }

    void FreezeHook()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
    }

    // Call ONLY by the owner
    public void Release()
    {
        transform.SetParent(null);
        if (photonView.IsMine) PhotonNetwork.Destroy(gameObject);
    }
}
