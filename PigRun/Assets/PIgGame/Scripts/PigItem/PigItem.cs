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
            if (targetPosition != Vector3.zero)
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
            else
            {
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
            
            isMoving = true;
            
            if (targetPosition != Vector3.zero)
            {
                animator.SetBool("IsRun", true);
            }
            else
            {
                animator.SetBool("IsHit", true);
            }
        }
        else
        {
            // 无障碍：匀速向前移动
            movingForward = true;
            movingToTarget = false;
            Map.Instance.UpdateMapItemArea(mapItem);
            
            isMoving = true;
            animator.SetBool("IsRun", true);
        }

        Debug.Log("PigItem Clicked");
    }

    bool CalculateTargetPosition(out Vector3 target)
    {
        target = Vector3.zero;
        Vector2Int forwardOffset = GetForwardOffset();
        Vector2Int currentGrid = mapItem.gridPos;
        Vector2Int checkGrid = currentGrid + forwardOffset;

        while (true)
        {
            // 检查边界
            if (checkGrid.x < 0 || checkGrid.x >= Map.Instance.rows ||
                checkGrid.y < 0 || checkGrid.y >= Map.Instance.cols)
            {
                // 无障碍，可继续移动（调用者需处理边界移动）
                return false;
            }

            int occupantId = Map.Instance.GetOccupantIdAtCell(checkGrid);
            if (occupantId != -1 && occupantId != mapItem.id)
            {
                // 判断是否紧邻障碍（第一次检查就遇到）
                if (checkGrid - forwardOffset == currentGrid) // 相邻格
                {
                    target = Vector3.zero; // 无移动空间
                    
                    PigItem hitItem = Map.Instance.GetPlacedItem(occupantId)?.instance.GetComponent<PigItem>();
                    hitItem.animator.SetBool("IsBeHit", true);
                    hitItem.animator.SetBool("IsBeHit", false);
                    return true;
                }
                else
                {
                    // 非紧邻，计算障碍前一格的位置
                    Vector2Int obstacleGrid = new Vector2Int(checkGrid.x, checkGrid.y);
                    Map.Instance.TryMoveItemTargetCell(mapItem, obstacleGrid, out target);
                    // 若使用旧逻辑，可直接：target = Map.Instance.GridToWorld(checkGrid - forwardOffset);
                    return true;
                }
            }

            // 继续向前扫描
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
            case 3: return new Vector2Int(-1, 0); // 向右
            case 0: return new Vector2Int(0, -1);  // 向上
            case 1: return new Vector2Int(0, 1);  // 向下
            case 2: return new Vector2Int(-1, 0); // 向左
            default: return Vector2Int.zero;
        }
    }

    void StopMoving()
    {
        isMoving = false;
        movingForward = false;
        movingToTarget = false;
        if (targetPosition != Vector3.zero)
        {
            animator.SetBool("IsRun", false);
        }
        else
        {
            animator.SetBool("IsHit", false);
        }
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