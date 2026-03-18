using System.Collections;
using UnityEngine;

public class RunwayState : PigItem.IPigState
{
    private readonly PigItem pig;
    private Coroutine moveCoroutine;

    
    public RunwayState(PigItem pig) { this.pig = pig; }
    
    public void Enter()
    {
        moveCoroutine = pig.StartCoroutine(RunwayMoveCoroutine());
    }

    public void Update()
    {
        throw new System.NotImplementedException();
    }

    public void Exit()
    {
        if (moveCoroutine != null)
            pig.StopCoroutine(moveCoroutine);
    }

    private IEnumerator RunwayMoveCoroutine()
    {
        Map map = Map.Instance;
        Vector2Int currentGrid = pig.MapItem.gridPos;

        while (true)
        {
            // 到达房子？
            if (map.IsHouseCell(currentGrid))
            {
                map.RemoveItem(pig.MapItem);
                pig.ChangeState(new IdleState(pig));
                yield break;
            }

            // 获取方向
            Vector2Int dir = map.GetRunwayDirection(currentGrid);
            if (dir == Vector2Int.zero)
            {
                Debug.LogError("无效跑道格子，退出状态");
                pig.ChangeState(new IdleState(pig));
                yield break;
            }

            Vector2Int nextGrid = currentGrid + dir;
            if (!map.IsInBounds(nextGrid))
            {
                pig.ChangeState(new IdleState(pig));
                yield break;
            }

            // 计算目标位置（3D 坐标，Y 固定为 0）
            Vector3 startPos = pig.transform.position;
            Vector3 targetPos = map.GridToWorldPosition(nextGrid);
            // 可选：保持 Y 不变（如果地图平面 Y 不是 0，可以统一）
            targetPos.y = pig.transform.position.y; // 或者固定值

            // 设置朝向（移动方向，忽略 Y）
            Vector3 moveDir = new Vector3(dir.x, 0, dir.y).normalized;
            pig.transform.rotation = Quaternion.LookRotation(moveDir);

            // 播放跑步动画
            if (pig.animator != null)
                pig.animator.SetBool("Run", true);

            // 移动插值
            float duration = 1f / pig.Speed; // 假设每格移动时间 = 1/速度
            float elapsed = 0f;
            while (elapsed < duration)
            {
                pig.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            pig.transform.position = targetPos;

            // 停止动画
            if (pig.animator != null)
                pig.animator.SetBool("Run", false);

            // 更新地图格子
            map.MoveItemToCell(pig.MapItem, nextGrid);
            currentGrid = nextGrid;
        }
    }
}