using System.Collections.Generic;
using UnityEngine;

public class RunwayPath : MonoBehaviour
{
    public List<Transform> waypoints; // 按顺序的路径点
    public Transform endpoint; // 终点

    private void Awake()
    {
        // 自动收集子物体中的路径点
        if (waypoints == null || waypoints.Count == 0)
        {
            waypoints = new List<Transform>();
            foreach (Transform child in transform)
                if (child.name.StartsWith("Waypoint"))
                    waypoints.Add(child);
            waypoints.Sort((a,b)=>string.Compare(a.name,b.name));
        }
    
        // 确保终点被包含（如果 endpoint 不为空且不在列表中）
        if (endpoint != null && !waypoints.Contains(endpoint))
        {
            waypoints.Add(endpoint);
        }
    
        Debug.Log($"最终 waypoints 数量: {waypoints.Count}，SegmentCount: {waypoints.Count-1}");
    }

  

    /// <summary> 获取指定线段的起点和终点 </summary>
    public (Vector3 start, Vector3 end) GetSegment(int index)
    {
        if (index < 0 || index >= waypoints.Count - 1)
            throw new System.IndexOutOfRangeException("线段索引超出范围");
        return (waypoints[index].position, waypoints[index + 1].position);
    }

    /// <summary> 找到离世界坐标最近的线段索引 </summary>
    public int FindClosestSegment(Vector3 worldPos)
    {
        int bestIdx = -1;
        float minDist = float.MaxValue;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 closest = GetClosestPointOnSegment(worldPos, waypoints[i].position, waypoints[i + 1].position);
            float dist = Vector3.Distance(worldPos, closest);
            if (dist < minDist)
            {
                minDist = dist;
                bestIdx = i;
            }
        }
        return bestIdx;
    }
    
    
    /// <summary> 获取世界坐标在跑道上的投影点、所在线段索引及线段内进度 t </summary>
    public (Vector3 point, int segmentIndex, float t) GetProjectedPointAndSegment(Vector3 worldPos)
    {
        int bestIdx = -1;
        float minDist = float.MaxValue;
        Vector3 bestPoint = Vector3.zero;
        float bestT = 0f;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 a = waypoints[i].position;
            Vector3 b = waypoints[i + 1].position;
            Vector3 ab = b - a;
            float t = Vector3.Dot(worldPos - a, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            Vector3 point = a + t * ab;
            float dist = Vector3.Distance(worldPos, point);
            if (dist < minDist)
            {
                minDist = dist;
                bestIdx = i;
                bestPoint = point;
                bestT = t;
            }
        }
        return (bestPoint, bestIdx, bestT);
    }
  

    /// <summary> 获取世界坐标在路径上的投影点，并返回所在线段索引 </summary>
    public Vector3 GetProjectedPoint(Vector3 worldPos, out int segmentIndex)
    {
        segmentIndex = FindClosestSegment(worldPos);
        var (start, end) = GetSegment(segmentIndex);
        return GetClosestPointOnSegment(worldPos, start, end);
    }

    private Vector3 GetClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(point - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        return a + t * ab;
    }
}