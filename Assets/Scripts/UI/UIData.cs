using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIData : MonoBehaviour
{
    [SerializeField] private GameObject[] ui;

    private void Awake()
    {
        foreach (GameObject _ui in ui)
        {
            _ui.SetActive(false);
        }
    }

    public GameObject getUI(int _uiNumber)
    { 
        return ui[_uiNumber];
    }
}
