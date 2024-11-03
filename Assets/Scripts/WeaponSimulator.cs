using UnityEngine;
using Photon.Pun;

public class WeaponSimulator : MonoBehaviourPun
{
    public Transform weaponTransform; // Transform broni
    public Transform leftHand; // Transform lewej ręki
    public Transform rightHand; // Transform prawej ręki
    public Transform playerCamera; // Transform kamery gracza

    public Vector3 weaponOffset = new Vector3(0.35f, -0.4f, 0.3f); // Przykładowy offset dla broni
    public float smoothFactor = 0.5f; // Jak szybko broń i ręce będą podążać za symulacją

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (photonView.IsMine)
        {
            // Ukryj ręce dla lokalnego gracza
            leftHand.gameObject.SetActive(false);
            rightHand.gameObject.SetActive(false);
        }
        else
        {
            // Włącz ręce tylko dla graczy zdalnych
            leftHand.gameObject.SetActive(true);
            rightHand.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        // Removed hand movement code to disable hand motions
    }

    [PunRPC]
    void SyncHandPositions(Vector3 leftHandPosition, Vector3 rightHandPosition)
    {
        // Removed hand position sync code for other players
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || photonView.IsMine) return;

        // Ustawienia IK dla graczy zdalnych
        if (leftHand != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHand.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHand.rotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
        }

        if (rightHand != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHand.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHand.rotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
        }
    }
}
