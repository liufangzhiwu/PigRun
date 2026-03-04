using System.Collections.Generic;
using UnityEngine;

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
    // 地图网格行列与单元大小、原点（本地坐标）
    public int rows = 10;
    public int cols = 10;
    public float cellSize = 0.21f;
    public Vector3 origin;
    
    // ==================== 视觉配置 ====================
    // 网格与选中高亮的视觉配置
    public Color gridColor = Color.gray;
    public Color selectedColor = Color.red;
    public bool selectedUseComplementary = false;

    // ==================== 数据资产 ====================
    public MapData dataAsset;
    MapData runtimeData;

    // ==================== 运行时内部数据结构 ====================
    /// <summary>
    /// 已放置实例的内部记录
    /// 运行时记录每个已放置实例的状态，用于占用管理和交互
    /// </summary>
    class PlacedItem
    {
        public int id;                      // 唯一标识符
        public PrefabInfo info;             // 预制体配置信息
        public Vector2Int gridPos;          // 锚点网格坐标
        public int rotIndex;                // 旋转索引
        public GameObject instance;         // 实例化的游戏对象
        public Quaternion baseRotation;     // 基础旋转
    }

    // ==================== 占用表和实例管理 ====================
    int[,] occupancy;                                           // 网格占用表，记录每个格子被哪个 id 占用
    Dictionary<int, PlacedItem> items = new Dictionary<int, PlacedItem>();  // 已放置实例字典
    int nextId = 1;                                             // 下一个可用 ID
    PlacedItem selected;                                        // 当前选中的实例
    
    // ==================== 拾取相关 ====================
    Plane plane;                                                // 拾取平面
    Camera cam;                                                 // 主相机引用
    
    // ==================== UI 消息 ====================
    string runtimeMessage;                                      // 运行时消息文本
    float runtimeMessageUntil;                                  // 消息显示截止时间

    // ==================== 初始化方法 ====================
    /// <summary>
    /// 初始化地图系统
    /// 设置单例、初始化占用表、拾取平面和加载数据资产
    /// </summary>
    void Awake()
    {
        // Step 1：设置单例引用，供运行时脚本（如 PigRunner）自动获取
        Instance = this;
        Physics.queriesHitTriggers = true;
        // Step 2：初始化占用表，将所有格标记为空（-1）
        occupancy = new int[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                occupancy[r, c] = -1;
        // Step 3：初始化拾取用平面与主相机引用
        plane = new Plane(transform.up, transform.TransformPoint(origin));
        cam = Camera.main;
        // Step 4：若绑定了数据资产，在唤醒时加载地图
        if (dataAsset != null)
        {
            if (Application.isPlaying)
            {
                runtimeData = ScriptableObject.CreateInstance<MapData>();
                runtimeData.rows = dataAsset.rows;
                runtimeData.cols = dataAsset.cols;
                //runtimeData.cellSize = dataAsset.cellSize;
                runtimeData.origin = dataAsset.origin;
                runtimeData.items = new List<MapData.MapItemData>();
                foreach (var it in dataAsset.items)
                {
                    var d = new MapData.MapItemData { info = it.info, gridPos = it.gridPos, rotIndex = it.rotIndex };
                    runtimeData.items.Add(d);
                }
                LoadFromAsset(runtimeData, true);
            }
            else
            {
                LoadFromAsset(dataAsset, true);
            }
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ==================== 占用表管理 ====================
    /// <summary>
    /// 重置占用表
    /// 将所有网格标记为空（-1）
    /// </summary>
    void ResetOccupancy()
    {
        occupancy = new int[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                occupancy[r, c] = -1;
    }

    // ==================== 占用计算工具方法 ====================
    /// <summary>
    /// 根据旋转索引计算占用矩形尺寸
    /// 行列在 90°/270° 时互换
    /// </summary>
    Vector2Int FootprintDims(PrefabInfo info, int rotIndex)
    {
        // Step 1：容错，当 info 为空时按 1x1 处理
        if (info == null) return new Vector2Int(1, 1);
        // Step 2：偶数旋转索引保持原行列，奇数时行列互换
        if (rotIndex % 2 == 0) return new Vector2Int(info.rows, info.cols);
        return new Vector2Int(info.cols, info.rows);
    }

    /// <summary>
    /// 获取未旋转时的锚点行列
    /// -1 时取中点
    /// </summary>
    Vector2Int PivotRC(PrefabInfo info)
    {
        // Step 1：容错，当 info 为空时返回 (0,0)
        if (info == null) return new Vector2Int(0, 0);
        // Step 2：pivotRow/pivotCol 为 -1 时使用几何中心
        int pr = info.pivotRow >= 0 ? info.pivotRow : Mathf.FloorToInt((info.rows - 1) * 0.5f);
        int pc = info.pivotCol >= 0 ? info.pivotCol : Mathf.FloorToInt((info.cols - 1) * 0.5f);
        return new Vector2Int(pr, pc);
    }

    /// <summary>
    /// 顺时针旋转后，锚点在占用矩阵中的行列索引映射
    /// </summary>
    Vector2Int RotatedPivot(int rotIndex, Vector2Int dims0)
    {
        // Step 1：归一化旋转索引到 [0,3]
        rotIndex = ((rotIndex % 4) + 4) % 4;
        // Step 2：根据旋转索引计算 pivot 的映射位置
        if (rotIndex == 0) return new Vector2Int(dims0.x, 0);
        if (rotIndex == 1) return new Vector2Int(dims0.y, dims0.x);
        if (rotIndex == 2) return new Vector2Int(0 , dims0.y);
        if (rotIndex == 3) return new Vector2Int(0, 0);
        //return new Vector2Int(dims0.y - 1 - pivot.y, dims0.x - 1 - pivot.x);
        return new Vector2Int(dims0.y - 1, dims0.x - 1);
    }

    /// <summary>
    /// 给定锚点所在网格坐标，计算占用矩形左下角起点（anchor）
    /// </summary>
    Vector2Int StartFromPivot(Vector2Int pivotGrid, PrefabInfo info, int rotIndex)
    {
        // Step 1：获取原始占用尺寸
        var dims0 = info != null ? new Vector2Int(info.rows, info.cols) : new Vector2Int(1, 1);
        // Step 2：计算未旋转时的 pivot
        var piv0 = PivotRC(info);
        // Step 3：计算旋转后 pivot 在占用矩阵中的索引
        var pivR = RotatedPivot(rotIndex, dims0);
        // Step 4：pivotGrid 减去旋转 pivot 得到占用矩形的左下角（anchor）
        return new Vector2Int(pivotGrid.x - pivR.x, pivotGrid.y - pivR.y);
        //return new Vector2Int(pivotGrid.x, pivotGrid.y);
    }

    // ==================== 边界和占用检测 ====================
    /// <summary>
    /// 检查占用区域是否在边界内
    /// </summary>
    bool InBounds(Vector2Int start, Vector2Int dims)
    {
        // Step 1：检查起点非负
        if (start.x < 0 || start.y < 0) return false;
        // Step 2：检查终点不越界
        if (start.x + dims.x > rows) return false;
        if (start.y + dims.y > cols) return false;
        return true;
    }


    /// <summary>
    /// 将区域写入占用表
    /// 标记指定区域为给定的 id
    /// </summary>
    void MarkArea(Vector2Int start, Vector2Int dims, int id)
    {
        // Step 1：遍历占用矩形范围并写入 id
        for (int r = 0; r < dims.x; r++)
            for (int c = 0; c < dims.y; c++)
                occupancy[start.x + r, start.y + c] = id;
    }
   

    /// <summary>
    /// 计算占用矩形的几何中心世界坐标
    /// </summary>
    Vector3 FootprintWorldCenter(Vector2Int anchor, Vector2Int dims)
    {
        // Step 1：计算占用矩形中心的本地坐标
        float lx = (anchor.y + dims.y * 0.5f) * cellSize + origin.x;
        float lz = (anchor.x + dims.x * 0.5f) * cellSize + origin.z;
        var local = new Vector3(lx, origin.y, lz);
        // Step 2：转换到世界坐标
        return transform.TransformPoint(local);
    }
  
    // ==================== Gizmos 绘制 ====================
    /// <summary>
    /// 绘制网格与选中框
    /// 运行时显示网格线和当前选中实例的占用范围
    /// </summary>
    void OnDrawGizmos()
    {
        // Step 1：绘制行线
        Gizmos.color = gridColor;
        for (int r = 0; r <= rows; r++)
        {
            var a = transform.TransformPoint(origin + new Vector3(0f, 0f, r * cellSize));
            var b = transform.TransformPoint(origin + new Vector3(cols * cellSize, 0f, r * cellSize));
            Gizmos.DrawLine(a, b);
        }
        // Step 2：绘制列线
        for (int c = 0; c <= cols; c++)
        {
            var a = transform.TransformPoint(origin + new Vector3(c * cellSize, 0f, 0f));
            var b = transform.TransformPoint(origin + new Vector3(c * cellSize, 0f, rows * cellSize));
            Gizmos.DrawLine(a, b);
        }

        // Step 3：绘制选中占用框
        if (selected != null)
        {
            var dims = FootprintDims(selected.info, selected.rotIndex);
            var anchor = StartFromPivot(selected.gridPos, selected.info, selected.rotIndex);
            var selCol = selectedUseComplementary ? new Color(1f - gridColor.r, 1f - gridColor.g, 1f - gridColor.b, 1f) : selectedColor;
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
    }


    

    // ==================== 数据资产加载 ====================
    /// <summary>
    /// 从数据资产加载地图
    /// 同步参数并实例化所有地图项
    /// </summary>
    public void LoadFromAsset(MapData data, bool clearExisting = true)
    {
        // Step 1：同步基础参数并重置占用表
        rows = data.rows;
        cols = data.cols;
        //cellSize = data.cellSize;
        origin = data.origin;
        ResetOccupancy();
        // Step 2：运行时下清空现有子对象
        if (clearExisting)
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        }

        // Step 3：遍历数据资产并实例化、定位与写入占用
        foreach (var it in data.items)
        {
            if (it.info == null || it.info.prefab == null) continue;
            var dims = FootprintDims(it.info, it.rotIndex);
            var anchor = StartFromPivot(it.gridPos, it.info, it.rotIndex);
            if (!InBounds(anchor, dims)) continue;
            var obj = Instantiate(it.info.prefab, transform);
            var baseRot = obj.transform.rotation;
            obj.transform.rotation = Quaternion.AngleAxis((it.rotIndex) * 90f, Vector3.up) * baseRot;
            obj.transform.position = FootprintWorldCenter(anchor, dims);
            var mi = obj.GetComponent<MapItem>();
            if (mi == null) mi = obj.AddComponent<MapItem>();
            mi.info = it.info;
            mi.gridPos = it.gridPos;
            mi.rotIndex = it.rotIndex;
            mi.baseRotation = baseRot;
            var id = nextId++;
            items[id] = new PlacedItem { id = id, info = it.info, gridPos = it.gridPos, rotIndex = it.rotIndex, instance = obj, baseRotation = baseRot };
            MarkArea(anchor, dims, id);
        }
    }


}

