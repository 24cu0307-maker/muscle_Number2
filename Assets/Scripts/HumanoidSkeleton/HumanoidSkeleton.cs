using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>
///スケルトン情報を保存
///</summary>
public class HumanoidSkeleton : MonoBehaviour
{
    [SerializeField] public Transform[] playerLeftHand = new Transform[21];

    [SerializeField] public Transform[] playerRightHand = new Transform[21];

    [SerializeField] public Transform[] playerLeftArm = new Transform[2];

    [SerializeField] public Transform[] playerRightArm = new Transform[2];

    [SerializeField] public Transform[] playerSpline = new Transform[4];

    [SerializeField] public Transform[] playerLeftLeg = new Transform[2];

    [SerializeField] public Transform[] playerRightLeg = new Transform[2];

    // 初期回転を保存
    private Quaternion[] initialLeftHandRotation = new Quaternion[21];
    private Quaternion[] initialRightHandRotation = new Quaternion[21];

    private Quaternion[] initialLeftArmRotation = new Quaternion[2];
    private Quaternion[] initialRightArmRotation = new Quaternion[2];

    private Quaternion[] initialSplineRotation = new Quaternion[4];

    private Quaternion[] initialLeftLegRotation = new Quaternion[2];
    private Quaternion[] initialRightLegRotation = new Quaternion[2];

    public Quaternion[] GetInitialLeftHandRotation()
    {
        return initialLeftHandRotation;
    }

    public Quaternion[] GetInitialRightHandRotation()
    {
        return initialRightHandRotation;
    }

    public Quaternion[] GetInitialLeftArmRotation()
    {
        return initialLeftArmRotation;
    }

    public Quaternion[] GetInitialRightArmRotation()
    {
        return initialRightArmRotation;
    }

    public Quaternion[] GetInitialSplineRotation()
    {
        return initialSplineRotation;
    }

    public Quaternion[] GetInitialLeftLegRotation()
    {
        return initialLeftLegRotation;
    }

    public Quaternion[] GetInitialRightLegRotation()
    {
        return initialRightLegRotation;
    }
    private void Awake()
    {
        Set();
    }

    private void Set()
    {
        // 左手
        for (int i = 0; i < playerLeftHand.Length; i++)
        {
            if (playerLeftHand[i] != null)
                initialLeftHandRotation[i] = playerLeftHand[i].localRotation;
        }

        // 右手
        for (int i = 0; i < playerRightHand.Length; i++)
        {
            if (playerRightHand[i] != null)
                initialRightHandRotation[i] = playerRightHand[i].localRotation;
        }

        // 左腕
        for (int i = 0; i < playerLeftArm.Length; i++)
        {
            if (playerLeftArm[i] != null)
                initialLeftArmRotation[i] = playerLeftArm[i].localRotation;
        }

        // 右腕
        for (int i = 0; i < playerRightArm.Length; i++)
        {
            if (playerRightArm[i] != null)
                initialRightArmRotation[i] = playerRightArm[i].localRotation;
        }

        // Spline
        for (int i = 0; i < playerSpline.Length; i++)
        {
            if (playerSpline[i] != null)
                initialSplineRotation[i] = playerSpline[i].localRotation;
        }

        // 左足
        for (int i = 0; i < playerLeftLeg.Length; i++)
        {
            if (playerLeftLeg[i] != null)
                initialLeftLegRotation[i] = playerLeftLeg[i].localRotation;
        }

        // 右足
        for (int i = 0; i < playerRightLeg.Length; i++)
        {
            if (playerRightLeg[i] != null)
                initialRightLegRotation[i] = playerRightLeg[i].localRotation;
        }
    }
}
