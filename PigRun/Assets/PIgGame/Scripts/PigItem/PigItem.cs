using System.Collections;
using UnityEngine;

public class PigItem : MonoBehaviour
{
    public Animator animator;
    
    private MapItem mapItem;
    private bool isMoving = false;
    
    [SerializeField] private float speed = 5f;
    private Vector3 targetPosition;
    private bool movingToTarget;
    private bool movingForward;

    // 新增：闲置触发 Fidget 相关
    [SerializeField] public float idleFidgetDelay = 30;   // 闲置多少秒后触发 Fidget
    private float idleTimer = 0f;                           // 当前闲置计时

    void Start()
    {
        mapItem = GetComponent<MapItem>();
        ResetIdleTimer(); // 初始化计时器
        idleFidgetDelay = Random.Range(10, 400);
    }

    void Update()
    {
        // 闲置计时逻辑：仅在非移动状态下累加时间
        if (!isMoving)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleFidgetDelay)
            {
                TriggerFidget();
                ResetIdleTimer(); // 触发后重置计时
            }
        }

        if (!isMoving) return;

        if (movingForward)
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
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
                float step = speed * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                    StopMoving();
            }
            else
            {
                StopMoving();
            }
        }
    }

    private void OnMouseUpAsButton()
    {
        // 移动中不响应点击，避免干扰
        if (isMoving) return;

        // 点击即重置闲置计时
        ResetIdleTimer();

        bool hasObstacle = CalculateTargetPosition(out targetPosition);

        if (hasObstacle)
        {
            if (targetPosition != Vector3.zero)
            {
                // 非紧邻障碍：移动到目标点
                movingToTarget = true;
                movingForward = false;
                isMoving = true;
                animator.SetBool("IsRun", true);
            }
            else
            {
                // 紧邻障碍：播放自身受击动画
                HitSelf();
            }
        }
        else
        {
            // 无障碍：匀速向前
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
            if (checkGrid.x < 0 || checkGrid.x >= Map.Instance.rows ||
                checkGrid.y < 0 || checkGrid.y >= Map.Instance.cols)
            {
                return false;
            }

            int occupantId = Map.Instance.GetOccupantIdAtCell(checkGrid);
            if (occupantId != -1 && occupantId != mapItem.id)
            {
                // 紧邻障碍
                if (checkGrid - forwardOffset == currentGrid)
                {
                    target = Vector3.zero;

                    PigItem hitItem = Map.Instance.GetPlacedItem(occupantId)?.instance.GetComponent<PigItem>();
                    if (hitItem != null)
                    {
                        hitItem.BeHit(); // 调用被撞对象的受击方法
                    }
                    else
                    {
                        Debug.LogWarning("未找到被撞物体的 PigItem 组件");
                    }
                    return true;
                }
                else
                {
                    // 非紧邻：计算目标位置
                    Vector2Int obstacleGrid = new Vector2Int(checkGrid.x, checkGrid.y);
                    Map.Instance.TryMoveItemTargetCell(mapItem, obstacleGrid, out target);
                    return true;
                }
            }
            checkGrid += forwardOffset;
        }
    }

    /// <summary>
    /// 自身受击（撞到紧邻障碍时播放）
    /// </summary>
    private void HitSelf()
    {
        animator.SetBool("IsHit", true);
        StartCoroutine(ResetHitAfterDelay(0.5f)); // 根据动画长度调整
        ResetIdleTimer(); // 发生交互，重置闲置计时
    }

    /// <summary>
    /// 被其他物体撞击时调用
    /// </summary>
    public void BeHit()
    {
        animator.SetBool("IsBeHit", true);
        StartCoroutine(ResetBeHitAfterDelay(0.5f));
        ResetIdleTimer(); // 被撞也视为交互，重置闲置计时
    }

    private IEnumerator ResetHitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.SetBool("IsHit", false);
    }

    private IEnumerator ResetBeHitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.SetBool("IsBeHit", false);
    }
    
    private IEnumerator ResetFidgetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.SetBool("IsFidget", false);
    }

    Vector2Int GetForwardOffset()
    {
        // 根据实际方向映射调整
        switch (mapItem.rotIndex)
        {
            case 3: return new Vector2Int(-1, 0); // 右
            case 0: return new Vector2Int(0, -1); // 上
            case 1: return new Vector2Int(0, 1);  // 下
            case 2: return new Vector2Int(-1, 0); // 左
            default: return Vector2Int.zero;
        }
    }

    void StopMoving()
    {
        isMoving = false;
        movingForward = false;
        movingToTarget = false;
        animator.SetBool("IsRun", false); // 只复位跑步动画
        ResetIdleTimer(); // 移动结束，重置闲置计时
    }

    bool IsOutOfScreen()
    {
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);
        return viewportPos.z < -0.05f ||
               viewportPos.x < -0.05f || viewportPos.x > 1.05f ||
               viewportPos.y < -0.05f || viewportPos.y > 1.05f;
    }

    // 新增：触发 Fidget 动画
    private void TriggerFidget()
    {
        // 假设 Animator 中有一个 Trigger 参数 "Fidget"
        animator.SetBool("IsFidget", true); // 只复位跑步动画
        StartCoroutine(ResetFidgetAfterDelay(0.5f));
        // 如果有多个 Fidget 动画，可以随机选择一个索引后再触发
        // int index = Random.Range(0, 3); // 假设有 3 种
        // animator.SetInteger("FidgetIndex", index);
        // animator.SetTrigger("Fidget");
    }

    // 新增：重置闲置计时器
    private void ResetIdleTimer()
    {
        idleTimer = 0f;
    }
}