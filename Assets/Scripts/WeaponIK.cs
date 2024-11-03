using UnityEngine;

public class WeaponIK : MonoBehaviour
{
    public Transform leftHandTarget; // Punkt docelowy dla lewej ręki
    public Transform rightHandTarget; // Punkt docelowy dla prawej ręki
    private Animator animator;
    private Camera playerCamera;

    void Start()
    {
        animator = GetComponent<Animator>();

        // Znalezienie kamery gracza
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("Player camera not found. Make sure a camera is tagged as MainCamera.");
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        // Ustawianie pozycji i rotacji dłoni tylko dla lokalnego gracza
        if (leftHandTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
        }

        if (rightHandTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
        }
    }

    public void ChangeWeapon(Transform weapon)
    {
        if (weapon == null)
        {
            Debug.LogError("Weapon is null. Cannot update hand targets.");
            return;
        }

        // Szukaj LeftHandTarget i RightHandTarget w nowej broni lub w kamerze gracza
        leftHandTarget = weapon.Find("LeftHandTarget") ?? playerCamera.transform.Find("LeftHandTarget");
        rightHandTarget = weapon.Find("RightHandTarget") ?? playerCamera.transform.Find("RightHandTarget");

        if (leftHandTarget == null)
        {
            Debug.LogWarning("LeftHandTarget not found for weapon: " + weapon.name);
        }
        if (rightHandTarget == null)
        {
            Debug.LogWarning("RightHandTarget not found for weapon: " + weapon.name);
        }
    }
}
