using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// 关卡管理器单例，负责加载和访问关卡数据（MapData）
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("配置")]
    public string LevelName = "level";
    public float cellSize = 1f;
    public List<PigTypeMapping> pigTypeMappings;

    // 加载完成事件
    public System.Action OnLevelLoaded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 加载关卡（异步协程，分批实例化）
    /// </summary>
    public void LoadLevel(int levelid)
    {
        StartCoroutine(LoadLevelCoroutine(levelid));
    }

    private IEnumerator LoadLevelCoroutine(int levelid)
    {
        // 1. 加载并解析 JSON
        string fileName = LevelName + levelid;
        TextAsset levelTextAsset = AssetBundleLoader.SharedInstance.LoadTextFile("levels", fileName);
        MapData mapData = ParseToMapData(levelTextAsset.ToString(), cellSize);

        // 2. 准备地图（清空、重置占用表等）
        Map.Instance.ClearAllItems();               // 清空现有物品
        int targetWidth = mapData.rows;
        int gridSize =Map.Instance.GetClosestGridSize(targetWidth);
        
        // 【新增】重置地图变换，避免位置偏移
        Map.Instance.transform.position = Vector3.zero;
        Map.Instance.transform.localScale = Vector3.one;
        
        Map.Instance.rows = gridSize;
        Map.Instance.cols = gridSize;
        Map.Instance.ResetOccupancy();               // 需要 public
        Map.Instance.dataAsset = mapData;
        Map.Instance.origin = mapData.origin;
        Map.Instance.LevelFinish = false;

        // 3. 分批实例化物品
        List<MapData.MapItemData> itemsToLoad = mapData.items;
        int totalCount = itemsToLoad.Count;
        int batchSize = 3;  // 每帧实例化数量，可根据性能调整
        int loaded = 0;

        while (loaded < totalCount)
        {
            int end = Mathf.Min(loaded + batchSize, totalCount);
            for (int i = loaded; i < end; i++)
            {
                Map.Instance.InstantiateItem(itemsToLoad[i]);   // 需要添加的方法
            }
            loaded = end;
            yield return null; // 等待下一帧
        }

        // 4. 所有物品加载完成，适配屏幕并触发事件
        Map.Instance.FitMapToScreen(new Vector2(0.55f, 0.45f));
        Map.Instance.OnLoadNewMapEvent();

        Debug.Log($"关卡 {levelid} 加载完成，共 {totalCount} 个动物");
        OnLevelLoaded?.Invoke();
    }

    /// <summary>
    /// 将 JSON 字符串转换为 MapData
    /// </summary>
    private MapData ParseToMapData(string jsonContent, float cellSize)
    {
        LevelData level = JsonConvert.DeserializeObject<LevelData>(jsonContent);

        MapData mapData = ScriptableObject.CreateInstance<MapData>();
        mapData.cellSize = cellSize;
        mapData.origin = Vector3.zero;
        mapData.cols = Mathf.RoundToInt(level.size.x / cellSize);
        mapData.rows = Mathf.RoundToInt(level.size.y / cellSize);
        mapData.version = "1.0";
        mapData.items = new List<MapData.MapItemData>();

        // 构建映射字典
        Dictionary<int, PrefabInfo> pigPrefabMap = new Dictionary<int, PrefabInfo>();
        foreach (var mapping in pigTypeMappings)
        {
            if (!pigPrefabMap.ContainsKey(mapping.typeId))
                pigPrefabMap.Add(mapping.typeId, mapping.prefabInfo);
            else
                Debug.LogWarning($"重复的猪类型 ID: {mapping.typeId}");
        }

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
                string resPath = $"Prefabs/Pigs/Type_{pig.type}";
                PrefabInfo loadedInfo = Resources.Load<PrefabInfo>(resPath);
                if (loadedInfo != null)
                {
                    item.info = loadedInfo;
                    pigPrefabMap[pig.type] = loadedInfo;
                }
                else
                {
                    Debug.LogError($"无法加载 PrefabInfo，路径: {resPath}");
                    continue;
                }
            }

            int gridX = Mathf.RoundToInt((pig.position.x - mapData.origin.x) / cellSize);
            int gridY = Mathf.RoundToInt((pig.position.y - mapData.origin.y) / cellSize);
            item.rotIndex = (pig.angle / 90 - 1) % 4;
            item.animalType = pig.type;
            item.boomTime = pig.boomTime;

            if (item.rotIndex == -1) //0度
                item.gridPos = new Vector2Int(gridX - 1, gridY-1);
            else
                item.gridPos = new Vector2Int(gridX, gridY);

            mapData.items.Add(item);
        }

        return mapData;
    }
}

[System.Serializable]
public class PigTypeMapping
{
    public int typeId;
    public PrefabInfo prefabInfo;
}