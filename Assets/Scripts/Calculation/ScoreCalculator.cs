using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreCalculator
{
    /*
    public float a = 5000;
    public float b = 29;
    public float c = 0.2929f;

    public float d = 5.23f;
    public float e = 15.23f;

    float total = a - b(c + d + e);
    */

    ///<summary>
    ///スコア計算
    ///</summary>
    public float TotalScore(params float[] angles)
    {
        const float maxScore = 10000;
        const float constant_1 = 29;
        const float constant_2 = 0.2929f;

        float totalAngle = 0;

        foreach (float angle in angles)
        {
            totalAngle += angle;
        }

        float totalScore = maxScore - constant_1 * (constant_2 + totalAngle);

        return totalScore;
    }
}
