using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionVectorCalculator 
{
    [Header("方向")]
    [SerializeField] private Vector3 _Dir;

    ///<summary>
    ///方向を計算
    ///</summary>
    public Vector3 Vector(Vector3 a, Vector3 b)
    {
        _Dir = a - b;
        return _Dir;
    }

    ///<summary>
    ///反対方向を計算
    ///</summary>
    public Vector3 MirroredVector_X(Vector3 a, Vector3 b)
    {
        _Dir = new Vector3(-(a.x - b.x), a.y - b.y, a.z - b.z); ;
        return _Dir;
    }
}
