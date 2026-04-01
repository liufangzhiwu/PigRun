using System;
using System.Collections;
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
    protected AnimalBase behitItem;          // 将要撞击的物体
    protected IAnimalState currentState;
    protected Vector2Int startGrid;

    // 跑道相关
    [HideInInspector] public RunwayPath currentRunway;
    public int currentSegmentIndex;
    protected bool isOnRunway;
    
    public event Action<AnimalBase> OnAnimalClicked;

    // 性能分析开关（可在运行时动态开启/关闭）
    public static bool EnableProfiling = true;  // 建议在游戏启动时设置为 true，分析后改为 false

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
        float startTime = 0f;
        if (EnableProfiling)
        {
            startTime = Time.realtimeSinceStartup;
        }

        if (UIManager.Instance.IsPanelTypeShowing() || !UIManager.Instance.PanelIsShowing(PanelType.GamePanel))
        {
            Debug.Log("进入弹窗界面，不触发动物逻辑");
            if (EnableProfiling)
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                if (elapsed > 0.001f)
                    Debug.LogWarning($"[Perf] OnMouseUpAsButton (early exit) on {name} took {elapsed * 1000:F2} ms");
            }
            return;
        }

        // 只有在选择模式下触发事件
        if (OnAnimalClicked != null && mapItem != null)
        {
            OnAnimalClicked(this);
        }

        if (OnAnimalClicked == null)
        {
            currentState?.HandleClick();
        }

        if (EnableProfiling)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            if (elapsed > 0.005f) // 超过5毫秒输出警告
                Debug.LogWarning($"[Perf] OnMouseUpAsButton on {name} took {elapsed * 1000:F2} ms");
        }
    }

    /// <summary>
    /// 被其他物体撞击时调用（外部触发）
    /// </summary>
    public virtual void BeHit()
    {
        if (EnableProfiling)
        {
            float start = Time.realtimeSinceStartup;
            ChangeState(new BeHitState(this));
            float elapsed = Time.realtimeSinceStartup - start;
            if (elapsed > 0.002f)
                Debug.LogWarning($"[Perf] BeHit on {name} took {elapsed * 1000:F2} ms");
        }
        else
        {
            ChangeState(new BeHitState(this));
        }
    }

    /// <summary>
    /// 自身撞击障碍时调用（内部触发）
    /// </summary>
    public virtual void HitSelf()
    {
        if (EnableProfiling)
        {
            float start = Time.realtimeSinceStartup;
            ChangeState(new HitState(this));
            float elapsed = Time.realtimeSinceStartup - start;
            if (elapsed > 0.002f)
                Debug.LogWarning($"[Perf] HitSelf on {name} took {elapsed * 1000:F2} ms");
        }
        else
        {
            ChangeState(new HitState(this));
        }
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    public void ChangeState(IAnimalState newState)
    {
        if (EnableProfiling)
        {
            float start = Time.realtimeSinceStartup;
            currentState?.Exit();
            currentState = newState;
            currentState?.Enter();
            float elapsed = Time.realtimeSinceStartup - start;
            if (elapsed > 0.001f)
                Debug.LogWarning($"[Perf] ChangeState to {newState.GetType().Name} on {name} took {elapsed * 1000:F2} ms");
        }
        else
        {
            currentState?.Exit();
            currentState = newState;
            currentState?.Enter();
        }
    }

    public virtual void EnterRunway(RunwayPath runway, Vector3 enterPos)
    {
        if (currentState is MovingState)
        {
            if (EnableProfiling)
            {
                float start = Time.realtimeSinceStartup;
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
                float elapsed = Time.realtimeSinceStartup - start;
                if (elapsed > 0.005f)
                    Debug.LogWarning($"[Perf] EnterRunway on {name} took {elapsed * 1000:F2} ms");
            }
            else
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
    /// 计算移动目标位置（用于点击）
    /// </summary>
    public virtual bool CalculateTargetPosition(out Vector3 target)
    {
        float startTime = 0f;
        if (EnableProfiling)
        {
            startTime = Time.realtimeSinceStartup;
        }

        target = Vector3.zero;
        Vector2Int checkGrid = GetForwardOffset(out Vector2Int currentGrid, out Vector2Int forwardOffset);

        // 缓存 Map 实例和尺寸，减少属性访问
        var map = Map.Instance;
        int rows = map.rows;
        int cols = map.cols;

        while (true)
        {
            if (checkGrid.x < 0 || checkGrid.x >= rows || checkGrid.y < 0 || checkGrid.y >= cols)
            {
                if (EnableProfiling)
                {
                    float elapsed = Time.realtimeSinceStartup - startTime;
                    if (elapsed > 0.002f)
                        Debug.LogWarning($"[Perf] CalculateTargetPosition (exit boundary) on {name} took {elapsed * 1000:F2} ms");
                }
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
                    if (EnableProfiling)
                    {
                        float elapsed = Time.realtimeSinceStartup - startTime;
                        if (elapsed > 0.002f)
                            Debug.LogWarning($"[Perf] CalculateTargetPosition (adjacent obstacle) on {name} took {elapsed * 1000:F2} ms");
                    }
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
                    map.TryMoveItemTargetCell(mapItem, obstacleGrid, out target);
                    if (EnableProfiling)
                    {
                        float elapsed = Time.realtimeSinceStartup - startTime;
                        if (elapsed > 0.002f)
                            Debug.LogWarning($"[Perf] CalculateTargetPosition (non-adjacent obstacle) on {name} took {elapsed * 1000:F2} ms");
                    }
                    return true;
                }
            }
            checkGrid += forwardOffset;
        }
    }

    protected Vector2Int GetForwardOffset(out Vector2Int currentGrid, out Vector2Int forwardOffset)
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