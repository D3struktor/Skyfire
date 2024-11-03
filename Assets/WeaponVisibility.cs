using Photon.Pun;
using UnityEngine;

public class WeaponVisibility : MonoBehaviour
{
    private PhotonView photonView;

    void Start()
    {
        photonView = GetComponentInParent<PhotonView>();

        // Check if this object is controlled by the local player
        if (photonView.IsMine)
        {
            // Hide the weapon for the local player
            SetWeaponVisibility(false);
        }
        else
        {
            // Show the weapon for other players
            SetWeaponVisibility(true);
        }
    }

    // Function to set visibility
    private void SetWeaponVisibility(bool isVisible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = isVisible;
        }
    }
}
