using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ChickItem : AnimalBase
{
    [Header("小鸡自动移动设置")]
    [SerializeField] private float detectionInterval = 0.5f;
    [SerializeField] private bool autoMoveEnabled = true;
    
    [Header("视觉效果")]
    [SerializeField] private Color pathColor = Color.green;
    [SerializeField] private float directionChangeDelay = 0.3f;
    
    private Coroutine autoMoveCoroutine;
    private bool isMoving = false;
    private Vector3 lastPosition;
    private float stuckTime = 0f;
    private List<Vector2Int> currentPath = new List<Vector2Int>();
    private bool isFindingPath = false;
    private Vector2Int escapeDirection = Vector2Int.zero;
    
    public bool IsAutoMoving => isMoving;
    
    private void OnEnable()
    {
        //CreateCountDisplay();
        
        if (mapItem != null)
        {
            mapItem.animalType = (int)AnimalType.Chick;
        }
        
        if (autoMoveEnabled)
        {
            DOVirtual.DelayedCall(0.5f, () => {
                StartAutoMove();
            });
        }
        
        Debug.Log($"小鸡已激活，当前位置网格: {mapItem.gridPos}");
    }
    

    protected void OnDestroy()
    {
        StopAutoMove();
    }
    
    protected override void Update()
    {
        base.Update();
        
        // 卡住检测
        if (isMoving && autoMoveEnabled && CurrentState is AutoMovingState)
        {
            if (Vector3.Distance(transform.position, lastPosition) < 0.01f)
            {
                stuckTime += Time.deltaTime;
                if (stuckTime > 1f)
                {
                    Debug.Log("小鸡卡住，重新寻路");
                    // stuckTime = 0f;
                    // RecalculatePath();
                }
            }
            else
            {
                stuckTime = 0f;
                lastPosition = transform.position;
            }
        }
    }
    
    
    public void StartAutoMove()
    {
        if (autoMoveCoroutine != null)
            StopCoroutine(autoMoveCoroutine);
        
        autoMoveCoroutine = StartCoroutine(AutoMoveCoroutine());
    }
    
    private void StopAutoMove()
    {
        if (autoMoveCoroutine != null)
        {
            StopCoroutine(autoMoveCoroutine);
            autoMoveCoroutine = null;
        }
    }
    
    private IEnumerator AutoMoveCoroutine()
    {
        while (autoMoveEnabled && !isMoving)
        {
            if (!isFindingPath)
            {
                isFindingPath = true;
                bool foundPath = FindPathToExit();
                isFindingPath = false;
                
                if (foundPath && currentPath.Count > 0)
                {
                    StartMovingAlongPath();
                    yield break;
                }
            }
            
            yield return new WaitForSeconds(detectionInterval);
        }
        
        while (isMoving && autoMoveEnabled)
        {
            yield return null;
        }
    }
    
    /// <summary>
    /// 寻找到达地图边界的路径（BFS算法）
    /// </summary>
    private bool FindPathToExit()
    {
        Vector2Int startPos = mapItem.gridPos;
        
        // 如果已经在边界上，直接跑出
        if (IsOnBoundary(startPos))
        {
            Debug.Log("小鸡已经在边界上，直接跑出");
            StartCoroutine(EscapeImmediately());
            return false;
        }
        
        // 使用BFS寻路
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue(startPos);
        visited.Add(startPos);
        cameFrom[startPos] = startPos;
        
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // 上
            new Vector2Int(1, 0),   // 右
            new Vector2Int(0, -1),  // 下
            new Vector2Int(-1, 0)   // 左
        };
        
        Vector2Int exitPosition = Vector2Int.zero;
        Vector2Int exitDir = Vector2Int.zero;
        bool foundExit = false;
        int shortestDistance = int.MaxValue;
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int currentDist = GetDistance(current, startPos);
            
            if (IsOnBoundary(current))
            {
                if (currentDist < shortestDistance)
                {
                    shortestDistance = currentDist;
                    exitPosition = current;
                    
                    if (current.x == 0) exitDir = new Vector2Int(-1, 0);
                    else if (current.x == Map.Instance.rows - 1) exitDir = new Vector2Int(1, 0);
                    else if (current.y == 0) exitDir = new Vector2Int(0, -1);
                    else if (current.y == Map.Instance.cols - 1) exitDir = new Vector2Int(0, 1);
                    
                    foundExit = true;
                    
                    if (currentDist == 0)
                        break;
                }
            }
            
            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir;
                
                if (next.x < 0 || next.x >= Map.Instance.rows ||
                    next.y < 0 || next.y >= Map.Instance.cols)
                {
                    continue;
                }
                
                if (visited.Contains(next))
                    continue;
                
                if (IsCellBlocked(next))
                    continue;
                
                visited.Add(next);
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }
        
        if (foundExit)
        {
            currentPath.Clear();
            Vector2Int pathNode = exitPosition;
            
            while (pathNode != startPos)
            {
                currentPath.Insert(0, pathNode);
                pathNode = cameFrom[pathNode];
            }
            
            escapeDirection = exitDir;
            ShowPath();
            
            Debug.Log($"小鸡找到跑出边界的路径，需要移动 {currentPath.Count} 步，跑出方向: {exitDir}");
            
            return true;
        }
        
        Debug.Log("小鸡未找到跑出边界的路径");
        return false;
    }
    
    /// <summary>
    /// 沿着找到的路径开始移动
    /// </summary>
    private void StartMovingAlongPath()
    {
        if (isMoving || currentPath.Count == 0) return;
        
        isMoving = true;
        lastPosition = transform.position;
        
        Debug.Log($"小鸡开始沿路径移动，共 {currentPath.Count} 个路点");
        
        // 切换到自动移动状态
        ChangeState(new AutoMovingState(this, currentPath, escapeDirection));
    }
    
    public override void EnterRunway(RunwayPath runway, Vector3 enterPos)
    {
        if (currentState is AutoMovingState)
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
    
    /// <summary>
    /// 立即跑出边界（当已经在边界上时）
    /// </summary>
    private IEnumerator EscapeImmediately()
    {
        Vector3 escapeWorldDir = GetEscapeWorldDirection(mapItem.gridPos);
        
        if (escapeWorldDir != Vector3.zero)
        {
            if (animator != null)
            {
                animator.SetBool("IsRun", true);
                transform.forward = escapeWorldDir;
            }
            
            float escapeDistance = 2f;
            Vector3 targetPos = transform.position + escapeWorldDir * escapeDistance;
            
            float elapsed = 0;
            float duration = 0.5f;
            Vector3 startPos = transform.position;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
            
            // 触发跑出事件
            //Map.Instance.OnAnimalEscaped?.Invoke(this);
            Map.Instance.RemoveItem(mapItem);
            Destroy(gameObject);
        }
    }
    
    private bool IsOnBoundary(Vector2Int gridPos)
    {
        return gridPos.x == 0 || gridPos.x == Map.Instance.rows - 1 ||
               gridPos.y == 0 || gridPos.y == Map.Instance.cols - 1;
    }
    
    private int GetDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
    
    private bool IsCellBlocked(Vector2Int gridPos)
    {
        int occupantId = Map.Instance.GetOccupantIdAtCell(gridPos);
        if (occupantId != -1 && occupantId != mapItem.id)
        {
            return true;
        }
        
        return false;
    }
    
    private Vector3 GetEscapeWorldDirection(Vector2Int gridPos)
    {
        if (gridPos.x == 0)
            return Vector3.left;
        else if (gridPos.x == Map.Instance.rows - 1)
            return Vector3.right;
        else if (gridPos.y == 0)
            return Vector3.back;
        else if (gridPos.y == Map.Instance.cols - 1)
            return Vector3.forward;
        
        return Vector3.zero;
    }
    
    private void ShowPath()
    {
        GameObject[] oldPaths = GameObject.FindGameObjectsWithTag("PathIndicator");
        foreach (GameObject obj in oldPaths)
        {
            Destroy(obj);
        }
        
        foreach (Vector2Int gridPos in currentPath)
        {
            Vector3 worldPos = Map.Instance.GridToWorld(gridPos);
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.transform.position = worldPos;
            indicator.transform.localScale = new Vector3(0.6f, 0.1f, 0.6f);
            indicator.GetComponent<Renderer>().material.color = pathColor;
            indicator.tag = "PathIndicator";
            Destroy(indicator, 0.8f);
        }
    }
    
    private void RecalculatePath()
    {
        if (!isMoving) return;
        
        // 停止当前移动
        isMoving = false;
        currentPath.Clear();
        
        // 重新开始寻路
        if (autoMoveEnabled)
        {
            StartCoroutine(DelayedRecalculate());
        }
    }
    
    private IEnumerator DelayedRecalculate()
    {
        yield return new WaitForSeconds(directionChangeDelay);
        
        if (autoMoveEnabled && !isMoving)
        {
            StartAutoMove();
        }
    }
    
    public override void BeHit()
    {
        if (isMoving)
        {
            StartCoroutine(HitAndRecalculate());
        }
        
        base.BeHit();
    }
    
    private IEnumerator HitAndRecalculate()
    {
        float originalSpeed = speed;
        speed *= 0.5f;
        
        yield return new WaitForSeconds(0.3f);
        
        speed = originalSpeed;
        
        if (isMoving)
        {
            isMoving = false;
            currentPath.Clear();
            yield return new WaitForSeconds(0.2f);
            StartAutoMove();
        }
    }
    
    protected override void OnMouseUpAsButton()
    {
        if (!autoMoveEnabled)
        {
            base.OnMouseUpAsButton();
        }
        else
        {
            string tips = "🐔 小鸡会自动寻找出口跑出网格！";
            MessageSystem.Instance.ShowTip(tips);
        }
    }
}