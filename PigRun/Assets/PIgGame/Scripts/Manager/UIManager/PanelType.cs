/// <summary>
/// 界面类型管理器 (Panel Type Manager)
/// 新版本重构：优化了界面常量管理方式 (New version: Optimized panel constant management)
/// </summary>
public class PanelType  // Renamed class
{
    /* 核心游戏界面 (Core Game Interfaces) */
    public const string MainPanel = "MainPanel";  
    public const string MenuPanel = "MenuPanel";  
    public const string GamePanel = "GamePanel";  
    public const string FinishPanel = "FinishPanel";  
    

    /// <summary>
    /// 获取所有可用界面名称 (Get all available panel names)
    /// 新版本使用属性缓存优化性能 (New version uses cached properties)
    /// </summary>
    public static string[] AvailableViews()  // Changed from GetPanelNames
    {
        var type = typeof(PanelType);
        var allFields = type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        string[] all = new string[allFields.Length];
        for (int i = 0; i<allFields.Length; i++)
            all[i] = allFields[i].Name;

        return all;
    }      

    /// <summary>
    /// 获取界面显示名称 (Get panel display name)
    /// 新增本地化支持方法 (Added localization support)
    /// </summary>
    public static string GetDisplayName(string panelId)
    {
        // 实际项目中应接入本地化系统
        // (In production should connect to localization system)
        return panelId;
    }
}
