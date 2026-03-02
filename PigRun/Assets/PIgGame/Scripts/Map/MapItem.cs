using UnityEngine;

/// <summary>
/// 地图项组件
/// 附加到已放置的预制体实例上，记录其在地图上的位置和旋转信息
/// </summary>
public class MapItem : MonoBehaviour
{
    #region 预制体配置
    // 该地图实例的预制体信息（尺寸与锚点）
    public PrefabInfo info;
    #endregion
    
    #region 位置和旋转信息
    // 实例锚点所在的网格坐标（Pivot 网格）
    public Vector2Int gridPos;
    // 顺时针旋转索引（0/1/2/3 分别对应 0/90/180/270°）
    public int rotIndex;
    // 预制体的初始旋转（作为世界 Y 轴叠加的基底）
    public Quaternion baseRotation;
    #endregion
}

