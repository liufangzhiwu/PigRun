using UnityEngine;

/// <summary>
/// 障碍物点击处理：显示障碍物名称及不可移动提示
/// </summary>
public class ObstacleClickHandler : MonoBehaviour
{
    private MapItem mapItem;

    private void Awake()
    {
        mapItem = GetComponent<MapItem>();
    }

    private void OnMouseUpAsButton()
    {
        if (UIManager.Instance.IsPanelTypeShowing() || !UIManager.Instance.PanelIsShowing(PanelType.GamePanel))
            return;

        // 获取障碍物类型ID（如果存储了）
        int typeId = mapItem.obstacleIdType;
        string obstacleName = GetObstacleName(typeId);

        MessageSystem.Instance.ShowTip($"{obstacleName} 是障碍物，无法移动！");
    }

    private string GetObstacleName(int typeId)
    {
        switch (typeId)
        {
            case 0: return "木围栏";
            case 1: return "长电围栏";
            case 2: return "短电围栏";
            case 4: return "农场主";
            case 10: return "方形铁笼";
            case 11: return "T形铁笼";
            default: return "障碍物";
        }
    }
}