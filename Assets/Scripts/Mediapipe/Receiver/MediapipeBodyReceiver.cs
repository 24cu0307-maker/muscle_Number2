using Mediapipe;
using Mediapipe.Unity.Sample.Holistic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MediapipeBodyReceiver : MonoBehaviour
{
    private const int ERequiredBodyLandmarkCount = 33;

    //オブザーバー
    private void OnEnable()
    {
        HolisticTrackingSolution.OnBodyUpdated += OnBody;
    }

    //オブザーバー
    private void OnDisable()
    {
        HolisticTrackingSolution.OnBodyUpdated -= OnBody;
    }


    //３３点のすべての座標を取得
    private void OnBody(LandmarkList result)
    {
        //データは入っているか
        if (result == null)
            return;
        if (result.Landmark == null)
            return;
        if (result.Landmark.Count < ERequiredBodyLandmarkCount)
            return;
        if (PositionDataManager.Instance == null
            || PositionDataManager.Instance.positionData == null)
            return;

        //座標を格納する箱
        Vector3[] Body = new Vector3[37];

        for (int i = 0; i < ERequiredBodyLandmarkCount; ++i)
        {

            //座標を受け取り
            Landmark point = result.Landmark[i];

            Vector3 bodyPoint = new Vector3(point.X, point.Y * -1, point.Z);
            if (!IsFinite(bodyPoint))return;

            // World Landmarkは腰付近を原点とするため、原点に近い点も有効値として保存する
            Body[i] = bodyPoint;


            
        }

        //データを保存
        PositionDataManager.Instance.positionData.SetBodyPosition(Body);

    }

    private static bool IsFinite(Vector3 _point)
    {
        return !float.IsNaN(_point.x)
            && !float.IsNaN(_point.y)
            && !float.IsNaN(_point.z)
            && !float.IsInfinity(_point.x)
            && !float.IsInfinity(_point.y)
            && !float.IsInfinity(_point.z);
    }

}
