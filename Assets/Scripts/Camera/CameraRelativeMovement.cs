using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRelativeMovement
{

    [Header("判定")]
    [SerializeField] private bool direction = false;

    public bool CameraDirection(GameObject camera_obj)
    {
      
        //基準
        direction = false;

        //判定
        if (camera_obj.transform.eulerAngles.y <= -120 || camera_obj.transform.eulerAngles.y >= 90)
        {
            direction = true;
        }

        //判定を返す
        return direction;
    }

}
