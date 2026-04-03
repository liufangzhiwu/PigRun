using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using static Map;
using Random = UnityEngine.Random;

/// <summary>
/// 地图管理器核心类
/// 负责网格管理、占用检测、预制体放置、旋转、移动和序列化
/// 支持编辑器编辑和运行时交互
/// </summary>
public class Map : MonoBehaviour
{
    // ==================== 单例引用 ====================
    public static Map Instance;

    // ==================== 地图网格配置 ====================
    [Header("地图网格")]
    public int rows = 10;
    public int cols = 10;
    private float cellSize = 0.18f;          // 每个网格的单位尺寸（世界单位）
    public Vector3 origin;                   // 地图原点（本地坐标）

    /// <summary>所有地图项被销毁时触发的事件（关卡完成）</summary>
    public event System.Action OnAllItemsDestroyed;
    /// <summary>新地图加载完成时触发的事件（用于 UI 更新）</summary>
    public event System.Action OnLoadNewMap;

    // ==================== 视觉配置 ====================
    [Header("视觉效果")]
    public Color gridColor = Color.gray;
    public Color selectedColor = Color.red;
    public bool selectedUseComplementary = false;
    public float gridLineWidth = 1f;
    public Sprite grid01 = null;
    public Sprite grid02 = null;

    [Tooltip("运行时在地图网格上显示每格占用 id，便于检查占用表是否正确")]
    public bool showOccupancyTable = true;

    public bool LevelFinish = false;          // 是否已完成关卡
    public MedicineCowItem medicineCowItem;   // 药牛引用（用于关联）
    public SickDonkeyItem sickDonkeyItem;     // 病驴引用（用于关联）

    /// <summary>动物跑出事件（用于乌龟任务计数等）</summary>
    public delegate void AnimalEscapedDelegate(AnimalBase animal);
    public event AnimalEscapedDelegate OnAnimalEscaped;

    // ==================== 数据资产 ====================
    public MapData dataAsset;

    // ==================== 运行时内部数据结构 ====================
    /// <summary>已放置物品的运行时数据</summary>
    public class PlacedItem
    {
        public int id;                      // 唯一标识符
        public PrefabInfo info;             // 预制体配置信息
        public Vector2Int gridPos;          // 锚点网格坐标
        public int rotIndex;                // 旋转索引
        public GameObject instance;         // 实例化的游戏对象
        public Quaternion baseRotation;     // 基础旋转
        public List<Vector2Int> occupiedCells;   // 预计算的所有占用格子（优化性能）
    }

    // ==================== 占用表和实例管理 ====================
    public int[,] occupancy;                                      // 网格占用表，-1 表示空闲，其他为物品 ID
    private Dictionary<int, PlacedItem> items = new Dictionary<int, PlacedItem>();
    private int nextId = 1;
    private PlacedItem selected;                                 // 当前选中的物品

    // ==================== 拾取相关 ====================
    private Plane plane;
    private Camera cam;

