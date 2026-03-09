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
    
    // ==================== 预制体目录 ====================
    // 场景可用的预制体目录（支持在编辑器中维护）
    public List<PrefabInfo> prefabCatalog = new List<PrefabInfo>();
    
    /// <summary>
    /// 所有地图项被销毁时触发的事件
    /// </summary>
    public event System.Action OnAllItemsDestroyed;
    
    // ==================== 视觉配置 ====================
    // 网格与选中高亮的视觉配置
    public Color gridColor = Color.gray;
    public Color selectedColor = Color.red;
    public bool selectedUseComplementary = false;
    public float gridLineWidth = 1f;
    
    // ==================== 交互反馈配置 ====================
    // 交互反馈：旋转/放置失败音效与消息
    public AudioClip rotateFailClip;
    public string rotateFailMessage = "无法旋转";
    public float failMessageDuration = 1.5f;
    public string placeFailMessage = "无法放置";
    
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
        if (rotIndex == 0) return new Vector2Int(dims0.x, 1);
        if (rotIndex == 1) return new Vector2Int(dims0.y, dims0.x);
        if (rotIndex == 2) return new Vector2Int(0, dims0.y);
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
    /// 检查指定区域是否可用
    /// 忽略自身 id，其他任意占用都视为不可用
    /// </summary>
    bool AreaFree(Vector2Int start, Vector2Int dims, int ignoreId)
    {
        // Step 1：遍历占用矩形范围
        for (int r = 0; r < dims.x; r++)
        {
            for (int c = 0; c < dims.y; c++)
            {
                // Step 2：读取占用表，忽略自身 id，其余任意占用都视为不可用
                int id = occupancy[start.x + r, start.y + c];
                if (id != -1 && id != ignoreId) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 检查是否可以放置
    /// 综合检查边界和占用
    /// </summary>
    bool CanPlace(PrefabInfo info, Vector2Int start, int rotIndex, int ignoreId)
    {
        // Step 1：计算当前旋转下的占用尺寸
        var dims = FootprintDims(info, rotIndex);
        // Step 2：越界与占用校验
        if (!InBounds(start, dims)) return false;
        if (!AreaFree(start, dims, ignoreId)) return false;
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
    /// 将区域写入占用表
    /// 标记指定区域为给定的 id
    /// </summary>
    void MarkAreaFormRotate(Vector2Int start, Vector2Int dims, int id,int rotIndex)
    {
        Vector2Int vector2Int=Vector2Int.one; 
        
        if (rotIndex == 0) vector2Int = new Vector2Int(-1, 1);
        if (rotIndex == 1) vector2Int = new Vector2Int(-1, -1);
        if (rotIndex == 2) vector2Int = new Vector2Int(1, -1);
        if (rotIndex == 3) vector2Int = new Vector2Int(0, 0);
        
        // Step 1：遍历占用矩形范围并写入 id
        for (int r = 0; r < dims.x; r++)
        for (int c = 0; c < dims.y; c++)
            occupancy[start.x + r*vector2Int.x, start.y + c*vector2Int.y] = id;
    }

    // ==================== 坐标转换 ====================
    /// <summary>
    /// 将网格坐标转换为世界坐标
    /// 返回单个网格中心的世界位置
    /// </summary>
    Vector3 GridToWorld(Vector2Int grid)
    {
        // Step 1：计算本地坐标系下的中心位置
        float lx = (grid.y + 0.5f) * cellSize + origin.x;
        float lz = (grid.x + 0.5f) * cellSize + origin.z;
        var local = new Vector3(lx, origin.y, lz);
        // Step 2：转换到世界坐标
        return transform.TransformPoint(local);
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

    // ==================== 鼠标拾取 ====================
    /// <summary>
    /// 从鼠标射线拾取网格坐标
    /// 随 transform 移动/旋转
    /// </summary>
    bool TryGetMouseGrid(out Vector2Int grid)
    {
        grid = default;
        // Step 1：准备射线与动态平面（随 MapEditor 变换）
        if (cam == null) return false;
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        var dynPlane = new Plane(transform.up, transform.TransformPoint(origin));
        if (!dynPlane.Raycast(ray, out float enter)) return false;
        var point = ray.GetPoint(enter);
        // Step 2：转到本地坐标，按 cellSize 量化为行列
        var local = transform.InverseTransformPoint(point) - origin;
        int c = Mathf.FloorToInt(local.x / cellSize);
        int r = Mathf.FloorToInt(local.z / cellSize);
        grid = new Vector2Int(r, c);
        // Step 3：返回是否在网格范围内
        return r >= 0 && c >= 0 && r < rows && c < cols;
    }

    /// <summary>
    /// 获取占用者 ID
    /// 读取指定网格的占用状态
    /// </summary>
    int GetOccupant(Vector2Int cell)
    {
        // Step 1：读取占用表中该格的 id（-1 表示空）
        return occupancy[cell.x, cell.y];
    }

    // ==================== 实例放置和管理 ====================
    /// <summary>
    /// 放置实例
    /// 以锚点为基准，校验边界与占用并实例化
    /// </summary>
    PlacedItem Place(PrefabInfo info, Vector2Int start, int rotIndex)
    {
        // Step 1：计算当前旋转下的占用尺寸与锚点/起点
        var dims = FootprintDims(info, rotIndex);
        var pivotGrid = start;
        var anchor = StartFromPivot(pivotGrid, info, rotIndex);
        // Step 2：越界与空间校验，失败时提示消息/音效
        if (!InBounds(anchor, dims)) { if (rotateFailClip != null) AudioSource.PlayClipAtPoint(rotateFailClip, transform.position); runtimeMessage = placeFailMessage + "：越界"; runtimeMessageUntil = Time.time + failMessageDuration; return null; }
        if (!AreaFree(anchor, dims, -1)) { if (rotateFailClip != null) AudioSource.PlayClipAtPoint(rotateFailClip, transform.position); runtimeMessage = placeFailMessage + "：空间不足"; runtimeMessageUntil = Time.time + failMessageDuration; return null; }
        // Step 3：分配 id 并实例化 GameObject 到地图节点下
        int id = nextId++;
        var go = Instantiate(info.prefab);
        go.transform.SetParent(transform);
        // Step 4：定位到占用中心并叠加旋转（保持预制体原始朝向）
        go.transform.position = FootprintWorldCenter(anchor, dims);
        var baseRot = go.transform.rotation;
        go.transform.rotation = Quaternion.AngleAxis(rotIndex * 90f, Vector3.up) * baseRot;
        // Step 5：记录 PlacedItem 并写入占用表
        var item = new PlacedItem { id = id, info = info, gridPos = pivotGrid, rotIndex = rotIndex, instance = go };
        item.baseRotation = baseRot;
        items[id] = item;
        MarkArea(anchor, dims, id);
        return item;
    }

    /// <summary>
    /// 按单格移动选中实例
    /// 含占用表更新
    /// </summary>
    bool TryMoveOneCell(PlacedItem item, Vector2Int delta)
    {
        // Step 1：释放当前占用区域
        var dims = FootprintDims(item.info, item.rotIndex);
        var curAnchor = StartFromPivot(item.gridPos, item.info, item.rotIndex);
        MarkArea(curAnchor, dims, -1);
        // Step 2：计算目标 pivot/anchor 并校验越界/占用
        var targetPivot = new Vector2Int(item.gridPos.x + delta.x, item.gridPos.y + delta.y);
        var targetAnchor = StartFromPivot(targetPivot, item.info, item.rotIndex);
        if (!InBounds(targetAnchor, dims) || !AreaFree(targetAnchor, dims, -1))
        {
            // Step 3：失败则恢复占用并返回 false
            MarkArea(curAnchor, dims, item.id);
            return false;
        }
        // Step 4：更新位置到占用中心并写入占用表
        item.gridPos = targetPivot;
        item.instance.transform.position = FootprintWorldCenter(targetAnchor, dims);
        MarkArea(targetAnchor, dims, item.id);
        return true;
    }

    /// <summary>
    /// 顺时针旋转 90°
    /// 沿世界空间 Y 轴，并更新占用与居中位置
    /// </summary>
    bool TryRotate90(PlacedItem item)
    {
        // Step 1：释放当前占用区域
        var dims = FootprintDims(item.info, item.rotIndex);
        var curAnchor = StartFromPivot(item.gridPos, item.info, item.rotIndex);
        MarkArea(curAnchor, dims, -1);
        // Step 2：计算下一旋转索引与目标占用范围
        int nextRot = (item.rotIndex + 1) % 4;
        var nextDims = FootprintDims(item.info, nextRot);
        var nextAnchor = StartFromPivot(item.gridPos, item.info, nextRot);
        // Step 3：越界/空间不足则提示并恢复占用
        if (!InBounds(nextAnchor, nextDims) || !AreaFree(nextAnchor, nextDims, -1))
        {
            if (rotateFailClip != null) AudioSource.PlayClipAtPoint(rotateFailClip, transform.position);
            runtimeMessage = rotateFailMessage;
            runtimeMessageUntil = Time.time + failMessageDuration;
            MarkArea(curAnchor, dims, item.id);
            return false;
        }
        // Step 4：应用旋转、重新居中并写入占用表
        item.rotIndex = nextRot;
        item.instance.transform.rotation = Quaternion.AngleAxis(item.rotIndex * 90f, Vector3.up) * item.baseRotation;
        item.instance.transform.position = FootprintWorldCenter(nextAnchor, nextDims);
        MarkArea(nextAnchor, nextDims, item.id);
        return true;
    }

    // ==================== 运行时交互 ====================
    /// <summary>
    /// 运行时编辑开关
    /// 启用后支持放置、选择、拖拽、旋转和删除
    /// </summary>
    public bool runtimeEditingEnabled = false;
    
    /// <summary>
    /// 简易交互更新
    /// 放置、选择、拖拽、旋转与 Alt+Delete 删除
    /// </summary>
    void Update()
    {
        // Step 1：运行时编辑开关（避免误触）
        if (!runtimeEditingEnabled) return;
        // Step 2：Alpha1 放置（示范交互），含越界与占用提示
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (prefabCatalog.Count > 0 && TryGetMouseGrid(out var cell))
            {
                var info = prefabCatalog[0];
                var dims = FootprintDims(info, 0);
                var anchor = StartFromPivot(cell, info, 0);
                if (!InBounds(anchor, dims))
                {
                    runtimeMessage = placeFailMessage + "：越界";
                    runtimeMessageUntil = Time.time + failMessageDuration;
                }
                else if (!AreaFree(anchor, dims, -1))
                {
                    runtimeMessage = placeFailMessage + "：目标区域已被占用";
                    runtimeMessageUntil = Time.time + failMessageDuration;
                }
                else
                {
                    var item = Place(info, cell, 0);
                    if (item != null) selected = item;
                }
            }
        }

        // Step 3：左键点击选择，同一元素再次点击触发旋转
        if (Input.GetMouseButtonDown(0))
        {
            if (TryGetMouseGrid(out var cell))
            {
                int id = GetOccupant(cell);
                if (id != -1)
                {
                    var item = items[id];
                    if (selected == item)
                    {
                        TryRotate90(selected);
                    }
                    else
                    {
                        selected = item;
                    }
                }
            }
        }

        // Step 4：左键拖拽按主方向单格移动
        if (Input.GetMouseButton(0) && selected != null)
        {
            if (TryGetMouseGrid(out var cell))
            {
                var diff = new Vector2Int(cell.x - selected.gridPos.x, cell.y - selected.gridPos.y);
                Vector2Int step = Vector2Int.zero;
                if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
                    step = new Vector2Int(diff.x > 0 ? 1 : -1, 0);
                else if (Mathf.Abs(diff.y) > 0)
                    step = new Vector2Int(0, diff.y > 0 ? 1 : -1);
                if (step != Vector2Int.zero)
                    TryMoveOneCell(selected, step);
            }
        }

        // Step 5：方向键移动与 Alt+Delete 删除选中
        if (selected != null)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) TryMoveOneCell(selected, new Vector2Int(-1, 0));
            if (Input.GetKeyDown(KeyCode.DownArrow)) TryMoveOneCell(selected, new Vector2Int(1, 0));
            if (Input.GetKeyDown(KeyCode.LeftArrow)) TryMoveOneCell(selected, new Vector2Int(0, -1));
            if (Input.GetKeyDown(KeyCode.RightArrow)) TryMoveOneCell(selected, new Vector2Int(0, 1));
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.Delete))
            {
                // Step 5.1：释放占用并销毁实例
                var dims = FootprintDims(selected.info, selected.rotIndex);
                var anchor = StartFromPivot(selected.gridPos, selected.info, selected.rotIndex);
                MarkArea(anchor, dims, -1);
                items.Remove(selected.id);
#if UNITY_EDITOR
                UnityEditor.Selection.activeObject = null;
#endif
                Destroy(selected.instance);
                selected = null;
            }
        }
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

    // ==================== UI 消息显示 ====================
    /// <summary>
    /// 简易屏幕消息显示
    /// 失败反馈提示
    /// </summary>
    void OnGUI()
    {
        // Step 1：在有效时间内显示失败提示框
        if (Time.time < runtimeMessageUntil && !string.IsNullOrEmpty(runtimeMessage))
        {
            var rect = new Rect(10, 10, 480, 26);
            GUI.Box(rect, runtimeMessage);
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
#if UNITY_EDITOR
        // Step 2：编辑器下清空现有子对象并记录 Undo
        if (clearExisting)
        {
            UnityEditor.Selection.activeObject = null;
            var toDelete = new List<GameObject>();
            for (int i = 0; i < transform.childCount; i++) toDelete.Add(transform.GetChild(i).gameObject);
            foreach (var go in toDelete) UnityEditor.Undo.DestroyObjectImmediate(go);
        }
#else
        // Step 2：运行时下清空现有子对象
        if (clearExisting)
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        }
#endif
        // Step 3：遍历数据资产并实例化、定位与写入占用
        foreach (var it in data.items)
        {
            if (it.info == null || it.info.prefab == null) continue;
            var dims = FootprintDims(it.info, it.rotIndex);
            var anchor = StartFromPivot(it.gridPos, it.info, it.rotIndex);
            if (!InBounds(anchor, dims)) continue;
#if UNITY_EDITOR
            var obj = UnityEditor.PrefabUtility.InstantiatePrefab(it.info.prefab, transform) as GameObject;
            UnityEditor.Undo.RegisterCreatedObjectUndo(obj, "Load Map Item");
#else
            var obj = Instantiate(it.info.prefab, transform);
#endif
            var baseRot = obj.transform.rotation;
            obj.transform.rotation = Quaternion.AngleAxis(it.rotIndex * 90f, Vector3.up) * baseRot;
            obj.transform.position = FootprintWorldCenter(anchor, dims);
            var mi = obj.GetComponent<MapItem>();
            if (mi == null) mi = obj.AddComponent<MapItem>();
            mi.info = it.info;
            mi.gridPos = it.gridPos;
            mi.rotIndex = it.rotIndex;
            mi.baseRotation = baseRot;
            var id = nextId++;
            items[id] = new PlacedItem { id = id, info = it.info, gridPos = it.gridPos, rotIndex = it.rotIndex, instance = obj, baseRotation = baseRot };
            MarkAreaFormRotate(it.gridPos, dims, id,mi.rotIndex);
        }
    }

    /// <summary>
    /// 清空所有地图项
    /// 重置占用表和实例字典
    /// </summary>
    public void ClearAllItems()
    {
        ResetOccupancy();
        items.Clear();
        nextId = 1;
        selected = null;
#if UNITY_EDITOR
        var toDelete = new List<GameObject>();
        UnityEditor.Selection.activeObject = null;
        for (int i = 0; i < transform.childCount; i++) toDelete.Add(transform.GetChild(i).gameObject);
        foreach (var go in toDelete) UnityEditor.Undo.DestroyObjectImmediate(go);
#else
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
#endif
        UpdateDataAssetMirror();
    }

#if UNITY_EDITOR
    // ==================== 编辑器专用：保存到资产 ====================
    /// <summary>
    /// 保存地图到数据资产
    /// 将当前场景中的地图项序列化到 ScriptableObject 资产
    /// </summary>
    public void SaveToAsset(MapData data)
    {
        // Step 1：通过 SerializedObject 写入标量字段
        var so = new UnityEditor.SerializedObject(dataAsset);
        so.FindProperty("rows").intValue = rows;
        so.FindProperty("cols").intValue = cols;
        so.FindProperty("cellSize").floatValue = cellSize;
        so.FindProperty("origin").vector3Value = origin;
        // Step 2：清空并重建 items 数组
        var itemsProp = so.FindProperty("items");
        itemsProp.ClearArray();
        int idx = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            var mi = child.GetComponent<MapItem>();
            if (mi == null || mi.info == null) continue;
            itemsProp.InsertArrayElementAtIndex(idx);
            var elem = itemsProp.GetArrayElementAtIndex(idx);
            elem.FindPropertyRelative("info").objectReferenceValue = mi.info;
            elem.FindPropertyRelative("gridPos").vector2IntValue = mi.gridPos;
            elem.FindPropertyRelative("rotIndex").intValue = mi.rotIndex;
            idx++;
        }
        // Step 3：应用改动、标记脏并保存刷新
        so.ApplyModifiedProperties();
        UnityEditor.EditorUtility.SetDirty(data);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
    }
