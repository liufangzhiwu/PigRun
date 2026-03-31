using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static Map;

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
    private float cellSize = 0.18f;
    public Vector3 origin;      


    /// <summary>
    /// 所有地图项被销毁时触发的事件
    /// </summary>
    public event System.Action OnAllItemsDestroyed;
    public event System.Action OnLoadNewMap;
    
    // ==================== 视觉配置 ====================
    // 网格与选中高亮的视觉配置
    public Color gridColor = Color.gray;
    public Color selectedColor = Color.red;
    public bool selectedUseComplementary = false;
    public float gridLineWidth = 1f;
    public Sprite grid01 = null;
    public Sprite grid02 = null;
    
    [Tooltip("运行时在地图网格上显示每格占用 id，便于检查占用表是否正确")]
    public bool showOccupancyTable = true;
   
    public bool LevelFinish = false;
    public MedicineCowItem medicineCowItem;
    public SickDonkeyItem sickDonkeyItem;
    
    // 添加事件委托
    public delegate void AnimalEscapedDelegate(AnimalBase animal);
    public event AnimalEscapedDelegate OnAnimalEscaped;
    
    // ==================== 数据资产 ====================
    public MapData dataAsset;
    //MapData runtimeData;

    // ==================== 运行时内部数据结构 ====================
    /// <summary>
    /// 已放置实例的内部记录
    /// 运行时记录每个已放置实例的状态，用于占用管理和交互
    /// </summary>
    public class PlacedItem
    {
        public int id;                      // 唯一标识符
        public PrefabInfo info;             // 预制体配置信息
        public Vector2Int gridPos;          // 锚点网格坐标
        public int rotIndex;                // 旋转索引
        public GameObject instance;         // 实例化的游戏对象
        public Quaternion baseRotation;     // 基础旋转
    }

    // ==================== 占用表和实例管理 ====================
    public int[,] occupancy;                                           // 网格占用表，记录每个格子被哪个 id 占用
    Dictionary<int, PlacedItem> items = new Dictionary<int, PlacedItem>();  // 已放置实例字典
    int nextId = 1;                                             // 下一个可用 ID
    PlacedItem selected;                                        // 当前选中的实例
    
    // ==================== 拾取相关 ====================
    Plane plane;                                                // 拾取平面
    Camera cam;                                                 // 主相机引用

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
        // // Step 4：若绑定了数据资产，在唤醒时加载地图
        // if (dataAsset != null)
        // {
        //     if (Application.isPlaying)
        //     {
        //         runtimeData = ScriptableObject.CreateInstance<MapData>();
        //         runtimeData.rows = dataAsset.rows;
        //         runtimeData.cols = dataAsset.cols;
        //         //runtimeData.cellSize = dataAsset.cellSize;
        //         runtimeData.origin = dataAsset.origin;
        //         runtimeData.items = new List<MapData.MapItemData>();
        //         foreach (var it in dataAsset.items)
        //         {
        //             var d = new MapData.MapItemData { info = it.info, gridPos = it.gridPos, rotIndex = it.rotIndex };
        //             runtimeData.items.Add(d);
        //         }
        //         LoadFromAsset(runtimeData, true);
        //     }
        //     else
        //     {
        //         LoadFromAsset(dataAsset, true);
        //     }
        // }
        grid01=Resources.Load<Sprite>("UI/grid01");
        grid02=Resources.Load<Sprite>("UI/grid02");
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
    
    public void OnLoadNewMapEvent()
    {
        Map.Instance.OnLoadNewMap?.Invoke();
    }

    // ==================== 占用计算工具方法 ====================
    /// <summary>
    /// 根据旋转索引计算占用矩形尺寸
    /// 行列在 90°/270° 时互换
    /// </summary>
    public Vector2Int FootprintDims(PrefabInfo info, int rotIndex)
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
        if (rotIndex == 2) return new Vector2Int(1, dims0.y);
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

        if (rotIndex==-1)
        {
            vector2Int = new Vector2Int(1, 1);
            // Step 1：遍历占用矩形范围并写入 id
            for (int r = 0; r < dims.x; r++)
            for (int c = 0; c < dims.y; c++)
                occupancy[start.x + r*vector2Int.x, start.y + c*vector2Int.y] = id;
        }
        if (rotIndex == 1) //向下
        {
            vector2Int = new Vector2Int(-1, -1);
            
            // Step 1：遍历占用矩形范围并写入 id
            for (int r = 0; r < dims.x; r++)
            for (int c = 0; c < dims.y; c++)
                occupancy[start.x + r*vector2Int.x-1, start.y + c*vector2Int.y-1] = id;
        }
        if (rotIndex == 2) //向左
        {
            vector2Int = new Vector2Int(1, -1);
            // Step 1：遍历占用矩形范围并写入 id
            for (int r = 0; r < dims.x; r++)
            for (int c = 1; c < dims.y+1; c++)
                occupancy[start.x + r*vector2Int.x-1, start.y + c*vector2Int.y] = id;
        }

        if (rotIndex == 0) //向右
        {
            vector2Int = new Vector2Int(-1, 1);
            // Step 1：遍历占用矩形范围并写入 id
            for (int r = 1; r < dims.x+1; r++)
            for (int c = 0; c < dims.y; c++)
                occupancy[start.x + r*vector2Int.x, start.y + c*vector2Int.y-1] = id;
        }
    }
    
    /// <summary>
    /// 将itemid所在的格子重置为-1
    /// </summary>
    void ResetMarkAreaFormRotate(Vector2Int start, Vector2Int dims, int itemid,int rotIndex)
    {
        Vector2Int vector2Int=Vector2Int.one;

        int id = -1;

        if (rotIndex==-1)
        {
            vector2Int = new Vector2Int(1, 1);
            // Step 1：遍历占用矩形范围并写入 id
            for (int r = 0; r < dims.x; r++)
            for (int c = 0; c < dims.y; c++)
                occupancy[start.x + r*vector2Int.x, start.y + c*vector2Int.y] = id;
        }
        if (rotIndex == 1) //向下
        {
            vector2Int = new Vector2Int(-1, -1);
            
            // Step 1：遍历占用矩形范围并写入 id
            for (int r = 0; r < dims.x; r++)
            for (int c = 0; c < dims.y; c++)
                occupancy[start.x + r*vector2Int.x-1, start.y + c*vector2Int.y-1] = id;
        }
        if (rotIndex == 2) //向左
        {
            vector2Int = new Vector2Int(1, -1);
            // Step 1：遍历占用矩形范围并写入 id
            for (int r = 0; r < dims.x; r++)
            for (int c = 1; c < dims.y+1; c++)
                occupancy[start.x + r*vector2Int.x-1, start.y + c*vector2Int.y] = id;
        }

        if (rotIndex == 0) //向右
        {
            vector2Int = new Vector2Int(-1, 1);
            // Step 1：遍历占用矩形范围并写入 id
            for (int r = 1; r < dims.x+1; r++)
            for (int c = 0; c < dims.y; c++)
                occupancy[start.x + r*vector2Int.x, start.y + c*vector2Int.y-1] = id;
        }
        
        // for (int r = 0; r < rows; r++)
        // for (int c = 0; c < cols; c++)
        //     if (occupancy[r, c] == itemid)
        //     {
        //         occupancy[r, c] = -1;
        //     }
            
    }

    // ==================== 坐标转换 ====================
    /// <summary>
    /// 将网格坐标转换为世界坐标
    /// 返回单个网格中心的世界位置
    /// </summary>
    public Vector3 GridToWorld(Vector2Int grid)
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
    public Vector3 FootprintWorldCenter(Vector2Int anchor, Vector2Int dims)
    {
        // Step 1：计算占用矩形中心的本地坐标
        float lx = (anchor.y + dims.y * 0.5f) * cellSize + origin.x;
        float lz = (anchor.x + dims.x * 0.5f) * cellSize + origin.z;
        var local = new Vector3(lx, origin.y, lz);
        // Step 2：转换到世界坐标
        return transform.TransformPoint(local);
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

#if UNITY_EDITOR
        // Step 4：运行时在地图网格上直接显示占用 id（每格中心，-1 显示为 ·）
        if (Application.isPlaying && showOccupancyTable && occupancy != null)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var worldPos = GridToWorld(new Vector2Int(r, c));
                    int id = occupancy[r, c];
                    string text = id >= 0 ? id.ToString() : "·";
                    UnityEditor.Handles.Label(worldPos + Vector3.up * 0.02f, text);
                }
            }
        }