    // ==================== 初始化方法 ====================
    private void Awake()
    {
        Instance = this;
        Physics.queriesHitTriggers = true;

        // 初始化占用表
        occupancy = new int[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                occupancy[r, c] = -1;

        plane = new Plane(transform.up, transform.TransformPoint(origin));
        cam = Camera.main;

        // 加载网格纹理（用于 UI 显示）
        grid01 = Resources.Load<Sprite>("UI/grid01");
        grid02 = Resources.Load<Sprite>("UI/grid02");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ==================== 占用表管理 ====================
    /// <summary>重置整个占用表为 -1</summary>
    public void ResetOccupancy()
    {
        occupancy = new int[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                occupancy[r, c] = -1;
    }

    /// <summary>触发新地图加载完成事件（供外部调用）</summary>
    public void OnLoadNewMapEvent()
    {
        OnLoadNewMap?.Invoke();
    }

    // ==================== 占用计算工具方法 ====================
    /// <summary>根据旋转索引计算占用矩形的尺寸（行列数）</summary>
    public Vector2Int FootprintDims(PrefabInfo info, int rotIndex)
    {
        if (info == null) return new Vector2Int(1, 1);
        // 偶数旋转（0°、180°）尺寸不变，奇数旋转（90°、270°）行列互换
        return rotIndex % 2 == 0 ? new Vector2Int(info.rows, info.cols) : new Vector2Int(info.cols, info.rows);
    }

    /// <summary>顺时针旋转后，锚点在占用矩阵中的行列索引映射</summary>
    private Vector2Int RotatedPivot(int rotIndex, Vector2Int dims0)
    {
        rotIndex = ((rotIndex % 4) + 4) % 4; // 归一化到 [0,3]
        switch (rotIndex)
        {
            case 0: return new Vector2Int(dims0.x, 1);
            case 1: return new Vector2Int(dims0.y, dims0.x);
            case 2: return new Vector2Int(1, dims0.y);
            default: return new Vector2Int(0, 0);
        }
    }

    /// <summary>给定锚点网格坐标，计算占用矩形的左下角起点（anchor）</summary>
    private Vector2Int StartFromPivot(Vector2Int pivotGrid, PrefabInfo info, int rotIndex)
    {
        var dims0 = info != null ? new Vector2Int(info.rows, info.cols) : Vector2Int.one;
        var pivR = RotatedPivot(rotIndex, dims0);
        return new Vector2Int(pivotGrid.x - pivR.x, pivotGrid.y - pivR.y);
    }

    // ==================== 预计算占用格子 ====================
    /// <summary>计算物品占用的所有网格坐标（用于快速更新占用表）</summary>
    private List<Vector2Int> ComputeOccupiedCells(Vector2Int pivotGrid, PrefabInfo info, int rotIndex)
    {
        var dims = FootprintDims(info, rotIndex);
        var anchor = StartFromPivot(pivotGrid, info, rotIndex);
        var cells = new List<Vector2Int>(dims.x * dims.y);
        for (int r = 0; r < dims.x; r++)
            for (int c = 0; c < dims.y; c++)
                cells.Add(new Vector2Int(anchor.x + r, anchor.y + c));
        return cells;
    }

    // ==================== 高效占用表更新 ====================
    /// <summary>标记物品占用的所有格子</summary>
    private void MarkArea(PlacedItem item)
    {
        foreach (var cell in item.occupiedCells)
            occupancy[cell.x, cell.y] = item.id;
    }

    /// <summary>清除物品占用的所有格子（设为 -1）</summary>
    private void ClearArea(PlacedItem item)
    {
        foreach (var cell in item.occupiedCells)
            occupancy[cell.x, cell.y] = -1;
    }

    // ==================== 边界和占用检测 ====================
    /// <summary>检查占用矩形是否在地图边界内</summary>
    private bool InBounds(Vector2Int start, Vector2Int dims)
    {
        if (start.x < 0 || start.y < 0) return false;
        if (start.x + dims.x > rows) return false;
        if (start.y + dims.y > cols) return false;
        return true;
    }

    // ==================== 坐标转换 ====================
    /// <summary>将网格坐标转换为世界坐标（网格中心）</summary>
    public Vector3 GridToWorld(Vector2Int grid)
    {
        float lx = (grid.y + 0.5f) * cellSize + origin.x;
        float lz = (grid.x + 0.5f) * cellSize + origin.z;
        var local = new Vector3(lx, origin.y, lz);
        return transform.TransformPoint(local);
    }

    /// <summary>计算占用矩形的几何中心世界坐标</summary>
    public Vector3 FootprintWorldCenter(Vector2Int anchor, Vector2Int dims)
    {
        float lx = (anchor.y + dims.y * 0.5f) * cellSize + origin.x;
        float lz = (anchor.x + dims.x * 0.5f) * cellSize + origin.z;
        var local = new Vector3(lx, origin.y, lz);
        return transform.TransformPoint(local);
    }

    // ==================== Gizmos 绘制 ====================
    private void OnDrawGizmos()
    {
        // 绘制网格线
        Gizmos.color = gridColor;
        for (int r = 0; r <= rows; r++)
        {
            var a = transform.TransformPoint(origin + new Vector3(0f, 0f, r * cellSize));
            var b = transform.TransformPoint(origin + new Vector3(cols * cellSize, 0f, r * cellSize));
            Gizmos.DrawLine(a, b);
        }
        for (int c = 0; c <= cols; c++)
        {
            var a = transform.TransformPoint(origin + new Vector3(c * cellSize, 0f, 0f));
            var b = transform.TransformPoint(origin + new Vector3(c * cellSize, 0f, rows * cellSize));
            Gizmos.DrawLine(a, b);
        }

        // 绘制选中框
        if (selected != null)
        {
            var dims = FootprintDims(selected.info, selected.rotIndex);
            var anchor = StartFromPivot(selected.gridPos, selected.info, selected.rotIndex);
            var selCol = selectedUseComplementary
                ? new Color(1f - gridColor.r, 1f - gridColor.g, 1f - gridColor.b, 1f)
                : selectedColor;
            Gizmos.color = selCol;

            var min = origin + new Vector3(anchor.y * cellSize, 0f, anchor.x * cellSize);
            var max = origin + new Vector3((anchor.y + dims.y) * cellSize, 0f, (anchor.x + dims.x) * cellSize);
            var wmin = transform.TransformPoint(min);
            var wmax = transform.TransformPoint(max);
            Gizmos.DrawLine(wmin, new Vector3(wmax.x, wmin.y, wmin.z));
            Gizmos.DrawLine(new Vector3(wmax.x, wmin.y, wmin.z), wmax);
            Gizmos.DrawLine(wmax, new Vector3(wmin.x, wmin.y, wmax.z));
            Gizmos.DrawLine(new Vector3(wmin.x, wmin.y, wmax.z), wmin);
        }

#if UNITY_EDITOR
        // 运行时显示占用 ID（调试用）
        if (Application.isPlaying && showOccupancyTable && occupancy != null)
        {
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var worldPos = GridToWorld(new Vector2Int(r, c));
                    int id = occupancy[r, c];
                    string text = id >= 0 ? id.ToString() : "·";
                    UnityEditor.Handles.Label(worldPos + Vector3.up * 0.02f, text);
                }
        }
#endif
    }

    // ==================== 地图尺寸适配 ====================
    private static readonly int[] AvailableGridSizes = { 20, 23, 25, 30 };
    private static readonly float[] MapScales = { 1.1f, 0.9f, 0.9f, 0.9f };

    /// <summary>获取与目标尺寸最接近的可用网格尺寸</summary>
    public int GetClosestGridSize(int target)
    {
        int closest = AvailableGridSizes[0];
        int minDiff = Mathf.Abs(target - closest);
        for (int i = 1; i < AvailableGridSizes.Length; i++)
        {
            int diff = Mathf.Abs(target - AvailableGridSizes[i]);
            if (diff < minDiff)
            {
                minDiff = diff;
                closest = AvailableGridSizes[i];
            }
            else if (diff == minDiff && AvailableGridSizes[i] > closest)
                closest = AvailableGridSizes[i];
        }
        return closest;
    }
    
    /// <summary>实例化一个物品并注册到地图中（用于分批加载）</summary>
    public void InstantiateItem(MapData.MapItemData it)
    {
        if (it.info == null || it.info.prefab == null) return;

        var dims = FootprintDims(it.info, it.rotIndex);
        var anchor = StartFromPivot(it.gridPos, it.info, it.rotIndex);
        if (!InBounds(anchor, dims)) return;

        var obj = Instantiate(it.info.prefab, transform);
        var baseRot = obj.transform.rotation;
        obj.transform.rotation = Quaternion.AngleAxis(it.rotIndex * 90f, Vector3.up) * baseRot;
        obj.transform.position = FootprintWorldCenter(anchor, dims);

        var mi = obj.GetComponent<MapItem>();
        if (mi == null) mi = obj.AddComponent<MapItem>();
        mi.info = it.info;
        mi.gridPos = it.gridPos;
        mi.rotIndex = it.rotIndex;
        mi.baseRotation = baseRot;
        mi.animalType = it.animalType;
        mi.boomTime = it.boomTime;

        int id = nextId++;
        mi.id = id;

        var placed = new PlacedItem
        {
            id = id,
            info = it.info,
            gridPos = it.gridPos,
            rotIndex = it.rotIndex,
            instance = obj,
            baseRotation = baseRot,
            occupiedCells = ComputeOccupiedCells(it.gridPos, it.info, it.rotIndex)
        };
        items[id] = placed;
        MarkArea(placed);

        // 关联药牛和病驴（特殊逻辑）
        if ((AnimalType)it.animalType == AnimalType.Cattle)
        {
            medicineCowItem = mi.info.prefab.GetComponent<MedicineCowItem>();
            if (medicineCowItem != null && sickDonkeyItem != null)
                medicineCowItem.SetLinkedDonkey(sickDonkeyItem);
        }
        if ((AnimalType)it.animalType == AnimalType.Donkey)
        {
            sickDonkeyItem = mi.info.prefab.GetComponent<SickDonkeyItem>();
            if (sickDonkeyItem != null && medicineCowItem != null)
                medicineCowItem.SetLinkedDonkey(sickDonkeyItem);
        }
    }

    /// <summary>获取放置的物品数据（通过 ID）</summary>
    public PlacedItem GetPlacedItem(int id)
    {
        items.TryGetValue(id, out var item);
        return item;
    }

    /// <summary>清空所有地图物品</summary>
    public void ClearAllItems()
    {
        items.Clear();
        nextId = 1;
        selected = null;
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    // ==================== 公共 API ====================
    /// <summary>尝试将物品移动到新位置，并返回新的世界坐标</summary>
    public bool TryMoveItemTargetCell(MapItem item, Vector2Int targetPivot, out Vector3 target)
    {
        target = Vector3.zero;
        if (!items.TryGetValue(item.id, out var placed)) return false;

        ClearArea(placed);
        var dims = FootprintDims(item.info, item.rotIndex);
        var targetAnchor = StartFromPivot(targetPivot, item.info, item.rotIndex);
        item.gridPos = targetPivot;
        placed.gridPos = targetPivot;
        placed.occupiedCells = ComputeOccupiedCells(targetPivot, item.info, item.rotIndex);
        MarkArea(placed);
        target = FootprintWorldCenter(targetAnchor, dims);
        return true;
    }

    /// <summary>更新地图区域：释放物品占用的格子并移除记录（用于移动前释放）</summary>
    public void UpdateMapItemArea(MapItem item)
    {
        if (items.TryGetValue(item.id, out var placed))
        {
            ClearArea(placed);
            items.Remove(item.id);
        }
    }

    /// <summary>动物跑出时调用（触发事件并移除）</summary>
    public void RunOutRemoveItem(AnimalBase animal)
    {
        OnAnimalEscaped?.Invoke(animal);
        RemoveItem(animal.MapItem);
    }

    /// <summary>移除地图物品（释放占用并销毁实例）</summary>
    public bool RemoveItem(MapItem item)
    {
        UpdateMapItemArea(item);
        Destroy(item.gameObject);
        if (items.Count == 0 && !LevelFinish)
        {
            OnAllItemsDestroyed?.Invoke();
            LevelFinish = true;
        }
        return true;
    }

    /// <summary>获取指定网格的占用者 ID（-1 表示空闲）</summary>
    public int GetOccupantIdAtCell(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= rows || cell.y < 0 || cell.y >= cols) return -1;
        return occupancy[cell.x, cell.y];
    }
    
    /// <summary>
    /// 洗牌：随机翻转指定数量的动物（旋转180度）
    /// </summary>
    /// <param name="count">需要翻转的动物数量</param>
    public void ShuffleAnimals(int count)
    {
        // 获取所有普通动物（排除药牛、病驴等特殊动物）
        List<PlacedItem> animalItems = new List<PlacedItem>();
        foreach (var kv in items)
        {
            var placed = kv.Value;
            // 根据animalType判断是否为特殊动物？这里简单通过组件判断
            var animal = placed.instance.GetComponent<AnimalBase>();
            if (animal == null) continue;
            if (animal is MedicineCowItem || animal is SickDonkeyItem) continue;
            animalItems.Add(placed);
        }

        if (animalItems.Count == 0)
        {
            Debug.Log("没有可洗牌的动物");
            return;
        }

        // 随机选择 count 个（不超过总数）
        int shuffleCount = Mathf.Min(count, animalItems.Count);
        // 随机打乱列表
        for (int i = 0; i < shuffleCount; i++)
        {
            int randomIndex = Random.Range(i, animalItems.Count);
            var temp = animalItems[i];
            animalItems[i] = animalItems[randomIndex];
            animalItems[randomIndex] = temp;
        }

        // 对前 shuffleCount 个动物进行旋转180度
        for (int i = 0; i < shuffleCount; i++)
        {
            RotateAnimal180(animalItems[i]);
        }
    }


    /// <summary>
    /// 将动物旋转180度（使用 DOTween 动画）
    /// </summary>
   /// <summary>
/// 将动物旋转180度（使用 DOTween 动画）
/// </summary>
private void RotateAnimal180(PlacedItem placed)
{
    // 计算新的旋转索引（+2 模4）
    int newRotIndex = (placed.rotIndex + 2) % 4;

    // 获取 MapItem 组件
    MapItem mi = placed.instance.GetComponent<MapItem>();
    if (mi == null) return;

    // 清除当前占用（防止动画期间其他动物进入该格子）
    //ClearArea(placed);

    // 获取当前的网格位置
    Vector2Int originalGridPos = mi.gridPos;
    
    // 计算旋转后的新网格位置
    // 旋转180度后，左上角会变为原来的右下角
    Vector2Int newGridPos = CalculateRotatedGridPos(originalGridPos, placed.rotIndex, newRotIndex, mi.info.rows, mi.info.cols);
    
    // 更新内存中的旋转索引和网格位置
    placed.rotIndex = newRotIndex;
    placed.gridPos = newGridPos; // 如果有这个字段的话
    mi.rotIndex = newRotIndex;
    mi.gridPos = newGridPos;

    Debug.Log($"Rotated grid pos is {newGridPos}");
    
    // 计算目标旋转四元数
    Quaternion targetRotation = Quaternion.AngleAxis(placed.rotIndex * 90f, Vector3.up) * placed.baseRotation;
    
    // 执行 DOTween 旋转动画，时长1秒
    placed.instance.transform.DORotateQuaternion(targetRotation, 1f)
        .SetEase(Ease.InOutQuad); // 缓动曲线，使动画更自然
}


    
    /// <summary>
    /// 计算旋转后的网格位置
    /// </summary>
    /// <param name="originalPos">原始网格位置（左上角）</param>
    /// <param name="oldRot">原始旋转索引</param>
    /// <param name="newRot">新旋转索引</param>
    /// <param name="rows">行数（高度）</param>
    /// <param name="cols">列数（宽度）</param>
    /// <returns>旋转后的网格位置（新的左上角）</returns>
    private Vector2Int CalculateRotatedGridPos(Vector2Int originalPos, int oldRot, int newRot, int rows, int cols)
    {
        // 计算旋转的角度差（90度的倍数）
        int rotDiff = (newRot - oldRot + 4) % 4;
    
        // 如果旋转不是180度，返回原位置（或者需要单独处理90度旋转）
        if (rotDiff != 2) return originalPos;
    
        // 旋转180度：需要考虑原始朝向
        // 旋转索引：0=右，1=下，2=左，3=上
        switch (oldRot)
        {
            case 0: // 原本朝右，旋转180度后朝左
                // 右转左：新的左上角在原位置的左上方
                return new Vector2Int(originalPos.x - rows, originalPos.y - cols);
            
            case 1: // 原本朝下，旋转180度后朝上
                // 下转上：新的左上角在原位置的左上方
                return new Vector2Int(originalPos.x - cols, originalPos.y - rows);
            
            case 2: // 原本朝左，旋转180度后朝右
                // 左转右：新的左上角在原位置的右下方
                return new Vector2Int(originalPos.x + rows, originalPos.y + cols);
            
           // 原本朝上，旋转180度后朝下
           // 上转下：新的左上角在原位置的右下方
            default:
                return new Vector2Int(originalPos.x + cols, originalPos.y + rows);
        }
    }
    
    /// <summary>自动将地图缩放并居中到屏幕（根据当前物品分布）</summary>
    /// <summary>自动将地图缩放并居中到屏幕（保证完整显示地图网格）</summary>
    /// <param name="targetScreenUV">目标屏幕位置，视口坐标 (0,0) 左下角到 (1,1) 右上角。默认 null 表示屏幕中心 (0.5,0.5)。</param>
    /// <summary>
    /// 自动将地图缩放并居中到屏幕
    /// </summary>
    /// <param name="targetScreenUV">目标屏幕位置，视口坐标 (0,0) 左下角到 (1,1) 右上角。默认 null 表示屏幕中心 (0.5,0.5)。</param>
    /// <param name="padding">缩放边距（0~1），数值越小地图越小，1 表示刚好填满屏幕无留白，默认 0.9。</param>
      /// <summary>自动将地图缩放并居中到屏幕（根据当前物品分布）</summary>
    public void FitMapToScreen(Vector2? targetScreenUV = null)
    {
        if (!cam.orthographic) return;

        if (items.Count == 0)
        {
            Debug.Log("没有物品，不进行缩放适配。");
            return;
        }

        // 计算所有物品占用的最小/最大行列
        int minRow = int.MaxValue, maxRow = int.MinValue, minCol = int.MaxValue, maxCol = int.MinValue;
        foreach (var kv in items)
        {
            var placed = kv.Value;
            var dims = FootprintDims(placed.info, placed.rotIndex);
            var anchor = StartFromPivot(placed.gridPos, placed.info, placed.rotIndex);
            for (int r = 0; r < dims.x; r++)
                for (int c = 0; c < dims.y; c++)
                {
                    int row = anchor.x + r, col = anchor.y + c;
                    if (row < minRow) minRow = row;
                    if (row > maxRow) maxRow = row;
                    if (col < minCol) minCol = col;
                    if (col > maxCol) maxCol = col;
                }
        }

        float occupiedWidth = (maxCol - minCol + 1) * cellSize;
        float occupiedHeight = (maxRow - minRow + 1) * cellSize;
        float screenWorldHeight = 2f * cam.orthographicSize;
        float screenWorldWidth = screenWorldHeight * cam.aspect;
        float scaleHeight = screenWorldHeight / occupiedHeight;
        float scaleWidth = screenWorldWidth / occupiedWidth;
        float scale = Mathf.Min(scaleHeight, scaleWidth);

        // 根据网格尺寸选择预设缩放系数
        int mscaleid = Array.FindIndex(AvailableGridSizes, size => size == rows);
        float mapScale = mscaleid >= 0 ? MapScales[mscaleid] : MapScales[MapScales.Length - 1];
        scale = Mathf.Min(mapScale, 1.3f);
        transform.localScale = Vector3.one * scale;

        // 计算中心点
        Vector2Int occupiedAnchor = new Vector2Int(minRow, minCol);
        Vector2Int occupiedDims = new Vector2Int(maxRow - minRow + 1, maxCol - minCol + 1);
        Vector3 occupiedCenter = FootprintWorldCenter(occupiedAnchor, occupiedDims);

        Vector2 screenUV = targetScreenUV ?? new Vector2(0.5f, 0.5f);
        Ray camRay = cam.ViewportPointToRay(screenUV);
        Plane groundPlane = new Plane(Vector3.up, occupiedCenter);
        if (groundPlane.Raycast(camRay, out float enter))
        {
            Vector3 targetWorldPoint = camRay.GetPoint(enter);
            transform.position += targetWorldPoint - occupiedCenter;
        }
    }

}