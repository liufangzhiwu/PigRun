using System.Collections;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class PigItem : MonoBehaviour
{
    public Animator animator;
    [SerializeField] private float speed = 5f;
    [SerializeField] public float idleFidgetDelay = 30;   // 闲置多少秒后触发 Fidget

    private MapItem mapItem;
    private PigItem behitItem;          // 将要撞击的物体（用于移动结束时触发其 BeHit）
    private IPigState currentState;

    // 外部可访问的属性
    public MapItem MapItem => mapItem;
    public float Speed => speed;
    public PigItem BehitItem => behitItem;
    public IPigState CurrentState => currentState;
    
    public RunwayPath currentRunway;
    public int currentSegmentIndex;
    private bool isOnRunway;
    
    public int currentWaypointIndex; // 当前要移动到的路径点索引
    

    void Start()
    {
        mapItem = GetComponent<MapItem>();
        ChangeState(new IdleState(this));
    }

    void Update()
    {
        currentState?.Update();
    }

    private void OnMouseUpAsButton()
    {
        if (UIManager.Instance.IsPanelTypeShowing())
        {
            Debug.Log("进入弹窗界面 不触发小猪逻辑");
            return;
        }
        
        currentState?.HandleClick();
    }

    /// <summary>
    /// 被其他物体撞击时调用（由外部触发）
    /// </summary>
    public void BeHit()
    {
        ChangeState(new BeHitState(this));
    }

    /// <summary>
    /// 自身撞击障碍时调用（内部触发）
    /// </summary>
    public void HitSelf()
    {
        ChangeState(new HitState(this));
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    public void ChangeState(IPigState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }
    
    
    public void EnterRunway(RunwayPath runway, Vector3 enterPos)
    {
        // 根据您的状态设计，可能只允许从直线奔跑状态进入
        if (currentState is MovingState)
        {
        
            currentRunway = runway;
            // 找到离进入点最近的路径点
            int targetWaypointIndex = runway.FindClosestWaypoint(enterPos);
            currentWaypointIndex = targetWaypointIndex;
        
            // 获取目标位置（最近的路径点）
            Vector3 targetPos = runway.waypoints[currentWaypointIndex].position;
        
            // 转向目标
            Vector3 dir = (targetPos - transform.position).normalized;
            
            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.DORotateQuaternion(targetRot, 0.05f); // 0.2秒内旋转到目标
            }
            
            // 先播放移动到目标点的动画，再切换状态
            // 可以使用 DOTween 移动，并等待完成
            transform.DOMove(targetPos, 0.05f) // 移动耗时0.3秒
                .SetEase(Ease.OutQuad)        // 缓动效果
                .OnComplete(() =>              // 移动完成后
                {
                    // 确保最终位置精确
                    transform.position = targetPos;
                    // 切换状态
                    ChangeState(new RunwayState(this));
                });
        
            // 可选：在移动过程中禁用点击或其他输入，防止重复触发
            // 可设置一个标志位，或者暂时停用状态切换
        }
    }

    public void ExitRunway()
    {
        if (currentState is RunwayState)
        {
            isOnRunway = false;
            ChangeState(new IdleState(this));
        }
    }
    

    /// <summary>
    /// 计算移动目标位置（用于点击时）
    /// </summary>
    /// <param name="target">输出目标世界坐标</param>
    /// <returns>true 表示前方有障碍，false 表示无障碍可直线前进</returns>
    public bool CalculateTargetPosition(out Vector3 target)
    {
        target = Vector3.zero;
        Vector2Int checkGrid = GetForwardOffset(out Vector2Int currentGrid, out Vector2Int forwardOffset);

        while (true)
        {
            if (checkGrid.x < 0 || checkGrid.x >= Map.Instance.rows ||
                checkGrid.y < 0 || checkGrid.y >= Map.Instance.cols)
            {
                return false; // 出界，无障碍
            }

            int occupantId = Map.Instance.GetOccupantIdAtCell(checkGrid);
            if (occupantId != -1 && occupantId != mapItem.id)
            {
                // 记录被撞物体
                behitItem = Map.Instance.GetPlacedItem(occupantId)?.instance.GetComponent<PigItem>();

                // 紧邻障碍
                if (checkGrid - forwardOffset == currentGrid)
                {
                    target = Vector3.zero;
                    return true;
                }
                else
                {
                    // 根据旋转调整目标格子（原有逻辑）
                    switch (mapItem.rotIndex)
                    {
                        case 0: break;
                        case 1: checkGrid = new Vector2Int(checkGrid.x + 1, checkGrid.y); break;
                        case 2: checkGrid = new Vector2Int(checkGrid.x + 2, checkGrid.y + 1); break;
                        default: checkGrid = new Vector2Int(checkGrid.x, checkGrid.y + 1); break;
                    }

                    Vector2Int obstacleGrid = new Vector2Int(checkGrid.x, checkGrid.y);
                    Map.Instance.TryMoveItemTargetCell(mapItem, obstacleGrid, out target);
                    return true;
                }
            }
            checkGrid += forwardOffset;
        }
    }

    /// <summary>
    /// 获取前进方向的偏移量和当前网格（考虑旋转）
    /// </summary>
    private Vector2Int GetForwardOffset(out Vector2Int currentGrid, out Vector2Int forwardOffset)
    {
        currentGrid = mapItem.gridPos;
        switch (mapItem.rotIndex)
        {
            case 0: forwardOffset = new Vector2Int(1, 0); break; // 右
            case 1:
                currentGrid = new Vector2Int(mapItem.gridPos.x - 1, mapItem.gridPos.y);
                forwardOffset = new Vector2Int(0, 1); // 下
                break;
            case 2:
                currentGrid = new Vector2Int(mapItem.gridPos.x, mapItem.gridPos.y - 1);
                forwardOffset = new Vector2Int(-1, 0); // 左
                break;
            default:
                forwardOffset = new Vector2Int(0, -1); // 上
                break;
        }
        return currentGrid + forwardOffset;
    }

    /// <summary>
    /// 判断物体是否移出屏幕
    /// </summary>
    public bool IsOutOfScreen()
    {
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);
        return viewportPos.z < -0.05f ||
               viewportPos.x < -0.05f || viewportPos.x > 1.05f ||
               viewportPos.y < -0.05f || viewportPos.y > 1.05f;
    }

    // ========== 状态接口 ==========
    public interface IPigState
    {
        void Enter();
        void Update();
        void Exit();
        void HandleClick() { } // 默认空实现
    }
 
}