using DG.Tweening;
using UnityEngine;

public class RunwayState : AnimalBase.IAnimalState
{
    private AnimalBase animal;
    private int currentSegment;   // 当前线段索引（0 ~ waypoints.Count-2）
    private float currentT;       // 当前线段内的进度（0~1）
    private bool isMoving;

    public RunwayState(AnimalBase pig) { this.animal = pig; }

    public void Enter()
    {
        if (animal.currentRunway == null || animal.currentRunway.waypoints.Count < 2)
        {
            animal.ChangeState(new IdleState(animal));
            return;
        }

        // 初始化线段索引（确保在有效范围内）
        currentSegment = Mathf.Clamp(animal.currentSegmentIndex, 0, animal.currentRunway.waypoints.Count - 2);

        Vector3 start = animal.currentRunway.waypoints[currentSegment].position;
        Vector3 end = animal.currentRunway.waypoints[currentSegment + 1].position;
        Vector3 line = end - start;
        float lineLen = line.magnitude;
        if (lineLen > 0)
        {
            Vector3 toPoint = animal.transform.position - start;
            currentT = Vector3.Dot(toPoint, line) / line.sqrMagnitude;
            currentT = Mathf.Clamp01(currentT);
        }
        else
        {
            currentT = 0f;
        }

        // 如果已经在最后一个线段且进度接近终点，直接结束
        if (currentSegment == animal.currentRunway.waypoints.Count - 2 && currentT >= 0.99f)
        {
            OnReachEnd();
            return;
        }

        isMoving = true;
        if (animal.animator != null)
            animal.animator.SetBool("IsRun", true);
    }

    public void Update()
    {
        if (!isMoving) return;

        float moveDist = animal.Speed * Time.deltaTime;
        if (moveDist <= 0) return;

        bool reachedEnd = false;

        while (moveDist > 0 && !reachedEnd)
        {
            // 边界检查：如果已经超出最后一个线段，则结束
            if (currentSegment >= animal.currentRunway.waypoints.Count - 1)
            {
                reachedEnd = true;
                break;
            }

            Vector3 start = animal.currentRunway.waypoints[currentSegment].position;
            Vector3 end = animal.currentRunway.waypoints[currentSegment + 1].position;
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
                if (currentSegment >= animal.currentRunway.waypoints.Count - 1)
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
        if (!reachedEnd && currentSegment < animal.currentRunway.waypoints.Count - 1)
        {
            animal.transform.position = GetCurrentPosition();

            // 更新朝向（平滑旋转）
            Vector3 segmentDir = (animal.currentRunway.waypoints[currentSegment + 1].position - animal.currentRunway.waypoints[currentSegment].position).normalized;
            if (segmentDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(segmentDir);
                animal.transform.rotation = Quaternion.Slerp(animal.transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }
        else
        {
            // 如果已到达终点，将位置设置到最后一个路径点
            var last = animal.currentRunway.waypoints[animal.currentRunway.waypoints.Count - 1];
            animal.transform.position = last.position;
        }

        // 到达终点判定
        if (reachedEnd || (currentSegment >= animal.currentRunway.waypoints.Count - 1 && currentT >= 0.99f))
        {
            OnReachEnd();
        }
    }

    private Vector3 GetCurrentPosition()
    {
        // 安全获取位置：如果线段索引超出，返回最后一个路径点
        if (currentSegment >= animal.currentRunway.waypoints.Count - 1)
            return animal.currentRunway.waypoints[animal.currentRunway.waypoints.Count - 1].position;

        Vector3 start = animal.currentRunway.waypoints[currentSegment].position;
        Vector3 end = animal.currentRunway.waypoints[currentSegment + 1].position;
        return Vector3.Lerp(start, end, currentT);
    }

    private void OnReachEnd()
    {
        isMoving = false;
        if (animal.animator != null)
            animal.animator.SetBool("IsRun", false);
        Map.Instance.RunOutRemoveItem(animal);
        animal.ChangeState(new IdleState(animal));
        
    }

    public void Exit()
    {
        if (animal.animator != null)
            animal.animator.SetBool("IsRun", false);
    }

    public void HandleClick() { }
}