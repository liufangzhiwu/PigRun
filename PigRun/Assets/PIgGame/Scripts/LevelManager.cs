using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    // 缓存已加载的 PrefabInfo，避免重复从 Resources 加载
    private Dictionary<int, PrefabInfo> prefabInfoCache = new Dictionary<int, PrefabInfo>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void OnEnable()
    {
        LoadLevel(GameDataManager.Instance.UserData.LevelIndex);
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
        Map.Instance.ClearAllItems();
        int targetWidth = mapData.rows;
        int gridSize = Map.Instance.GetClosestGridSize(targetWidth);

        Map.Instance.transform.position = Vector3.zero;
        Map.Instance.transform.localScale = Vector3.one;

        Map.Instance.rows = gridSize;
        Map.Instance.cols = gridSize;
        Map.Instance.ResetOccupancy();
        Map.Instance.dataAsset = mapData;
        Map.Instance.origin = mapData.origin;
        Map.Instance.LevelFinish = false;

        // 3. 分批实例化物品
        List<MapData.MapItemData> itemsToLoad = mapData.items;
        int totalCount = itemsToLoad.Count;
        int batchSize = 3;
        int loaded = 0;

        while (loaded < totalCount)
        {
            int end = Mathf.Min(loaded + batchSize, totalCount);
            for (int i = loaded; i < end; i++)
            {
                Map.Instance.InstantiateItem(itemsToLoad[i]);
            }
            loaded = end;
            yield return null;
        }

        // 4. 所有物品加载完成，适配屏幕并触发事件
        Map.Instance.FitMapToScreen(new Vector2(0.53f, 0.48f));
        Map.Instance.OnLoadNewMapEvent();

        Debug.Log($"关卡 {levelid} 加载完成，共 {totalCount} 个动物");
        GameManager.instance.OverLevelLoadedEvent();
    }

    /// <summary>
    /// 根据类型 ID 动态加载 PrefabInfo（自动从 Resources 中按规则加载）
    /// </summary>
    private PrefabInfo LoadPrefabInfoByType(int typeId)
    {
        // 先从缓存查找
        if (prefabInfoCache.TryGetValue(typeId, out var cached))
            return cached;

        // 构建资源路径，格式：Prefabs/Pigs/Type_{typeId}
        // 注意：根据你的目录结构，Type_0, Type_1, ... 等预制体直接放在 Prefabs/Pigs 下
        string path = $"Prefabs/Pigs/Type_{typeId}";
        PrefabInfo loadedInfo = Resources.Load<PrefabInfo>(path);
        if (loadedInfo == null)
        {
            Debug.LogError($"无法加载PrefabInfo预制体，路径: {path}");
            return null;
        }

        // 缓存并返回
        prefabInfoCache[typeId] = loadedInfo;
        return loadedInfo;
    }

    /// <summary>
    /// 将 JSON 字符串转换为 MapData（自动加载 PrefabInfo）
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

        foreach (var pig in level.pigGroup)
        {
            int typeId = (int)pig.type;
            PrefabInfo info = LoadPrefabInfoByType(typeId);
            if (info == null) continue;

            MapData.MapItemData item = new MapData.MapItemData();
            item.info = info;

            int gridX = Mathf.RoundToInt((pig.position.x - mapData.origin.x) / cellSize);
            int gridY = Mathf.RoundToInt((pig.position.y - mapData.origin.y) / cellSize);
            item.rotIndex = ((int)pig.angle / 90 - 1) % 4;
            item.animalType = typeId;
            item.boomTime = (int)pig.boomTime;

            if (item.rotIndex == -1) // 0度
                item.gridPos = new Vector2Int(gridX - 1, gridY - 1);
            else
                item.gridPos = new Vector2Int(gridX, gridY);

            mapData.items.Add(item);
        }

        return mapData;
    }
}