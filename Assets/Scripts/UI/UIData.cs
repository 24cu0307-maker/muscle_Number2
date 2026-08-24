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

    public void SetSize()
    {
        m_currentFrameSuccess.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
        m_currentFrameApproaching.transform.localScale = new Vector3(0.17f, 0.17f, 0.17f);
        m_currentFrameFailure.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
        m_currentFrameWating.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
    }




}

public class UIData : MonoBehaviour
{
    [SerializeField] private List<FlameBase> ui;

    private void Awake()
    {
        foreach (var ui in ui) ui.SetActive(false);

        foreach (var ui in ui) ui.SetSize();
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
