using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChickItem : AnimalBase
{
    [Header("小鸡自动移动设置")]
    [SerializeField] private float detectionInterval = 0.5f;  // 检测间隔时间
    [SerializeField] private bool autoMoveEnabled = true;     // 是否启用自动移动
    [SerializeField] private float waypointReachDistance = 0.1f; // 到达路点的距离阈值
    
    [Header("视觉效果")]
    [SerializeField] private Color pathColor = Color.green;    // 路径颜色
    [SerializeField] private float directionChangeDelay = 0.3f;   // 方向改变延迟
    
    private Coroutine autoMoveCoroutine;
    private bool isMoving = false;
    private Vector3 lastPosition;
    private float stuckTime = 0f;
    private List<Vector2Int> currentPath = new List<Vector2Int>(); // 当前寻路路径
    private int currentPathIndex = 0;
    private bool isFindingPath = false;
    
    // 公共属性
    public bool IsAutoMoving => isMoving;
    
    protected override void Start()
    {
        base.Start();
        
        // 设置小鸡类型
        if (mapItem != null)
        {
            mapItem.animalType = (int)AnimalType.Chick;
        }
        
        // 开始自动移动检测
        if (autoMoveEnabled)
        {
            StartAutoMove();
        }
        
        Debug.Log("小鸡已激活，将自动寻找跑道并跑向终点");
    }
    
    protected void OnDestroy()
    {
        StopAutoMove();
    }
    
    protected override void Update()
    {
        base.Update();
        
        // 自动移动时的额外逻辑
        if (isMoving && autoMoveEnabled)
        {
            // 检测是否卡住
            if (Vector3.Distance(transform.position, lastPosition) < 0.01f)
            {
                stuckTime += Time.deltaTime;
                if (stuckTime > 1f)
                {
                    Debug.Log("小鸡卡住，重新寻路");
                    stuckTime = 0f;
                    RecalculatePath();
                }
            }
            else
            {
                stuckTime = 0f;
                lastPosition = transform.position;
            }
        }
    }
    
    /// <summary>
    /// 开始自动移动
    /// </summary>
    private void StartAutoMove()
    {
        if (autoMoveCoroutine != null)
            StopCoroutine(autoMoveCoroutine);
        
        autoMoveCoroutine = StartCoroutine(AutoMoveCoroutine());
    }
    
    /// <summary>
    /// 停止自动移动
    /// </summary>
    private void StopAutoMove()
    {
        if (autoMoveCoroutine != null)
        {
            StopCoroutine(autoMoveCoroutine);
            autoMoveCoroutine = null;
        }
        
        if (isMoving)
        {
            StopMoving();
        }
    }
    
    /// <summary>
    /// 自动移动协程
    /// </summary>
    private IEnumerator AutoMoveCoroutine()
    {
        while (autoMoveEnabled && !isMoving)
        {
            // 寻找通往跑道的路径
            if (!isFindingPath)
            {
                isFindingPath = true;
                bool foundPath = FindPathToRunway();
                isFindingPath = false;
                
                if (foundPath)
                {
                    // 找到路径，开始移动
                    StartMovingAlongPath();
                    yield break;
                }
            }
            
            // 等待下次检测
            yield return new WaitForSeconds(detectionInterval);
        }
        
        // 如果正在移动，持续移动直到跑出
        while (isMoving && autoMoveEnabled)
        {
            yield return null;
        }
    }
    
    /// <summary>
    /// 寻找到达跑道的路径（BFS算法）
    /// </summary>
    private bool FindPathToRunway()
    {
        Vector2Int startPos = mapItem.gridPos;
        
        // 使用BFS寻路
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue(startPos);
        visited.Add(startPos);
        cameFrom[startPos] = startPos;
        
        // 四个方向
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // 上
            new Vector2Int(1, 0),   // 右
            new Vector2Int(0, -1),  // 下
            new Vector2Int(-1, 0)   // 左
        };
        
        Vector2Int runwayPosition = Vector2Int.zero;
        bool foundRunway = false;
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            
            // 检查当前格子是否在跑道上
            if (IsCellOnRunway(current))
            {
                runwayPosition = current;
                foundRunway = true;
                break;
            }
            
            // 检查四个方向
            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir;
                
                // 检查边界
                if (next.x < 0 || next.x >= Map.Instance.rows ||
                    next.y < 0 || next.y >= Map.Instance.cols)
                {
                    continue;
                }
                
                // 检查是否已访问
                if (visited.Contains(next))
                    continue;
                
                // 检查是否有障碍物
                if (IsCellBlocked(next))
                    continue;
                
                visited.Add(next);
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }
        
        // 如果找到跑道，重建路径
        if (foundRunway)
        {
            currentPath.Clear();
            Vector2Int pathNode = runwayPosition;
            
            while (pathNode != startPos)
            {
                currentPath.Insert(0, pathNode);
                pathNode = cameFrom[pathNode];
            }
            
            // 显示寻路路径（调试用）
            ShowPath();
            
            Debug.Log($"小鸡找到通往跑道的路径，需要移动 {currentPath.Count} 步");
            return true;
        }
        
        Debug.Log("小鸡未找到通往跑道的路径");
        return false;
    }
    
    /// <summary>
    /// 检查格子是否在跑道上
    /// </summary>
    private bool IsCellOnRunway(Vector2Int gridPos)
    {
        // 获取该位置的跑道触发器
        Vector3 worldPos = Map.Instance.GridToWorld(gridPos);
        Collider[] colliders = Physics.OverlapSphere(worldPos, 0.5f);
        
        foreach (Collider col in colliders)
        {
            if (col.GetComponent<RunwayTrigger>() != null)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查格子是否被阻挡
    /// </summary>
    private bool IsCellBlocked(Vector2Int gridPos)
    {
        // 检查是否有其他动物占据
        int occupantId = Map.Instance.GetOccupantIdAtCell(gridPos);
        if (occupantId != -1 && occupantId != mapItem.id)
        {
            return true;
        }
        
        // 检查是否有障碍物（可以根据需要添加更多检测）
        return false;
    }
    
    /// <summary>
    /// 显示寻路路径（调试用）
    /// </summary>
    private void ShowPath()
    {
        // 清除之前的路径显示
        GameObject[] oldPaths = GameObject.FindGameObjectsWithTag("PathIndicator");
        foreach (GameObject obj in oldPaths)
        {
            Destroy(obj);
        }
        
        // 显示新路径
        foreach (Vector2Int gridPos in currentPath)
        {
            Vector3 worldPos = Map.Instance.GridToWorld(gridPos);
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.transform.position = worldPos;
            indicator.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f);
            indicator.GetComponent<Renderer>().material.color = pathColor;
            indicator.tag = "PathIndicator";
            Destroy(indicator, 0.5f);
        }
    }
    
    /// <summary>
    /// 沿着找到的路径开始移动
    /// </summary>
    private void StartMovingAlongPath()
    {
        if (isMoving || currentPath.Count == 0) return;
        
        isMoving = true;
        currentPathIndex = 0;
        lastPosition = transform.position;
        
        // 播放移动动画
        if (animator != null)
        {
            animator.SetBool("IsRun", true);
        }
        
        // 播放移动音效
        //AudioManager.Instance.PlaySoundEffect(moveSound);
        
        Debug.Log($"小鸡开始沿路径移动，共 {currentPath.Count} 个路点");
        
        // 开始移动到第一个路点
        StartCoroutine(MoveToNextWaypoint());
    }
    
    /// <summary>
    /// 移动到下一个路点
    /// </summary>
    private IEnumerator MoveToNextWaypoint()
    {
        while (currentPathIndex < currentPath.Count && isMoving)
        {
            Vector2Int targetGrid = currentPath[currentPathIndex];
            Vector3 targetPos = Map.Instance.GridToWorld(targetGrid);
            
            // 计算移动方向
            Vector3 moveDirection = (targetPos - transform.position).normalized;
            if (moveDirection != Vector3.zero)
            {
                transform.forward = moveDirection;
            }
            
            // 移动到目标点
            while (Vector3.Distance(transform.position, targetPos) > waypointReachDistance)
            {
                if (!isMoving) yield break;
                
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                yield return null;
            }
            
            // 到达当前路点
            currentPathIndex++;
            
            // 检查是否到达跑道
            if (currentPathIndex >= currentPath.Count)
            {
                // 到达跑道入口，进入跑道系统
                EnterRunwaySystem();
                yield break;
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 进入跑道系统
    /// </summary>
    private void EnterRunwaySystem()
    {
        Debug.Log("小鸡到达跑道入口，准备进入跑道");
        
        // 播放进入跑道音效
        //AudioManager.Instance.PlaySoundEffect(enterRunwaySound);
        
        // 显示进入跑道特效
        ShowEnterRunwayEffect();
        
        // 查找附近的跑道触发器
        Collider[] colliders = Physics.OverlapSphere(transform.position, 1f);
        foreach (Collider col in colliders)
        {
            RunwayTrigger trigger = col.GetComponent<RunwayTrigger>();
            if (trigger != null)
            {
                // 进入跑道
                RunwayPath runwayPath = trigger.GetComponent<RunwayPath>();
                if (runwayPath != null)
                {
                    EnterRunway(runwayPath, transform.position);
                    break;
                }
            }
        }
        
        // 停止路径移动
        isMoving = false;
    }
    
    /// <summary>
    /// 重写进入跑道方法
    /// </summary>
    public override void EnterRunway(RunwayPath runway, Vector3 enterPos)
    {
        base.EnterRunway(runway, enterPos);
        
        Debug.Log("小鸡已进入跑道，将沿着跑道跑向终点");
        
        // 播放进入跑道特效
        ShowEnterRunwayEffect();
    }
    
    /// <summary>
    /// 停止移动
    /// </summary>
    private void StopMoving()
    {
        isMoving = false;
        currentPath.Clear();
        currentPathIndex = 0;
        
        if (animator != null)
        {
            animator.SetBool("IsRun", false);
        }
        
        // 重新开始检测
        if (autoMoveEnabled)
        {
            StartAutoMove();
        }
    }
    
    /// <summary>
    /// 重新计算路径
    /// </summary>
    private void RecalculatePath()
    {
        if (!isMoving) return;
        
        StopMoving();
        StartCoroutine(DelayedRecalculate());
    }
    
    private IEnumerator DelayedRecalculate()
    {
        yield return new WaitForSeconds(directionChangeDelay);
        
        if (autoMoveEnabled && !isMoving)
        {
            StartAutoMove();
        }
    }
    
    /// <summary>
    /// 显示进入跑道特效
    /// </summary>
    private void ShowEnterRunwayEffect()
    {
        GameObject effectObj = new GameObject("EnterRunwayEffect");
        effectObj.transform.position = transform.position + Vector3.up * 0.5f;
        
        var textMesh = effectObj.AddComponent<TextMesh>();
        textMesh.text = "🏃 进入跑道！ 🏃";
        textMesh.color = Color.green;
        textMesh.fontSize = 35;
        textMesh.characterSize = 0.05f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        effectObj.AddComponent<Billboard>();
        StartCoroutine(FadeAndDestroy(effectObj, 0.8f));
    }
    
    /// <summary>
    /// 淡出并销毁物体
    /// </summary>
    private IEnumerator FadeAndDestroy(GameObject obj, float duration)
    {
        float elapsed = 0;
        TextMesh textMesh = obj.GetComponent<TextMesh>();
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (textMesh != null)
            {
                Color color = textMesh.color;
                color.a = 1 - (elapsed / duration);
                textMesh.color = color;
            }
            obj.transform.Translate(Vector3.up * Time.deltaTime * 0.5f);
            yield return null;
        }
        
        Destroy(obj);
    }
    
    /// <summary>
    /// 重写被撞击方法
    /// </summary>
    public override void BeHit()
    {
        if (isMoving)
        {
            // 被撞击时重新寻路
            StartCoroutine(HitAndRecalculate());
        }
        
        base.BeHit();
    }
    
    private IEnumerator HitAndRecalculate()
    {
        // 临时减速
        float originalSpeed = speed;
        speed *= 0.5f;
        
        yield return new WaitForSeconds(0.3f);
        
        speed = originalSpeed;
        
        // 重新寻路
        if (isMoving)
        {
            StopMoving();
            yield return new WaitForSeconds(0.2f);
            StartAutoMove();
        }
    }
    
    /// <summary>
    /// 重写点击方法
    /// </summary>
    protected override void OnMouseUpAsButton()
    {
        if (!autoMoveEnabled)
        {
            base.OnMouseUpAsButton();
        }
        else
        {
            ShowTip();
        }
    }
    
    private void ShowTip()
    {
        string tips =  "🐔 小鸡会自动寻找跑道并跑向终点！";
        MessageSystem.Instance.ShowTip(tips);
    }
}