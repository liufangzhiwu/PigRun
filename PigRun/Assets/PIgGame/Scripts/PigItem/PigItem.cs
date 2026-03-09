using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PigItem : MonoBehaviour
{
    public Animator animator;
    
    Rigidbody pigRigidbody;
    MapItem mapItem;
    bool isMoving=false;
    
    [SerializeField] private float speed = 5f;          // 移动速度
    private Vector3 targetPosition;                      // 目标位置（有障碍时使用）
    private bool movingToTarget;                         // 是否向目标移动
    private bool movingForward;                           // 是否向前匀速移动

    void Start()
    {
        pigRigidbody = GetComponent<Rigidbody>();
        mapItem = GetComponent<MapItem>();
    }

    void Update()
    {
        if (!isMoving) return;

        if (movingForward)
        {
            // 匀速向前移动
            transform.Translate(Vector3.forward * speed * Time.deltaTime);

            // 检测出屏
            if (Map.Instance != null && IsOutOfScreen())
            {
                Map.Instance.RemoveItem(mapItem);
                StopMoving();
            }
        }
        else if (movingToTarget)
        {
            // 平滑移动到目标位置
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                // 到达目标，停止
                StopMoving();
            }
        }
    }

    private void OnMouseUpAsButton()
    {
        // 计算终点
        bool hasObstacle = CalculateTargetPosition(out targetPosition);

        if (hasObstacle)
        {
            // 有障碍：直接移动到障碍前
            movingToTarget = true;
            movingForward = false;
        }
        else
        {
            // 无障碍：匀速向前移动
            movingForward = true;
            movingToTarget = false;
        }

        isMoving = true;
        animator.SetBool("IsRun", true);
        Debug.Log("PigItem Clicked");
    }

    /// <summary>
    /// 计算移动终点
    /// </summary>
    /// <param name="target">终点世界坐标（若无障碍，则无效）</param>
    /// <returns>true=有障碍（target为障碍前位置），false=无障碍</returns>
    bool CalculateTargetPosition(out Vector3 target)
    {
        target = Vector3.zero;
        Vector2Int forwardOffset = GetForwardOffset();
        Vector2Int currentGrid = mapItem.gridPos;
        Vector2Int checkGrid = currentGrid + forwardOffset;

        // 逐格向前扫描，直到遇到障碍或边界
        while (true)
        {
            // 检查是否越界
            if (checkGrid.x < 0 || checkGrid.x >= Map.Instance.rows ||
                checkGrid.y < 0 || checkGrid.y >= Map.Instance.cols)
            {
                // 无障碍物，但已到边界，可继续向前移动（最终出屏）
                return false;
            }
        
            int occupantId = Map.Instance.GetOccupantIdAtCell(checkGrid);
            if (occupantId != -1 && occupantId != GetMyId())
            {
                // 遇到障碍，终点为当前检查格的前一个空闲格
                Vector2Int lastFreeGrid = checkGrid - forwardOffset;
                target = Map.Instance.GetCellWorldPosition(lastFreeGrid);
                return true;
            }
        
            // 继续向前
            checkGrid += forwardOffset;
        }
    }

    /// <summary>
    /// 根据旋转索引获取前方网格偏移量（需与Map坐标系一致）
    /// 假设：rotIndex 3=上，0=右，1=下，2=左（根据你的实际映射调整）
    /// </summary>
    Vector2Int GetForwardOffset()
    {
        switch (mapItem.rotIndex)
        {
            case 3: return new Vector2Int(-1, 0); // 向上
            case 0: return new Vector2Int(0, 1);  // 向右
            case 1: return new Vector2Int(0, 1);  // 向下
            case 2: return new Vector2Int(0, -1); // 向左
            default: return Vector2Int.zero;
        }
    }

    /// <summary>
    /// 获取当前小猪在Map中的ID
    /// </summary>
    int GetMyId()
    {
        return Map.Instance.GetIdByItem(mapItem);
    }

    void StopMoving()
    {
        isMoving = false;
        movingForward = false;
        movingToTarget = false;
        animator.SetBool("IsRun", false);
    }

    bool IsOutOfScreen()
    {
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);
        if (viewportPos.z < -0.05f ||
            viewportPos.x < -0.05f || viewportPos.x > 1.05f ||
            viewportPos.y < -0.05f || viewportPos.y > 1.05f)
        {
            return true;
        }
        return false;
    }
}