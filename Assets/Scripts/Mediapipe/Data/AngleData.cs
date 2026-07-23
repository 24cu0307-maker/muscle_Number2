using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngleData
{
    ///<summary>
    ///‘Ì‚ÌŠp“x‚ðŠi”[‚·‚é” 
    ///</summary>
    public float[] angle { get; private set; } = new float[4];


    ///<summary>
    ///‘Ì‚ÌŠp“x‚ðŠi”[
    ///</summary>
    public void SetAngle(int angleNumber,float _angle)
    {
        angle[angleNumber] = _angle;
    }
}
