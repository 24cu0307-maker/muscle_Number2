using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseJudge
{
    //ポーズのタイミングが判定
    public bool PoseJudge_Perfect(GameObject UI_approaching, GameObject UI_wating)
    {
        return UI_wating.transform.localScale.x >= UI_approaching.transform.localScale.x - 0.1f && UI_wating.transform.localScale.x <= UI_approaching.transform.localScale.x + 0.1f;
    }

}
