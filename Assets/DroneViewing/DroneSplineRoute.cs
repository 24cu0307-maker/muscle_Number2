using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一台のDrone Objectが使用する独立したSpline経路です。
/// </summary>
public sealed class DroneSplineRoute : MonoBehaviour
{
    private const int ESamplesPerSegment = 24;

    [SerializeField, Min(0.01f)] private float m_speed = 5.0f;
    [SerializeField] private bool b_m_loop = true;
    [SerializeField] private bool b_m_showSpline = true;
    [SerializeField] private Vector3[] m_points =
    {
        new Vector3(-12.0f, 8.0f, -12.0f),
        new Vector3(-12.0f, 10.0f, 12.0f),
        new Vector3(12.0f, 8.0f, 12.0f),
        new Vector3(12.0f, 10.0f, -12.0f)
    };

    private readonly List<Vector3> m_samplePositions = new List<Vector3>();
    private readonly List<float> m_sampleDistances = new List<float>();
    private float m_distance;
    private float m_totalDistance;
    private bool b_m_cacheDirty = true;

    public Vector3[] Points => m_points;
    public bool ShowSpline => b_m_showSpline;

    public void Configure(Vector3[] _points, float _speed, bool _loop)
    {
        m_points = _points;
        m_speed = Mathf.Max(0.01f, _speed);
        b_m_loop = _loop;
        b_m_cacheDirty = true;
        m_distance = 0.0f;
    }

    public void Advance(float _deltaTime)
    {
        if (b_m_cacheDirty)RebuildCache();
        if (m_totalDistance <= Mathf.Epsilon)return;

        m_distance += m_speed * _deltaTime;
        m_distance = b_m_loop
            ? Mathf.Repeat(m_distance, m_totalDistance)
            : Mathf.Min(m_distance, m_totalDistance);
        transform.localPosition = GetPositionAtDistance(m_distance);
    }

    public Vector3 EvaluateSegment(int _segmentIndex, float _time)
    {
        if (m_points == null || m_points.Length < 2)return Vector3.zero;

        int pointCount = m_points.Length;
        int currentIndex = Mathf.Clamp(_segmentIndex, 0, pointCount - 1);
        int nextIndex = GetPointIndex(currentIndex + 1);
        int previousIndex = GetPointIndex(currentIndex - 1);
        int followingIndex = GetPointIndex(currentIndex + 2);
        Vector3 previous = m_points[previousIndex];
        Vector3 current = m_points[currentIndex];
        Vector3 next = m_points[nextIndex];
        Vector3 following = m_points[followingIndex];
        float timeSquared = _time * _time;
        float timeCubed = timeSquared * _time;
        return 0.5f * (
            2.0f * current
            + (-previous + next) * _time
            + (2.0f * previous - 5.0f * current
                + 4.0f * next - following) * timeSquared
            + (-previous + 3.0f * current
                - 3.0f * next + following) * timeCubed);
    }

    public int GetSegmentCount()
    {
        if (m_points == null || m_points.Length < 2)return 0;
        return b_m_loop ? m_points.Length : m_points.Length - 1;
    }

    private void RebuildCache()
    {
        b_m_cacheDirty = false;
        m_samplePositions.Clear();
        m_sampleDistances.Clear();
        m_totalDistance = 0.0f;
        int segmentCount = GetSegmentCount();
        if (segmentCount == 0)return;

        Vector3 previous = EvaluateSegment(0, 0.0f);
        m_samplePositions.Add(previous);
        m_sampleDistances.Add(0.0f);
        for (int i = 0; i < segmentCount; ++i)
        {
            for (int j = 1; j <= ESamplesPerSegment; ++j)
            {
                Vector3 position = EvaluateSegment(
                    i,
                    (float)j / ESamplesPerSegment);
                m_totalDistance += Vector3.Distance(previous, position);
                m_samplePositions.Add(position);
                m_sampleDistances.Add(m_totalDistance);
                previous = position;
            }
        }
    }

    private Vector3 GetPositionAtDistance(float _distance)
    {
        for (int i = 1; i < m_sampleDistances.Count; ++i)
        {
            if (_distance > m_sampleDistances[i])continue;

            float startDistance = m_sampleDistances[i - 1];
            float length = m_sampleDistances[i] - startDistance;
            float interpolation = length > Mathf.Epsilon
                ? (_distance - startDistance) / length
                : 0.0f;
            return Vector3.Lerp(
                m_samplePositions[i - 1],
                m_samplePositions[i],
                interpolation);
        }
        return m_samplePositions[m_samplePositions.Count - 1];
    }

    private int GetPointIndex(int _index)
    {
        if (b_m_loop)
        {
            return (_index % m_points.Length + m_points.Length) % m_points.Length;
        }
        return Mathf.Clamp(_index, 0, m_points.Length - 1);
    }

    private void OnValidate()
    {
        m_speed = Mathf.Max(0.01f, m_speed);
        b_m_cacheDirty = true;
    }
}
