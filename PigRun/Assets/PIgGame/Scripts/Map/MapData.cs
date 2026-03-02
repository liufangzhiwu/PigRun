using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图数据资产类
/// 用于序列化和持久化地图配置，支持在编辑器和运行时加载地图布局
/// </summary>
[CreateAssetMenu(menuName = "Map/MapData")]
public class MapData : ScriptableObject
{
    #region 地图网格配置
    // 地图网格行数
    public int rows;
    // 地图网格列数
    public int cols;
    // 单个网格单元的尺寸（世界单位）
    public float cellSize;
    // 地图原点的本地坐标（左下角起点）
    public Vector3 origin;
    // 数据版本号，用于后续兼容性管理
    public string version = "1.0";
    #endregion
    
    #region 地图项数据结构
    /// <summary>
    /// 地图项数据结构
    /// 记录单个预制体实例在地图上的位置和旋转信息
    /// </summary>
    [System.Serializable]
    public struct MapItemData
    {
        // 预制体信息引用（包含尺寸、锚点等配置）
        public PrefabInfo info;
        // 实例锚点所在的网格坐标
        public Vector2Int gridPos;
        // 旋转索引（0/1/2/3 对应 0°/90°/180°/270°）
        public int rotIndex;
    }
    #endregion
    
    #region 地图项列表
    // 存储所有已放置的地图项数据
    public List<MapItemData> items = new List<MapItemData>();
    #endregion
}