#endif

    // ==================== 项 ID 解析 ====================
    /// <summary>
    /// 解析地图项的 ID
    /// 从占用表或实例字典中查找，未找到则分配新 ID
    /// </summary>
    int ResolveItemId(MapItem item)
    {
        // Step 1：尝试通过当前 anchor 在占用表中读取 id
        var dims = FootprintDims(item.info, item.rotIndex);
        var anchor = StartFromPivot(item.gridPos, item.info, item.rotIndex);
        int id = occupancy[anchor.x, anchor.y];
        if (id != -1) return id;
        // Step 2：遍历记录表，匹配 GameObject 引用以获取 id
        foreach (var kv in items)
        {
            if (kv.Value.instance == item.gameObject) return kv.Key;
        }
        // Step 3：若未记录，则分配新 id 并写入占用表
        int nid = nextId++;
        items[nid] = new PlacedItem { id = nid, info = item.info, gridPos = item.gridPos, rotIndex = item.rotIndex, instance = item.gameObject, baseRotation = item.baseRotation };
        MarkArea(anchor, dims, nid);
        return nid;
    }

    // ==================== 公共 API ====================
    /// <summary>
    /// 尝试移动地图项一格
    /// 供外部组件调用，用于移动地图项
    /// </summary>
    public bool TryMoveItemOneCell(MapItem item, Vector2Int delta)
    {
        // Step 1：解析/确保该项的 id
        int id = ResolveItemId(item);
        // Step 2：释放当前占用区域
        var dims = FootprintDims(item.info, item.rotIndex);
        var curAnchor = StartFromPivot(item.gridPos, item.info, item.rotIndex);
        MarkArea(curAnchor, dims, -1);
        // Step 3：计算目标占用并校验
        var targetPivot = new Vector2Int(item.gridPos.x + delta.x, item.gridPos.y + delta.y);
        var targetAnchor = StartFromPivot(targetPivot, item.info, item.rotIndex);
        if (!InBounds(targetAnchor, dims) || !AreaFree(targetAnchor, dims, -1))
        {
            MarkArea(curAnchor, dims, id);
            return false;
        }
        // Step 4：应用位置并写入占用与记录表
        item.gridPos = targetPivot;
        item.transform.position = FootprintWorldCenter(targetAnchor, dims);
        MarkArea(targetAnchor, dims, id);
        if (items.TryGetValue(id, out var rec)) rec.gridPos = item.gridPos;
        return true;
    }

    /// <summary>
    /// 检查区域是否空闲
    /// 供外部组件调用，用于检查目标位置是否可放置
    /// </summary>
    public bool IsAreaFreeFor(MapItem item, Vector2Int targetPivot, int rotIndex, out bool outOfBounds)
    {
        // Step 1：计算目标占用范围
        var dims = FootprintDims(item.info, rotIndex);
        var targetAnchor = StartFromPivot(targetPivot, item.info, rotIndex);
        // Step 2：输出越界标志并在越界时短路
        outOfBounds = !InBounds(targetAnchor, dims);
        if (outOfBounds) return false;
        // Step 3：忽略自身占用，返回区域是否空闲
        int ignoreId = ResolveItemId(item);
        return AreaFree(targetAnchor, dims, ignoreId);
    }

    /// <summary>
    /// 移除地图项
    /// 释放占用并销毁实例
    /// </summary>
    public bool RemoveItem(MapItem item)
    {
        // Step 1：解析 id 并释放占用
        int id = ResolveItemId(item);
        var dims = FootprintDims(item.info, item.rotIndex);
        var anchor = StartFromPivot(item.gridPos, item.info, item.rotIndex);
        MarkArea(anchor, dims, -1);

        // Step 2：移除记录并销毁对象
        items.Remove(id);

#if UNITY_EDITOR
        UnityEditor.Selection.activeObject = null;
#endif
        Destroy(item.gameObject);

        // Step 3：检查是否所有物品都已销毁
        if (items.Count == 0)
        {
            OnAllItemsDestroyed?.Invoke();
        }

        return true;
    }

    /// <summary>
    /// 更新数据资产镜像
    /// 将当前地图状态同步到数据资产
    /// </summary>
    public void UpdateDataAssetMirror()
    {
        // Step 1：若无数据资产则返回
        if (dataAsset == null) return;
        // Step 2：运行态仅更新内存副本（不改动资产）
        if (Application.isPlaying)
        {
            if (runtimeData == null) return;
            runtimeData.rows = rows;
            runtimeData.cols = cols;
            runtimeData.cellSize = cellSize;
            runtimeData.origin = origin;
            runtimeData.items.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                var mi = transform.GetChild(i).GetComponent<MapItem>();
                if (mi == null || mi.info == null) continue;
                var d = new MapData.MapItemData { info = mi.info, gridPos = mi.gridPos, rotIndex = mi.rotIndex };
                runtimeData.items.Add(d);
            }
            return;
        }
