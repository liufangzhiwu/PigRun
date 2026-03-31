using System.Collections.Generic;
using UnityEngine;

public class AutoMovingState : AnimalBase.IAnimalState
{
    private readonly ChickItem chick;
    private readonly List<Vector2Int> path;           // 移动路径（网格坐标）
    private readonly Vector2Int finalEscapeDirection; // 最终跑出方向
    private int currentPathIndex;
    private bool hasStarted = false;
    private bool isEscaping = false;
    
    public AutoMovingState(ChickItem chick, List<Vector2Int> path, Vector2Int escapeDirection)
    {
        this.chick = chick;
        this.path = path;
        this.finalEscapeDirection = escapeDirection;
        this.currentPathIndex = 0;
    }
    
    public void Enter()
    {
        hasStarted = true;
        
        // 播放移动动画
        if (chick.animator != null)
        {
            chick.animator.SetBool("IsRun", true);
        }
        
        Debug.Log($"小鸡进入自动移动状态，需要移动 {path.Count} 个路点");
        Map.Instance.UpdateMapItemArea(chick.MapItem);
    }
    
    public void Update()
    {
        if (!hasStarted) return;
        
        // 如果已经在跑出阶段
        if (isEscaping)
        {
            UpdateEscape();
            return;
        }
        
        // 还有路点需要移动
        if (currentPathIndex < path.Count)
        {
            MoveToNextWaypoint();
        }
        else
        {
            // 所有路点移动完毕，开始跑出边界
            StartEscape();
        }
    }
    
    /// <summary>
    /// 移动到下一个路点
    /// </summary>
    private void MoveToNextWaypoint()
    {
        Vector2Int targetGrid = path[currentPathIndex];
        Vector3 targetPos = Map.Instance.GridToWorld(targetGrid);
        
        // 计算移动方向
        Vector3 moveDirection = (targetPos - chick.transform.position).normalized;
        
        // 更新朝向
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            chick.transform.rotation = Quaternion.Slerp(chick.transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
        
        // 向目标点移动
        chick.transform.position = Vector3.MoveTowards(
            chick.transform.position, 
            targetPos, 
            chick.Speed * Time.deltaTime
        );
        
        // 检查是否到达目标点
        if (Vector3.Distance(chick.transform.position, targetPos) < 0.1f)
        {
            currentPathIndex++;
            Debug.Log($"小鸡到达路点 {currentPathIndex}/{path.Count}");
        }
    }
    
    /// <summary>
    /// 开始跑出边界
    /// </summary>
    private void StartEscape()
    {
        isEscaping = true;
        
        // 获取跑出的世界方向
        Vector3 escapeWorldDir = GetEscapeWorldDirection(finalEscapeDirection);
        
        if (escapeWorldDir != Vector3.zero)
        {
            chick.transform.forward = escapeWorldDir;
            Debug.Log($"小鸡开始跑出边界，方向: {escapeWorldDir}");
        }
        else
        {
            // 如果没有指定方向，根据当前位置判断
            escapeWorldDir = GetEscapeWorldDirectionFromPosition();
            if (escapeWorldDir != Vector3.zero)
            {
                chick.transform.forward = escapeWorldDir;
            }
            else
            {
                // 无法跑出，重新寻路
                Debug.LogWarning("小鸡无法确定跑出方向，重新寻路");
                chick.ChangeState(new IdleState(chick));
                chick.StartAutoMove();
            }
        }
    }
    
    /// <summary>
    /// 更新跑出状态
    /// </summary>
    private void UpdateEscape()
    {
        Vector3 escapeWorldDir = GetEscapeWorldDirection(finalEscapeDirection);
        
        // 如果没有指定方向，根据当前位置判断
        if (escapeWorldDir == Vector3.zero)
        {
            escapeWorldDir = GetEscapeWorldDirectionFromPosition();
        }
        
        if (escapeWorldDir != Vector3.zero)
        {
            // 向边界外移动
            chick.transform.Translate( Vector3.forward* chick.Speed * Time.deltaTime);
            
            //检查是否跑出屏幕
            // if (chick.IsOutOfScreen())
            // {
            //     //OnEscape();
            // }
        }
    }
    
    /// <summary>
    /// 根据网格方向获取世界方向
    /// </summary>
    private Vector3 GetEscapeWorldDirection(Vector2Int gridDirection)
    {
        if (gridDirection.x == -1)
            return Vector3.left;
        else if (gridDirection.x == 1)
            return Vector3.right;
        else if (gridDirection.y == -1)
            return Vector3.back;
        else if (gridDirection.y == 1)
            return Vector3.forward;
        
        return Vector3.zero;
    }
    
    /// <summary>
    /// 根据当前位置获取跑出方向
    /// </summary>
    private Vector3 GetEscapeWorldDirectionFromPosition()
    {
        Vector2Int currentGrid = chick.MapItem.gridPos;
        
        if (currentGrid.x == 0)
            return Vector3.left;
        else if (currentGrid.x == Map.Instance.rows - 1)
            return Vector3.right;
        else if (currentGrid.y == 0)
            return Vector3.back;
        else if (currentGrid.y == Map.Instance.cols - 1)
            return Vector3.forward;
        
        return Vector3.zero;
    }
    
    /// <summary>
    /// 跑出成功
    /// </summary>
    private void OnEscape()
    {
        Debug.Log("小鸡成功跑出网格！");
        
        // 播放跑出音效
        //AudioManager.Instance.PlaySoundEffect("chick_escape");
        
        // 触发跑出事件（用于乌龟任务计数）
        //Map.Instance.OnAnimalEscaped?.Invoke(chick);
        
        // 从地图移除
        Map.Instance.RemoveItem(chick.MapItem);
        
        // 销毁小鸡
        Object.Destroy(chick.gameObject);
    }
    
    public void Exit()
    {
        // 停止移动动画
        if (chick.animator != null)
        {
            chick.animator.SetBool("IsRun", false);
        }
        
        Debug.Log("小鸡退出自动移动状态");
    }
    
    public void HandleClick()
    {
        // 自动移动状态不响应点击
    }
}