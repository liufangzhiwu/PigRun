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
    
    
    public int FindClosestWaypoint(Vector3 worldPos)
    {
        int bestIdx = -1;
        float minDist = float.MaxValue;
        for (int i = 0; i < waypoints.Count; i++)
        {
            float dist = Vector3.Distance(worldPos, waypoints[i].position);
            if (dist < minDist)
            {
                minDist = dist;
                bestIdx = i;
            }
        }
        return bestIdx;
    }

    /// <summary> 获取当前线段的下一个目标点，并根据需要更新线段索引 </summary>
    public Vector3 GetNextTarget(Vector3 currentPos, ref int segmentIndex)
    {
        if (segmentIndex < 0) segmentIndex = 0;
        if (segmentIndex >= waypoints.Count - 1)
            return waypoints[waypoints.Count - 1].position; // 已到终点

        var (start, end) = GetSegment(segmentIndex);
        Vector3 lineDir = end - start;
        float lineLength = lineDir.magnitude;
        lineDir.Normalize();

        Vector3 toStart = currentPos - start;
        float t = Vector3.Dot(toStart, lineDir) / lineLength;
        t = Mathf.Clamp01(t);

        // 如果非常接近终点，尝试切换到下一线段
        if (t >= 0.99f && segmentIndex < waypoints.Count - 2)
        {
            segmentIndex++;
            return waypoints[segmentIndex + 1].position; // 新线段的终点
        }
        else
        {
            return end; // 当前线段终点作为目标
        }
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