#if UNITY_EDITOR
        // Step 3：非运行态（编辑器）写入资产并保存刷新
        var so = new UnityEditor.SerializedObject(dataAsset);
        so.FindProperty("rows").intValue = rows;
        so.FindProperty("cols").intValue = cols;
        so.FindProperty("cellSize").floatValue = cellSize;
        so.FindProperty("origin").vector3Value = origin;
        var itemsProp = so.FindProperty("items");
        itemsProp.ClearArray();
        int idx = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            var mi = transform.GetChild(i).GetComponent<MapItem>();
            if (mi == null || mi.info == null) continue;
            itemsProp.InsertArrayElementAtIndex(idx);
            var elem = itemsProp.GetArrayElementAtIndex(idx);
            elem.FindPropertyRelative("info").objectReferenceValue = mi.info;
            elem.FindPropertyRelative("gridPos").vector2IntValue = mi.gridPos;
            elem.FindPropertyRelative("rotIndex").intValue = mi.rotIndex;
            idx++;
        }
        so.ApplyModifiedProperties();
        UnityEditor.EditorUtility.SetDirty(dataAsset);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    /// <summary>
    /// 获取地图项占用区的中心位置
    /// 供外部组件调用，用于计算目标位置
    /// </summary>
    public Vector3 GetFootprintCenter(MapItem item, Vector2Int pivotGrid)
    {
        // Step 1：计算目标占用尺寸与起点
        var dims = FootprintDims(item.info, item.rotIndex);
        var anchor = StartFromPivot(pivotGrid, item.info, item.rotIndex);
        // Step 2：返回占用中心世界坐标
        return FootprintWorldCenter(anchor, dims);
    }
    
    // 获取指定网格的占用者ID（-1表示空闲）
    public int GetOccupantIdAtCell(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= rows || cell.y < 0 || cell.y >= cols)
            return -1;
        return occupancy[cell.x, cell.y];
    }

// 获取网格中心的世界坐标
    public Vector3 GetCellWorldPosition(Vector2Int grid)
    {
        // 直接调用已有的私有方法 GridToWorld（将其改为public，或复制逻辑）
        float lx = (grid.y + 0.5f) * cellSize + origin.x;
        float lz = (grid.x + 0.5f) * cellSize + origin.z;
        var local = new Vector3(lx, origin.y, lz);
        return transform.TransformPoint(local);
    }
    
    public int GetIdByItem(MapItem item)
    {
        foreach (var kv in items)
        {
            if (kv.Value.instance == item.gameObject)
                return kv.Key;
        }
        // 若未找到，尝试通过占用表查找
        var dims = FootprintDims(item.info, item.rotIndex);
        var anchor = StartFromPivot(item.gridPos, item.info, item.rotIndex);
        int id = occupancy[anchor.x, anchor.y];
        if (id != -1) return id;
        return -1; // 未找到
    }
}

