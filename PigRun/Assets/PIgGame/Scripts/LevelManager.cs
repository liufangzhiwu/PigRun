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

    // 缓存
    private Dictionary<int, PrefabInfo> animalPrefabCache = new Dictionary<int, PrefabInfo>();
    private Dictionary<int, PrefabInfo> obstaclePrefabCache = new Dictionary<int, PrefabInfo>();

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

    public void LoadLevel(int levelid)
    {
        StartCoroutine(LoadLevelCoroutine(levelid));
    }

    private IEnumerator LoadLevelCoroutine(int levelid)
    {
        string fileName = LevelName + levelid;
        TextAsset levelTextAsset = AssetBundleLoader.SharedInstance.LoadTextFile("levels", fileName);
        MapData mapData = ParseToMapData(levelTextAsset.ToString(), cellSize);

        // 清空并重置地图
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

        // 分批实例化所有物品（动物 + 障碍物）
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

        Map.Instance.FitMapToScreen(new Vector2(0.53f, 0.48f));
        Map.Instance.OnLoadNewMapEvent();

        Debug.Log($"关卡 {levelid} 加载完成，共 {totalCount} 个物体（动物+障碍物）");
        GameManager.instance.OverLevelLoadedEvent();
    }

    /// <summary>
    /// 动态加载动物 PrefabInfo（按路径 Resources/Prefabs/Pigs/Type_{typeId}）
    /// </summary>
    private PrefabInfo LoadAnimalPrefabInfo(int typeId)
    {
        if (animalPrefabCache.TryGetValue(typeId, out var cached))
            return cached;

        string path = $"Prefabs/Pigs/Type_{typeId}";
        PrefabInfo info = Resources.Load<PrefabInfo>(path);
        if (info == null)
        {
            Debug.LogError($"无法加载动物预制体: {path}");
            return null;
        }
        animalPrefabCache[typeId] = info;
        return info;
    }

    /// <summary>
    /// 加载障碍物 PrefabInfo（从 Inspector 映射表）
    /// </summary>
    private PrefabInfo LoadObstaclePrefabInfo(int obstacleId)
    {
        if (obstaclePrefabCache.TryGetValue(obstacleId, out var cached))
            return cached;

        string path = $"Prefabs/ObstacleIds/Type_{obstacleId}";
        PrefabInfo info = Resources.Load<PrefabInfo>(path);
        if (info == null)
        {
            Debug.LogError($"无法加载障碍物预制体: {path}");
            return null;
        }
        obstaclePrefabCache[obstacleId] = info;
        return info;
    }

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

        // ---- 处理动物 ----
        foreach (var pig in level.pigGroup)
        {
            int typeId = (int)pig.type;
            PrefabInfo info = LoadAnimalPrefabInfo(typeId);
            if (info == null) continue;

            MapData.MapItemData item = new MapData.MapItemData();
            item.info = info;
            item.animalType = typeId;   // 正数表示动物
            item.boomTime = (int)pig.boomTime;

            int gridX = Mathf.RoundToInt((pig.position.x - mapData.origin.x) / cellSize);
            int gridY = Mathf.RoundToInt((pig.position.y - mapData.origin.y) / cellSize);
            // 注意：动物角度映射特殊处理 (angle/90 - 1) % 4
            int rotIndex = ((int)pig.angle / 90 - 1) % 4;
            item.rotIndex = rotIndex;

            if (item.rotIndex == -1)
                item.gridPos = new Vector2Int(gridX - 1, gridY - 1);
            else
                item.gridPos = new Vector2Int(gridX, gridY);

            mapData.items.Add(item);
        }

        // ---- 处理障碍物 ----
        if (level.obstacleGroup != null)
        {
            foreach (var obs in level.obstacleGroup)
            {
                PrefabInfo info = LoadObstaclePrefabInfo((int)obs.id);
                if (info == null) continue;

                MapData.MapItemData item = new MapData.MapItemData();
                item.info = info;
                item.animalType = -1;   // 标记为障碍物
                item.obstacleIdType =(int)obs.id;   // 障碍物类型
                item.way =obs.way;   // 障碍物类型
                item.boomTime = 0;

                int gridX = Mathf.RoundToInt((obs.position.x - mapData.origin.x) / cellSize);
                int gridY = Mathf.RoundToInt((obs.position.y - mapData.origin.y) / cellSize);
                // 障碍物旋转：直接 angle/90，无偏移
                int rotIndex = ((int)obs.angle / 90) % 4;
                item.rotIndex = rotIndex;

                // 障碍物锚点：假定锚点在中心（或按 PrefabInfo 的 pivot 计算）
                // 为了简化，直接使用计算出的网格作为锚点，具体占用由 ComputeOccupiedCells 根据 PrefabInfo.rows/cols 决定
                item.gridPos = new Vector2Int(gridX, gridY);

                mapData.items.Add(item);
            }
        }

        return mapData;
    }
}