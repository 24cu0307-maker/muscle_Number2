using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>
///ƒXƒPƒ‹ƒgƒ“î•ñ‚ğ•Û‘¶
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

}
