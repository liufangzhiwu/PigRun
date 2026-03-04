using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// 关卡管理器单例，负责加载和访问关卡数据（MapData）
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("配置")]
    [Tooltip("JSON 文件所在的文件夹路径（相对于 Application.dataPath）")]
    public string levelsFolder = "PIgGame/MultipleData/GameInfo/levels";
    [Tooltip("网格单元大小（世界单位）")]
    public float cellSize = 1f;
    [Tooltip("猪类型与 PrefabInfo 的映射（在 Inspector 中手动配置）")]
    public List<PigTypeMapping> pigTypeMappings;   // 可在 Inspector 中配置

    // 存储所有已加载的关卡，键为文件名（不含扩展名），值为 MapData
    private Dictionary<string, MapData> loadedLevels = new Dictionary<string, MapData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 保持场景切换时不被销毁

        LoadAllLevels();
    }

    /// <summary>
    /// 加载 levels 文件夹下所有 .json 文件
    /// </summary>
    private void LoadAllLevels()
    {
        string folderPath = Path.Combine(Application.dataPath, levelsFolder);
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"关卡文件夹不存在: {folderPath}");
            return;
        }

        string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");
        foreach (string filePath in jsonFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                MapData mapData = ParseToMapData(jsonContent, cellSize);
                loadedLevels[fileName] = mapData;
                Debug.Log($"成功加载关卡: {fileName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载关卡文件 {fileName} 失败: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 将 JSON 字符串转换为 MapData（调用 LevelJsonParser 的逻辑，并注入 PrefabInfo 映射）
    /// </summary>
    private MapData ParseToMapData(string jsonContent, float cellSize)
    {
        // 反序列化 JSON
        LevelData level = JsonConvert.DeserializeObject<LevelData>(jsonContent);

        // 创建 MapData 实例
        MapData mapData = ScriptableObject.CreateInstance<MapData>();

        // 设置地图网格参数
        mapData.cellSize = cellSize;
        mapData.origin = Vector3.zero;
        mapData.cols = Mathf.RoundToInt(level.size.x / cellSize);
        mapData.rows = Mathf.RoundToInt(level.size.y / cellSize);
        mapData.version = "1.0";
        mapData.items = new List<MapData.MapItemData>();

        // 构建猪类型映射字典（从 Inspector 配置的列表转换）
        Dictionary<int, PrefabInfo> pigPrefabMap = new Dictionary<int, PrefabInfo>();
        foreach (var mapping in pigTypeMappings)
        {
            if (!pigPrefabMap.ContainsKey(mapping.typeId))
                pigPrefabMap.Add(mapping.typeId, mapping.prefabInfo);
            else
                Debug.LogWarning($"重复的猪类型 ID: {mapping.typeId}");
        }

        // 处理猪群
        foreach (var pig in level.pigGroup)
        {
            MapData.MapItemData item = new MapData.MapItemData();

            if (pigPrefabMap.TryGetValue(pig.type, out PrefabInfo info))
            {
                item.info = info;
            }
            else
            {
                Debug.LogWarning($"未找到类型 {pig.type} 对应的 PrefabInfo，尝试从 Resources 加载...");
                // 备选方案：按约定路径从 Resources 加载（例如 "Prefabs/Pigs/Type_" + pig.type）
                string resPath = $"Prefabs/Pigs/Type_{pig.type}";
                PrefabInfo loadedInfo = Resources.Load<PrefabInfo>(resPath);
                if (loadedInfo != null)
                {
                    item.info = loadedInfo;
                    // 可选：自动添加到映射缓存，避免重复警告
                    pigPrefabMap[pig.type] = loadedInfo;
                }
                else
                {
                    Debug.LogError($"无法加载 PrefabInfo，路径: {resPath}");
                    continue;
                }
            }

            // 计算网格坐标
            int gridX = Mathf.RoundToInt((pig.position.x - mapData.origin.x) / cellSize);
            int gridY = Mathf.RoundToInt((pig.position.y - mapData.origin.y) / cellSize);
            item.gridPos = new Vector2Int(gridX, gridY);

            // 旋转索引 (0,1,2,3 对应 0°,90°,180°,270°)
            //item.rotIndex = pig.angle / 90;
            item.rotIndex = (pig.angle / 90-1) % 4;   // 新映射
            
            mapData.items.Add(item);
        }

        // 若未来需要处理障碍物，可在此添加
        return mapData;
    }

    /// <summary>
    /// 通过关卡名称获取 MapData（不区分大小写）
    /// </summary>
    public MapData GetLevel(string levelName)
    {
        if (loadedLevels.TryGetValue(levelName, out MapData data))
            return data;
        else
            Debug.LogError($"未找到关卡: {levelName}");
        return null;
    }

    /// <summary>
    /// 通过索引获取关卡（按文件名排序）
    /// </summary>
    public MapData GetLevelByIndex(int index)
    {
        if (index < 0 || index >= loadedLevels.Count)
        {
            Debug.LogError($"索引超出范围: {index}");
            return null;
        }
        List<string> keys = new List<string>(loadedLevels.Keys);
        keys.Sort(); // 自然排序
        return loadedLevels[keys[index]];
    }

    /// <summary>
    /// 获取所有已加载的关卡名称
    /// </summary>
    public List<string> GetAllLevelNames()
    {
        return new List<string>(loadedLevels.Keys);
    }

    /// <summary>
    /// 重新加载所有关卡（清空缓存）
    /// </summary>
    public void ReloadAllLevels()
    {
        loadedLevels.Clear();
        LoadAllLevels();
    }
}

/// <summary>
/// 用于在 Inspector 中配置猪类型 ID 与 PrefabInfo 的映射
/// </summary>
[System.Serializable]
public class PigTypeMapping
{
    public int typeId;
    public PrefabInfo prefabInfo;
}