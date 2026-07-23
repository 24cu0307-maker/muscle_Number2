using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPresenter 
{
    public void Show(GameObject ui)
    {
        ui.SetActive(true);
    }

    public void Hide(GameObject ui)
    {
        ui.SetActive(false);
    }
}
