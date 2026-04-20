using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public abstract class AnimalBase : MonoBehaviour
{
    [Header("动物属性")]
    public Animator animator;
    public ParticleSystem runParticleSystem;
    [SerializeField] protected float speed = 4f;
    [SerializeField] public float idleFidgetDelay = 30;   // 闲置多少秒后触发小动作

    protected MapItem mapItem;
    protected AnimalBase behitItem;          // 将要撞击的物体（单目标）
    protected IAnimalState currentState;
    protected Vector2Int startGrid;

    // 跑道相关
    [HideInInspector] public RunwayPath currentRunway;
    public int currentSegmentIndex;
    protected bool isOnRunway;
    
    public readonly int IsRunHash = Animator.StringToHash("IsRun");
    public readonly int IsHitHash = Animator.StringToHash("IsHit");
    public readonly int IsBeHitHash = Animator.StringToHash("IsBeHit");
    
    public event Action<AnimalBase> OnAnimalClicked;

    // 公共属性
    public MapItem MapItem => mapItem;
    public float Speed => speed;
    public AnimalBase BehitItem => behitItem;
    public IAnimalState CurrentState => currentState;

    protected virtual void Start()
    {
        mapItem = GetComponent<MapItem>();
        ChangeState(new IdleState(this));
        startGrid = new Vector2Int(mapItem.gridPos.x, mapItem.gridPos.y);
    }

    protected virtual void Update()
    {
        currentState?.Update();
    }

    protected virtual void OnMouseUpAsButton()
    {
        if (UIManager.Instance.IsPanelTypeShowing() || !UIManager.Instance.PanelIsShowing(PanelType.GamePanel))
        {
            Debug.Log("进入弹窗界面，不触发动物逻辑");
            return;
        }

        // 检查是否处于选择模式（由 SelectionModeManager 管理）
        bool isInSelectionMode = SelectionModeManager.Instance != null && SelectionModeManager.Instance.IsInSelectionMode;

        if (isInSelectionMode)
        {
            // 选择模式下，仅触发点击事件（用于选择），绝不触发移动
            OnAnimalClicked?.Invoke(this);
        }
        else
        {
            // 非选择模式，正常逻辑：如果有监听则触发事件，否则移动
            if (OnAnimalClicked != null && mapItem != null)
            {
                OnAnimalClicked(this);
            }
            else
            {
                currentState?.HandleClick();
            }
        }
    }

    /// <summary>
    /// 被其他物体撞击时调用（外部触发）
    /// </summary>
    public virtual void BeHit()
    {
        ChangeState(new BeHitState(this));
    }

    /// <summary>
    /// 自身撞击障碍时调用（内部触发）
    /// </summary>
    public virtual void HitSelf()
    {
        ChangeState(new HitState(this));
    }
    
    /// <summary>
    /// 有目标对象的自身撞击障碍时调用（内部触发）
    /// </summary>
    public virtual void TargetHitSelf()
    {
        ChangeState(new HitState(this));
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    public void ChangeState(IAnimalState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public virtual void EnterRunway(RunwayPath runway, Vector3 enterPos)
    {
        if (currentState is MovingState)
        {
            currentRunway = runway;
            var (projectedPoint, segmentIndex, t) = runway.GetProjectedPointAndSegment(enterPos);
            transform.position = projectedPoint;
            currentSegmentIndex = segmentIndex;

            if (segmentIndex < runway.waypoints.Count - 1)
            {
                Vector3 segmentDir = (runway.waypoints[segmentIndex + 1].position - runway.waypoints[segmentIndex].position).normalized;
                if (segmentDir != Vector3.zero)
                    transform.DORotateQuaternion(Quaternion.LookRotation(segmentDir), 0.02f);
            }

            ChangeState(new RunwayState(this));
        }
    }

    public virtual void ExitRunway()
    {
        if (currentState is RunwayState)
        {
            isOnRunway = false;
            ChangeState(new IdleState(this));
        }
    }

    /// <summary>
    /// 计算移动目标位置（单目标，用于点击移动）
    /// </summary>
    public virtual bool CalculateTargetPosition(out Vector3 target)
    {
        target = Vector3.zero;
        Vector2Int checkGrid = GetForwardOffset(out Vector2Int currentGrid, out Vector2Int forwardOffset);

        var map = Map.Instance;
        int rows = map.rows;
        int cols = map.cols;

        while (true)
        {
            if (checkGrid.x < 0 || checkGrid.x >= rows || checkGrid.y < 0 || checkGrid.y >= cols)
            {
                return false;
            }

            int occupantId = map.GetOccupantIdAtCell(checkGrid);
            if (occupantId != -1 && occupantId != mapItem.id)
            {
                behitItem = map.GetPlacedItem(occupantId)?.instance.GetComponent<AnimalBase>();

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
                        case 0: checkGrid = new Vector2Int(checkGrid.x, checkGrid.y-1); break;
                        case 1: checkGrid = new Vector2Int(checkGrid.x + 2, checkGrid.y); break;
                        case 2: checkGrid = new Vector2Int(checkGrid.x + 2, checkGrid.y + 2); break;
                        default: checkGrid = new Vector2Int(checkGrid.x - 1, checkGrid.y + 1); break;
                    }

                    Vector2Int obstacleGrid = new Vector2Int(checkGrid.x, checkGrid.y);
                    map.TryMoveItemTargetCell(mapItem, obstacleGrid, out target);
                    return true;
                }
            }
            checkGrid += forwardOffset;
        }
    }

    /// <summary>
    /// 获取撞击目标列表（支持多目标，如大象）。默认实现为单目标。
    /// </summary>
    /// <param name="hitAnimals">被撞击的动物列表</param>
    /// <param name="targetPositions">每个动物对应的目标世界坐标列表</param>
    /// <returns>是否至少有一个有效目标</returns>
    public virtual bool GetHitTargets(out List<AnimalBase> hitAnimals, out List<Vector3> targetPositions)
    {
        hitAnimals = new List<AnimalBase>();
        targetPositions = new List<Vector3>();

        if (CalculateTargetPosition(out Vector3 target))
        {
            if (behitItem != null)
            {
                hitAnimals.Add(behitItem);
                targetPositions.Add(target);
                return true;
            }
        }
        return false;
    }

    protected Vector2Int GetForwardOffset(out Vector2Int currentGrid, out Vector2Int forwardOffset)
    {
        currentGrid = mapItem.gridPos;
        switch (mapItem.rotIndex)
        {
            case 0: forwardOffset = new Vector2Int(1, 0); // 右
                currentGrid = new Vector2Int(mapItem.gridPos.x, mapItem.gridPos.y+1);
                break;
            case 1:
                currentGrid = new Vector2Int(mapItem.gridPos.x - 2, mapItem.gridPos.y);
                forwardOffset = new Vector2Int(0, 1); // 下
                break;
            case 2:
                currentGrid = new Vector2Int(mapItem.gridPos.x, mapItem.gridPos.y - 2);
                forwardOffset = new Vector2Int(-1, 0); // 左
                break;
            default:
                currentGrid = new Vector2Int(mapItem.gridPos.x+1, mapItem.gridPos.y); 
                forwardOffset = new Vector2Int(0, -1); // 上
                break;
        }
        return currentGrid + forwardOffset;
    }

    public bool IsOutOfScreen()
    {
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);
        return viewportPos.z < -0.05f ||
               viewportPos.x < -0.05f || viewportPos.x > 1.05f ||
               viewportPos.y < -0.05f || viewportPos.y > 1.05f;
    }

    // 状态接口
    public interface IAnimalState
    {
        void Enter();
        void Update();
        void Exit();
        void HandleClick() { }
    }
}