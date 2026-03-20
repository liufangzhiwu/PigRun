using System.Collections;
using DG.Tweening;
using UnityEngine;

public class RunwayState : PigItem.IPigState
{
    private PigItem pig;
    private Coroutine moveCoroutine;

    public RunwayState(PigItem pig) { this.pig = pig; }

    public void Enter()
    {
        moveCoroutine = pig.StartCoroutine(RunwayMoveCoroutine());
    }

    public void Exit()
    {
        if (moveCoroutine != null)
            pig.StopCoroutine(moveCoroutine);
        // 确保退出时停止动画
        if (pig.animator != null)
            pig.animator.SetBool("Run", false);
    }

    private IEnumerator RunwayMoveCoroutine()
    {
        if (pig.currentRunway == null || pig.currentWaypointIndex < 0 || pig.currentWaypointIndex >= pig.currentRunway.waypoints.Count)
        {
            pig.ChangeState(new IdleState(pig));
            yield break;
        }

        if (pig.animator != null)
            pig.animator.SetBool("Run", true);

        // 从当前路径点开始，依次移动到下一个，直到最后一个
        for (int i = pig.currentWaypointIndex; i < pig.currentRunway.waypoints.Count; i++)
        {
            Vector3 target = pig.currentRunway.waypoints[i].position;

            // 转向目标
            Vector3 dir = (target - pig.transform.position).normalized;
            
            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                pig.transform.DORotateQuaternion(targetRot, 0.2f); // 0.2秒内旋转到目标
            }
            
            Debug.Log($"进入点: {target}, 最近线段索引: {i}");

            // 逐帧移动直到到达目标点
            while (Vector3.Distance(pig.transform.position, target) > 0.05f)
            {
                pig.transform.position = Vector3.MoveTowards(
                    pig.transform.position,
                    target,
                    pig.Speed * Time.deltaTime
                );
                yield return null;
            }
        }

        if (pig.animator != null)
            pig.animator.SetBool("Run", false);

        // 到达终点（最后一个路径点）
        Map.Instance.RemoveItem(pig.MapItem);
        if (pig != null)
            pig.ChangeState(new IdleState(pig));
    }


    public void Update() { }
}