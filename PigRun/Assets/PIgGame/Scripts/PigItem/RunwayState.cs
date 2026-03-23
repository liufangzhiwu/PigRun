using DG.Tweening;
using UnityEngine;

public class RunwayState : PigItem.IPigState
{
    private PigItem pig;
    private int currentSegment;   // 当前线段索引（0 ~ waypoints.Count-2）
    private float currentT;       // 当前线段内的进度（0~1）
    private bool isMoving;

    public RunwayState(PigItem pig) { this.pig = pig; }

    public void Enter()
    {
        if (pig.currentRunway == null || pig.currentRunway.waypoints.Count < 2)
        {
            pig.ChangeState(new IdleState(pig));
            return;
        }

        // 初始化线段索引（确保在有效范围内）
        currentSegment = Mathf.Clamp(pig.currentSegmentIndex, 0, pig.currentRunway.waypoints.Count - 2);

        Vector3 start = pig.currentRunway.waypoints[currentSegment].position;
        Vector3 end = pig.currentRunway.waypoints[currentSegment + 1].position;
        Vector3 line = end - start;
        float lineLen = line.magnitude;
        if (lineLen > 0)
        {
            Vector3 toPoint = pig.transform.position - start;
            currentT = Vector3.Dot(toPoint, line) / line.sqrMagnitude;
            currentT = Mathf.Clamp01(currentT);
        }
        else
        {
            currentT = 0f;
        }

        // 如果已经在最后一个线段且进度接近终点，直接结束
        if (currentSegment == pig.currentRunway.waypoints.Count - 2 && currentT >= 0.99f)
        {
            OnReachEnd();
            return;
        }

        isMoving = true;
        if (pig.animator != null)
            pig.animator.SetBool("Run", true);
    }

    public void Update()
    {
        if (!isMoving) return;

        float moveDist = pig.Speed * Time.deltaTime;
        if (moveDist <= 0) return;

        bool reachedEnd = false;

        while (moveDist > 0 && !reachedEnd)
        {
            // 边界检查：如果已经超出最后一个线段，则结束
            if (currentSegment >= pig.currentRunway.waypoints.Count - 1)
            {
                reachedEnd = true;
                break;
            }

            Vector3 start = pig.currentRunway.waypoints[currentSegment].position;
            Vector3 end = pig.currentRunway.waypoints[currentSegment + 1].position;
            Vector3 line = end - start;
            float segmentLen = line.magnitude;

            // 防止零长度线段（跳过）
            if (segmentLen < 0.001f)
            {
                currentSegment++;
                continue;
            }

            float remainingInSegment = segmentLen * (1f - currentT);

            if (moveDist >= remainingInSegment)
            {
                // 超出当前线段，进入下一段
                moveDist -= remainingInSegment;
                currentSegment++;
                currentT = 0f;

                // 如果已经走完所有线段，标记结束
                if (currentSegment >= pig.currentRunway.waypoints.Count - 1)
                {
                    reachedEnd = true;
                    break;
                }
            }
            else
            {
                // 在当前线段内移动
                float tInc = moveDist / segmentLen;
                currentT += tInc;
                moveDist = 0;
            }
        }

        // 更新位置
        if (!reachedEnd && currentSegment < pig.currentRunway.waypoints.Count - 1)
        {
            pig.transform.position = GetCurrentPosition();

            // 更新朝向（平滑旋转）
            Vector3 segmentDir = (pig.currentRunway.waypoints[currentSegment + 1].position - pig.currentRunway.waypoints[currentSegment].position).normalized;
            if (segmentDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(segmentDir);
                pig.transform.rotation = Quaternion.Slerp(pig.transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }
        else
        {
            // 如果已到达终点，将位置设置到最后一个路径点
            var last = pig.currentRunway.waypoints[pig.currentRunway.waypoints.Count - 1];
            pig.transform.position = last.position;
        }

        // 到达终点判定
        if (reachedEnd || (currentSegment >= pig.currentRunway.waypoints.Count - 1 && currentT >= 0.99f))
        {
            OnReachEnd();
        }
    }

    private Vector3 GetCurrentPosition()
    {
        // 安全获取位置：如果线段索引超出，返回最后一个路径点
        if (currentSegment >= pig.currentRunway.waypoints.Count - 1)
            return pig.currentRunway.waypoints[pig.currentRunway.waypoints.Count - 1].position;

        Vector3 start = pig.currentRunway.waypoints[currentSegment].position;
        Vector3 end = pig.currentRunway.waypoints[currentSegment + 1].position;
        return Vector3.Lerp(start, end, currentT);
    }

    private void OnReachEnd()
    {
        isMoving = false;
        if (pig.animator != null)
            pig.animator.SetBool("Run", false);
        Map.Instance.RemoveItem(pig.MapItem);
        pig.ChangeState(new IdleState(pig));
    }

    public void Exit()
    {
        if (pig.animator != null)
            pig.animator.SetBool("Run", false);
    }

    public void HandleClick() { }
}