#endif
    }


   // 在 Map 类中添加静态数组，用于存储可用的网格尺寸
    private static readonly int[] AvailableGridSizes = new int[] { 20, 23, 25,30 };
    private static readonly float[] mapScales = new float[] { 1.3f, 0.9f, 0.9f,0.9f };

    /// <summary>
    /// 获取与目标尺寸最接近的可用网格尺寸
    /// </summary>
    private int GetClosestGridSize(int target)
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
            else if (diff == minDiff)
            {
                // 如果距离相同，选择较大的尺寸（更保守）
                if (AvailableGridSizes[i] > closest)
                    closest = AvailableGridSizes[i];
            }
        }
        return closest;
    }

    /// <summary>
    /// 从数据资产加载地图
    /// 同步参数并实例化所有地图项（仅加载在选定网格范围内的物品）
    /// </summary>
    public void LoadFromAsset(MapData data, bool clearExisting = true)
    {
        // Step 1：根据数据中的宽度选择最接近的固定网格尺寸
        int targetWidth = data.rows;  // 假设数据中的 rows 代表地图宽度
        int gridSize = GetClosestGridSize(targetWidth);
        rows = gridSize;
        cols = gridSize;

        // Step 2：重置占用表（使用选定尺寸）
        ResetOccupancy();

        // Step 3：重置其他状态
        nextId = 1;
        dataAsset = data;
        LevelFinish = false;
        origin = data.origin;  // 保持原点不变

        // Step 4：清理现有子对象（如果指定）
        if (clearExisting)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        // Step 5：遍历数据中的物品，只加载完全在网格范围内的
        foreach (var it in data.items)
        {
            if (it.info == null || it.info.prefab == null) continue;

            // 计算该物品在指定旋转下的占用尺寸
            var dims = FootprintDims(it.info, it.rotIndex);
            // 计算锚点位置（左下角网格坐标）
            var anchor = StartFromPivot(it.gridPos, it.info, it.rotIndex);

            // 检查是否完全在网格边界内
            if (!InBounds(anchor, dims)) continue;  // 超出部分不加载

            // 实例化预制体
            var obj = Instantiate(it.info.prefab, transform);
            var baseRot = obj.transform.rotation;
            obj.transform.rotation = Quaternion.AngleAxis(it.rotIndex * 90f, Vector3.up) * baseRot;
            obj.transform.position = FootprintWorldCenter(anchor, dims);

            // 添加/获取 MapItem 组件并设置属性
            var mi = obj.GetComponent<MapItem>();
            if (mi == null) mi = obj.AddComponent<MapItem>();
            mi.info = it.info;
            mi.gridPos = it.gridPos;
            mi.rotIndex = it.rotIndex;
            mi.baseRotation = baseRot;
            mi.animalType = it.animalType;
            mi.boomTime = it.boomTime;

            // 分配 ID 并记录
            int id = nextId++;
            mi.id = id;
            items[id] = new PlacedItem
            {
                id = id,
                info = it.info,
                gridPos = it.gridPos,
                rotIndex = it.rotIndex,
                instance = obj,
                baseRotation = baseRot
            };

            // 更新占用表
            MarkAreaFormRotate(it.gridPos, dims, id, mi.rotIndex);
            
            if ((AnimalType)it.animalType == AnimalType.Cattle)
            {
                medicineCowItem=mi.info.prefab.GetComponent<MedicineCowItem>();
                if (medicineCowItem == null)
                {
                    Debug.LogError("药牛数据获取异常！");
                }

                if (sickDonkeyItem != null)
                {
                    medicineCowItem.SetLinkedDonkey(sickDonkeyItem);
                }
            }
            
            if ((AnimalType)it.animalType == AnimalType.Donkey)
            {
                sickDonkeyItem=mi.info.prefab.GetComponent<SickDonkeyItem>();
                if (sickDonkeyItem == null)
                {
                    Debug.LogError("病驴数据获取异常！");
                }
                
                if (medicineCowItem != null)
                {
                    medicineCowItem.SetLinkedDonkey(sickDonkeyItem);
                }
            }
        }

        // Step 6：可选——将地图适配到屏幕（根据加载后的物品）
        FitMapToScreen(new Vector2(0.535f, 0.5f));
    }

    public PlacedItem GetPlacedItem(int id)
    {
        PlacedItem item = items[id];
        if (item == null) return null;
        return item;
    }

    /// <summary>
    /// 清空所有地图项
    /// 重置占用表和实例字典
    /// </summary>
    public void ClearAllItems()
    {
        //ResetOccupancy();
        items.Clear();
        nextId = 1;
        selected = null;
#if UNITY_EDITOR
        // var toDelete = new List<GameObject>();
        // UnityEditor.Selection.activeObject = null;
        // for (int i = 0; i < transform.childCount; i++) toDelete.Add(transform.GetChild(i).gameObject);
        // foreach (var go in toDelete) UnityEditor.Undo.DestroyObjectImmediate(go);
#endif
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        //UpdateDataAssetMirror();
    }

    // ==================== 公共 API ====================
    /// <summary>
    /// 尝试移动到指定地图坐标并同步位置
    /// 用于移动地图项
    /// </summary>
    public bool TryMoveItemTargetCell(MapItem item, Vector2Int targetPivot,out Vector3 target)
    {
        // Step 1：解析/确保该项的 id
        //int id = ResolveItemId(item);
        // Step 2：释放当前占用区域
        var dims = FootprintDims(item.info, item.rotIndex);
        //var curAnchor = StartFromPivot(item.gridPos, item.info, item.rotIndex);
        ResetMarkAreaFormRotate(item.gridPos, dims,item.id,item.rotIndex);
        // Step 3：计算目标占用并校验
        var targetAnchor = StartFromPivot(targetPivot, item.info, item.rotIndex);
        item.gridPos = targetPivot;
        //item.transform.position = FootprintWorldCenter(targetAnchor, dims);
        target=FootprintWorldCenter(targetAnchor, dims);
        MarkAreaFormRotate(item.gridPos, dims, item.id,item.rotIndex);
        if (items.TryGetValue(item.id, out var rec)) rec.gridPos = item.gridPos;
        return true;
    }


    public void UpdateMapItemArea(MapItem item)
    {
        // Step 1：解析 id 并释放占用
        //int id = ResolveItemId(item);
        var dims = FootprintDims(item.info, item.rotIndex);
        //var anchor = StartFromPivot(item.gridPos, item.info, item.rotIndex);
        ResetMarkAreaFormRotate(item.gridPos, dims, item.id,item.rotIndex);
        items.Remove(item.id);
    }

    public void RunOutRemoveItem(AnimalBase animal)
    {
        // 动物到达终点，触发跑出事件
        OnAnimalEscaped?.Invoke(animal);
        RemoveItem(animal.MapItem);
    }

    /// <summary>
    /// 移除地图项
    /// 释放占用并销毁实例
    /// </summary>
    public bool RemoveItem(MapItem item)
    {
        // Step 1：解析 id 并释放占用
        UpdateMapItemArea(item);

        // Step 2：移除记录并销毁对象

        Destroy(item.gameObject);

        // Step 3：检查是否所有物品都已销毁
        if (items.Count == 0&&!LevelFinish)
        {
            OnAllItemsDestroyed?.Invoke();
            LevelFinish = true;
        }
        return true;
    }

    // 获取指定网格的占用者ID（-1表示空闲）
    public int GetOccupantIdAtCell(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= rows || cell.y < 0 || cell.y >= cols)
            return -1;
        return occupancy[cell.x, cell.y];
    }
  
    
    /// <summary>
    /// 根据实际放置的物品占用区域，缩放并移动地图使其完整显示在屏幕内，并可指定目标屏幕位置（视口坐标）。
    /// </summary>
    /// <param name="targetScreenUV">目标屏幕位置，视口坐标 (0,0) 左下角到 (1,1) 右上角。默认 null 表示屏幕中心 (0.5,0.5)。</param>
    public void FitMapToScreen(Vector2? targetScreenUV = null)
    {
        if (!cam.orthographic)
        {
            Debug.LogWarning("相机不是正交相机，此方法仅适用于正交相机。");
            return;
        }

        // 如果没有物品，则无需调整
        if (items.Count == 0)
        {
            Debug.Log("没有物品，不进行缩放适配。");
            return;
        }

        // 初始化极值
        int minRow = int.MaxValue;
        int maxRow = int.MinValue;
        int minCol = int.MaxValue;
        int maxCol = int.MinValue;

        // 遍历所有已放置物品，收集占用的所有格子
        foreach (var kv in items)
        {
            var placed = kv.Value;
            var info = placed.info;
            var gridPos = placed.gridPos;
            var rotIndex = placed.rotIndex;
        
            var dims = FootprintDims(info, rotIndex);
            var anchor = StartFromPivot(gridPos, info, rotIndex);
        
            for (int r = 0; r < dims.x; r++)
            {
                for (int c = 0; c < dims.y; c++)
                {
                    int row = anchor.x + r;
                    int col = anchor.y + c;
                    if (row < minRow) minRow = row;
                    if (row > maxRow) maxRow = row;
                    if (col < minCol) minCol = col;
                    if (col > maxCol) maxCol = col;
                }
            }
        }

        // 计算实际占用区域的尺寸（世界单位）
        float occupiedWidth = (maxCol - minCol + 1) * cellSize;
        float occupiedHeight = (maxRow - minRow + 1) * cellSize;

        // 正交相机的视野范围
        float screenWorldHeight = 2f * cam.orthographicSize;
        float screenWorldWidth = screenWorldHeight * cam.aspect;

        // 计算所需缩放比例
        float scaleHeight = screenWorldHeight / occupiedHeight;
        float scaleWidth = screenWorldWidth / occupiedWidth;
        float scale = Mathf.Min(scaleHeight, scaleWidth);
        
        // 在需要的地方使用
        int mscaleid = Array.FindIndex(AvailableGridSizes, size => size == rows);
        float mapScale = mapScales[mapScales.Length-1];
        if (mscaleid >= 0)
        {
            mapScale = mapScales[mscaleid];
        }
        else
        {
            Debug.LogWarning($"地图尺寸 {rows}x{cols} 不在预定义列表中，使用默认缩放");
        }
        // 限制最大缩放不超过 1.3f
        scale = Mathf.Min(mapScale, 1.3f);

        // 应用均匀缩放
        transform.localScale = Vector3.one * scale;

        // 计算缩放后实际占用区域中心的世界坐标
        Vector2Int occupiedAnchor = new Vector2Int(minRow, minCol);
        Vector2Int occupiedDims = new Vector2Int(maxRow - minRow + 1, maxCol - minCol + 1);
        Vector3 occupiedCenter = FootprintWorldCenter(occupiedAnchor, occupiedDims);

        // 确定目标屏幕位置（默认中心）
        Vector2 screenUV = targetScreenUV ?? new Vector2(0.5f, 0.5f);

        // 从相机发射一条穿过目标屏幕位置的射线
        Ray camRay = cam.ViewportPointToRay(screenUV);
        Plane groundPlane = new Plane(Vector3.up, occupiedCenter); // 平面经过区域中心，法线向上

        float enter;
        if (groundPlane.Raycast(camRay, out enter))
        {
            Vector3 targetWorldPoint = camRay.GetPoint(enter);
            // 移动地图使区域中心与目标世界点重合
            transform.position += targetWorldPoint - occupiedCenter;
        }
        else
        {
            Debug.LogWarning("相机视线未与地图平面相交，无法自动居中。");
        }
    }
}

