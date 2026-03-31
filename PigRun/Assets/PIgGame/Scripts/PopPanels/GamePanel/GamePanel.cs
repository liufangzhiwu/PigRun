using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePanel : UIBase
{
    [SerializeField] private Text LevelText;
    [SerializeField] private Button removeButton;
    [SerializeField] private Button shuffleButton;
    [SerializeField] private Button reverseButton;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        InitUI();
    }

    protected override void InitButtonEvents()
    {
        base.InitButtonEvents();
        removeButton.AddClickAction(() => ClickToolButton(ToolType.Remove));
        shuffleButton.AddClickAction(() => ClickToolButton(ToolType.Shuffle));
        reverseButton.AddClickAction(() => ClickToolButton(ToolType.Reverse));
    }

    private void InitUI()
    {
        LevelText.text = "关卡" + GameDataManager.Instance.UserData.LevelIndex;
    }
    
    void Start()
    {
        if (Map.Instance != null)
        {
            Map.Instance.OnAllItemsDestroyed += ShowLevelComplete;
            Map.Instance.OnLoadNewMap += InitUI;
        }
    }

    void ShowLevelComplete()
    {
        GameDataManager.Instance.UserData.UpdateLevelIndex();
        UIManager.Instance.ShowPanel(PanelType.FinishPanel);
    }

    private void ClickToolButton(ToolType toolType)
    {
        // 显示道具面板并设置道具类型
        var toolPanel = UIManager.Instance.ShowPanel(PanelType.UseToolPanel) as UseToolPanel;
        if (toolPanel != null)
        {
            toolPanel.SetToolType(toolType);
        }
    }

    void OnDestroy()
    {
        if (Map.Instance != null)
        {
            Map.Instance.OnAllItemsDestroyed -= ShowLevelComplete;
            Map.Instance.OnLoadNewMap -= InitUI;
        }
    }
}