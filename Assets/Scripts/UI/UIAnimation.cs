using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAnimation
{
    public void ScaleDown(GameObject ui)
    {
        ui.transform.localScale -= Vector3.one * Time.deltaTime * 0.15f;
    }

    public void ScaleReset(GameObject ui)
    {
        ui.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
    }
}
