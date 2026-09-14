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

    [Header("位置")]
    public Vector2 position;

    [Header("サイズ")]
    public Vector3 size;

    public void SetActive(bool b)
    {
        m_currentFrameSuccess.SetActive(b);
        m_currentFrameApproaching.SetActive(b);
        m_currentFrameFailure.SetActive(b);
        m_currentFrameWating.SetActive(b);
    }
    /*
    public void SetSize(Vector3 _position)
    {
        m_currentFrameSuccess.transform.localScale = _position;
        m_currentFrameApproaching.transform.localScale = _position + new Vector3(0.05f, 0.05f, 0.05f);
        m_currentFrameFailure.transform.localScale = _position;
        m_currentFrameWating.transform.localScale = _position;
    }
    */

    public void SetTransform()
    {
        SetPosition(m_currentFrameSuccess);
        SetPosition(m_currentFrameApproaching);
        SetPosition(m_currentFrameFailure);
        SetPosition(m_currentFrameWating);

        if (m_currentFrameSuccess != null)
            m_currentFrameSuccess.transform.localScale = size;

        if (m_currentFrameApproaching != null)
            m_currentFrameApproaching.transform.localScale =
                size + new Vector3(0.05f, 0.05f, 0.05f);

        if (m_currentFrameFailure != null)
            m_currentFrameFailure.transform.localScale = size;

        if (m_currentFrameWating != null)
            m_currentFrameWating.transform.localScale = size;
    }


    private void SetPosition(GameObject obj)
    {
        if (obj == null) return;

        RectTransform rect = obj.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(position.x, position.y);
        }
    }


}

public class UIData : MonoBehaviour
{
    [SerializeField] private List<FlameBase> ui;

    private void Awake()
    {
        foreach (var flame in ui)
        {
            flame.SetActive(false);
            flame.SetTransform();
        }
    }

    public Vector2 Position()
    {
        return new Vector2();
    }

    public GameObject getUI(string _name, int _number)
    {
        if (ui == null || _number < 0 || _number >= ui.Count)
        {
            return null;
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

            default:
                return null;
        }
    }

    public bool TryGetApproachingFrame(
        int _poseId,
        out GameObject _frame)
    {
        _frame = null;

        if (ui == null || _poseId < 0 || _poseId >= ui.Count)
            return false;

        _frame = ui[_poseId].m_currentFrameApproaching;

        return _frame != null;
    }

    public Vector2 setUINumber9()
    {

        return ui[9].position;
    }
}