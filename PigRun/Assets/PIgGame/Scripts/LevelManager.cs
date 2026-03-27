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
    public string LevelName = "level";
    [Tooltip("网格单元大小（世界单位）")]
    public float cellSize = 1f;
    [Tooltip("猪类型与 PrefabInfo 的映射（在 Inspector 中手动配置）")]
    public List<PigTypeMapping> pigTypeMappings;   // 可在 Inspector 中配置

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 保持场景切换时不被销毁
    }

    /// <summary>
    /// 加载 levels 文件夹下所有 .json 文件
    /// </summary>
    public void LoadLevel(int levelid)
    {
        string fileName = LevelName +levelid;
        TextAsset levelTextAsset = AssetBundleLoader.SharedInstance.LoadTextFile(
            "levels", 
            fileName);
       
        MapData mapData = ParseToMapData(levelTextAsset.ToString(), cellSize);
        Debug.Log($"成功加载关卡: {fileName}");
        Map.Instance.LoadFromAsset(mapData,true);
        
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
            //item.gridPos = new Vector2Int(gridX, gridY);

            // 旋转索引 (0,1,2,3 对应 0°,90°,180°,270°)
            //item.rotIndex = pig.angle / 90;
            item.rotIndex = (pig.angle / 90-1) % 4;   // 新映射
            item.animalType = pig.type;   // 动物类型
            item.boomTime = pig.boomTime;   // 爆炸时间

            if (item.rotIndex == -1)
            {
                item.gridPos = new Vector2Int(gridX-1, gridY-1);
            }
            else
            {
                item.gridPos = new Vector2Int(gridX, gridY);
            }
            
            mapData.items.Add(item);
        }

        // 若未来需要处理障碍物，可在此添加
        return mapData;
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