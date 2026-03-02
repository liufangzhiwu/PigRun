using UnityEngine;

/// <summary>
/// 预制体配置信息资产
/// 定义预制体在地图网格上的占用尺寸和旋转锚点
/// </summary>
[CreateAssetMenu(menuName = "Map/PrefabInfo")]
public class PrefabInfo : ScriptableObject
{
    #region 预制体引用
    // 预制体资源引用
    public GameObject prefab;
    #endregion
    
    #region 网格占用配置
    // 该预制体在网格上占据的行数（默认 1）
    public int rows = 1;
    // 该预制体在网格上占据的列数（默认 1）
    public int cols = 1;
    #endregion
    
    #region 旋转锚点配置
    // 旋转/定位的锚点所在行索引（-1 表示使用行的中点）
    public int pivotRow = -1;
    // 旋转/定位的锚点所在列索引（-1 表示使用列的中点）
    public int pivotCol = -1;
    #endregion
}

