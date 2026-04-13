using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingObstacle : MonoBehaviour
{
    public float moveSpeed = 0.5f;
    public bool loop = true;
    public float startDelay = 1f; // 启动延迟（秒）

    private Map map;
    private MapItem mapItem;
    private List<Vector2Int> waypoints;
    private int currentTargetIndex = 1;
    private bool movingForward = true;
    private Vector2Int targetGrid;
    private Vector3 targetWorldPos;
    private bool isMoving = false;
    private bool hasCausedGameOver = false; // 防止重复触发游戏结束

    void Start()
    {
        map = Map.Instance;
        mapItem = GetComponent<MapItem>();

        // 从 mapItem.way 中读取路径点（假设已存储为 Vector2Int 列表）
        if (mapItem.way == null || mapItem.way.Count < 2)
        {
            Debug.LogWarning("MovingObstacle 路径点不足，禁用移动", this);
            enabled = false;
            return;
        }
        waypoints = new List<Vector2Int>();
        foreach (var wp in mapItem.way)
        {
            waypoints.Add(new Vector2Int((int)wp.x, (int)wp.y));
        }

        // 延迟启动移动
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(startDelay);
        SetNextTarget();
        isMoving = true;
    }

    void Update()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorldPos) < 0.05f)
        {
            OnReachWaypoint();
        }
    }

    private void SetNextTarget()
    {
        if (movingForward)
        {
            if (currentTargetIndex < waypoints.Count)
                targetGrid = waypoints[currentTargetIndex];
            else
            {
                if (loop)
                {
                    movingForward = false;
                    currentTargetIndex = waypoints.Count - 2;
                    targetGrid = waypoints[currentTargetIndex];
                }
                else
                {
                    isMoving = false;
                    return;
                }
            }
        }
        else
        {
            if (currentTargetIndex >= 0)
                targetGrid = waypoints[currentTargetIndex];
            else
            {
                if (loop)
                {
                    movingForward = true;
                    currentTargetIndex = 1;
                    targetGrid = waypoints[currentTargetIndex];
                }
                else
                {
                    isMoving = false;
                    return;
                }
            }
        }

        // 计算目标位置（占用矩形中心）
        var dims = map.FootprintDims(mapItem.info, mapItem.rotIndex);
        var anchor = map.StartFromPivot(targetGrid, mapItem.info, mapItem.rotIndex);
        targetWorldPos = map.FootprintWorldCenter(anchor, dims);
        targetWorldPos.y = transform.position.y;
    }

    private void OnReachWaypoint()
    {
        UpdateGridPosition(targetGrid);

        if (movingForward)
            currentTargetIndex++;
        else
            currentTargetIndex--;

        SetNextTarget();
    }

    private void UpdateGridPosition(Vector2Int newGrid)
    {
        Vector2Int oldGrid = mapItem.gridPos;
        if (oldGrid == newGrid) return;

        ClearCurrentOccupancy(oldGrid);
        mapItem.gridPos = newGrid;

        Map.PlacedItem placed = map.GetPlacedItem(mapItem.id);
        if (placed != null)
        {
            placed.gridPos = newGrid;
            placed.occupiedCells = map.ComputeOccupiedCells(newGrid, mapItem.info, mapItem.rotIndex);
            map.MarkArea(placed);
        }
        else
        {
            var dims = map.FootprintDims(mapItem.info, mapItem.rotIndex);
            var anchor = map.StartFromPivot(newGrid, mapItem.info, mapItem.rotIndex);
            for (int r = 0; r < dims.x; r++)
                for (int c = 0; c < dims.y; c++)
                    map.occupancy[anchor.x + r, anchor.y + c] = mapItem.id;
        }
    }

    private void ClearCurrentOccupancy(Vector2Int gridPos)
    {
        Map.PlacedItem placed = map.GetPlacedItem(mapItem.id);
        if (placed != null)
        {
            map.ClearArea(placed);
        }
        else
        {
            var dims = map.FootprintDims(mapItem.info, mapItem.rotIndex);
            var anchor = map.StartFromPivot(gridPos, mapItem.info, mapItem.rotIndex);
            for (int r = 0; r < dims.x; r++)
                for (int c = 0; c < dims.y; c++)
                    map.occupancy[anchor.x + r, anchor.y + c] = -1;
        }
    }

    // ==================== 碰撞检测逻辑 ====================
    private void OnTriggerEnter(Collider other)
    {
        // 如果已经触发过游戏结束，不再重复触发
        if (hasCausedGameOver) return;

        // 检测碰撞对象是否为动物
        AnimalBase animal = other.GetComponent<AnimalBase>();
        if (animal != null)
        {
            hasCausedGameOver = true;
            // 触发游戏失败
            GameManager.instance.GameOver(false);
        }
    }
}