using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;  // 需要安装 Newtonsoft.Json 包

public static class LevelJsonParser
{
    // 将 JSON 字符串转换为 MapData 资产
    public static MapData ParseToMapData(string jsonContent, float cellSize = 1f)
    {
        // 1. 反序列化 JSON
        LevelData level = JsonConvert.DeserializeObject<LevelData>(jsonContent);

        // 2. 创建 MapData 实例
        MapData mapData = ScriptableObject.CreateInstance<MapData>();

        // 3. 设置地图网格参数
        mapData.cellSize = cellSize;
        mapData.origin = Vector3.zero;               // 假设原点在左下角
        // 根据世界大小计算网格行列数（假设网格单元为正方形）
        mapData.cols = Mathf.RoundToInt(level.size.x / cellSize);
        mapData.rows = Mathf.RoundToInt(level.size.y / cellSize);
        mapData.version = "1.0";

        // 4. 清空已有 items（新实例默认为空）
        mapData.items = new List<MapData.MapItemData>();

        // 5. 定义猪的类型到 PrefabInfo 的映射（示例，需根据实际项目配置）
        // 这里假设 PrefabInfo 可以从 Resources 加载或由外部传入
        Dictionary<int, PrefabInfo> pigPrefabMap = new Dictionary<int, PrefabInfo>();
        // 例如：pigPrefabMap[0] = Resources.Load<PrefabInfo>("Prefabs/Pig_Type0");

        // 6. 处理猪群
        foreach (var pig in level.pigGroup)
        {
            MapData.MapItemData item = new MapData.MapItemData();

            // 根据 type 获取对应的 PrefabInfo
            if (pigPrefabMap.TryGetValue(pig.type, out PrefabInfo info))
            {
                item.info = info;
            }
            else
            {
                Debug.LogWarning($"未找到类型 {pig.type} 对应的 PrefabInfo，跳过该猪");
                continue;
            }

            // 将世界坐标转换为网格坐标（假设锚点在单元格中心，且原点左下角）
            // 公式：gridX = (worldX - origin.x) / cellSize，并取整
            int gridX = Mathf.RoundToInt((pig.position.x - mapData.origin.x) / cellSize);
            int gridY = Mathf.RoundToInt((pig.position.y - mapData.origin.y) / cellSize);
            item.gridPos = new Vector2Int(gridX, gridY);

            // 将角度（0,90,180,270）转换为旋转索引（0=0°,1=90°,2=180°,3=270°）
            // 注意：270° 对应索引 3
            item.rotIndex = pig.angle / 90;   // 前提是 angle 始终为 0,90,180,270

            mapData.items.Add(item);
        }

        // 7. 处理障碍物（如果未来有数据，可类似处理）
        // 目前 obstacleGroup 为空，跳过

        return mapData;
    }
}