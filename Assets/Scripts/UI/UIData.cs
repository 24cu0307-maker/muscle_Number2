using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct FlameBase
{
    public GameObject m_currentFrameSuccess;
    public GameObject m_currentFrameApproaching;
    public GameObject m_currentFrameFailure;
    public GameObject m_currentFrameWating;

    public void SetActive(bool b)
    {
        m_currentFrameSuccess.SetActive(b);
        m_currentFrameApproaching.SetActive(b);
        m_currentFrameFailure.SetActive(b);
        m_currentFrameWating.SetActive(b);
    }
}

public class UIData : MonoBehaviour
{
    [SerializeField] private List<FlameBase> ui;

    private void Awake()
    {
        foreach (var ui in ui) ui.SetActive(false);


    }

    public GameObject getUI(string _name, int _number)
    {
        if (_name == "Failure")
        {
            return ui[_number].m_currentFrameFailure;

        }
        switch (_name)
        {
            case "Success":
                return ui[_number].m_currentFrameSuccess;


            case "Approaching":
                return ui[_number].m_currentFrameApproaching;

            case "Failure":
                return ui[_number].m_currentFrameFailure;

            case "Wating":
                return ui[_number].m_currentFrameWating;

            default: return null;
        }

    }
}
