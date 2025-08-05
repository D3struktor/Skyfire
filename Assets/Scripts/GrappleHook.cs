using UnityEngine;
using Photon.Pun;

public class GrappleHook : MonoBehaviourPun
{
    private int ownerViewID;
    private bool attached = false;

    private float maxRange = 60f;
    private Vector3 launchPosition;

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
            PhotonNetwork.Destroy(gameObject); // Auto-destrukcja jeśli za daleko
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (attached) return;
        attached = true;

        if (photonView.IsMine)
        {
            photonView.RPC("NotifyAttachHook", RpcTarget.All, transform.position);
        }

        FreezeHook();
    }

    [PunRPC]
    void NotifyAttachHook(Vector3 point)
    {
        PhotonView ownerView = PhotonView.Find(ownerViewID);
        if (ownerView != null)
        {
            GrappleGun gun = ownerView.GetComponentInChildren<GrappleGun>();
            if (gun != null)
            {
                gun.AttachHook(point);
            }
        }
    }

    void FreezeHook()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }
}
