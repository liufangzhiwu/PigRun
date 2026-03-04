using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    [Header("关卡设置")]
    [Tooltip("要加载的关卡名称（对应文件名，如 'level1'）")]
    public string levelName = "level1";

    [Tooltip("是否在 Start 时自动加载")]
    public bool loadOnStart = true;

    private void Start()
    {
        if (loadOnStart)
        {
            LoadLevel(levelName);
        }
    }

    /// <summary>
    /// 根据关卡名称加载地图
    /// </summary>
    public void LoadLevel(string name)
    {
        // 1. 通过 LevelManager 获取 MapData
        MapData mapData = LevelManager.Instance.GetLevel(name);
        if (mapData == null)
        {
            Debug.LogError($"关卡 '{name}' 不存在或加载失败");
            return;
        }

        // 2. 确保场景中有 Map 组件（通常挂载在场景的一个空对象上，例如 "Map"）
        if (Map.Instance == null)
        {
            Debug.LogError("场景中不存在 Map 实例，请先放置 Map 组件");
            return;
        }

        // 3. 将 MapData 加载到 Map 中（会清除现有物品）
        Map.Instance.LoadFromAsset(mapData, true);
        Debug.Log($"成功加载关卡: {name}");
    }

    /// <summary>
    /// 按索引加载（例如 0 对应第一个关卡）
    /// </summary>
    public void LoadLevelByIndex(int index)
    {
        MapData mapData = LevelManager.Instance.GetLevelByIndex(index);
        if (mapData != null && Map.Instance != null)
        {
            Map.Instance.LoadFromAsset(mapData, true);
        }
    }
}