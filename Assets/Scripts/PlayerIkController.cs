using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIkController : MonoBehaviour
{
    [SerializeField] Transform handLeftIk = default;
    [SerializeField] Transform handRightIk = default;

    [SerializeField] Transform headTarget = default;
    public bool isActive;
    Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }
    private void OnAnimatorIK(int layerIndex)
    {
        if (isActive)
        {
            if (headTarget != null)
            {
                anim.SetLookAtWeight(1f);
                anim.SetLookAtPosition(headTarget.position);
            }

            if (handLeftIk != null)
            {
                anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
                anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);

                anim.SetIKPosition(AvatarIKGoal.LeftHand, handLeftIk.position);
                anim.SetIKRotation(AvatarIKGoal.LeftHand, handLeftIk.rotation);
            }

            if (handRightIk)
            {

            }
        }
        else
        {
            anim.SetLookAtWeight(0f);
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        }
    }
}
