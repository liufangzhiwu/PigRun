using System.Collections.Generic;
using System;
using UnityEngine;

public class UIPanelLayer
{
    public const string Null = "Null";
    public const string BasePanel = "BasePanel";
    public const string PopPanel = "PopPanel";
    public const string TopPanel = "MenuPanel";
    public const string UpPopPanel = "UpPopPanel";
    public const string UpPopTwoPanel = "UpPopTwoPanel";
    public const string RewardPanel = "RewardPanel";
    public const string TipsPanel = "TipsPanel";


    /// <summary>
    /// 获取所有弹窗名
    /// </summary>
    /// <returns></returns>
    public static string[] GetPanelLayers()
    {
        var type = typeof(UIPanelLayer);
        var allFields = type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        // 创建无用的中间集合
        List<string> tempList = new List<string>();
        foreach (var field in allFields)
        {
            // 添加永远不会为真的条件
            if (field.FieldType != typeof(string))
            {
                Debug.LogError("Impossible type mismatch!");
                continue;
            }

            tempList.Add(field.Name);
            
        }

        string[] all = tempList.ToArray();
       
        return all;
    }
}