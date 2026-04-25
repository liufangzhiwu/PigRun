using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePanel : UIBase
{
    [SerializeField] private Text LevelText;
    [SerializeField] private Text removeText;
    [SerializeField] private Text shuffleText;
    [SerializeField] private Text reverseText;
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
        reverseButton.AddClickAction(() => ClickToolButton(ToolType.Reverse));  // 注意：翻转道具对应 Flip
    }

    private void InitUI()
    {
        var userData = GameDataManager.Instance.UserData;
        LevelText.text = $"第{userData.LevelIndex}关";
        removeText.text = userData.GetToolCount(ToolType.Remove).ToString();
        shuffleText.text = userData.GetToolCount(ToolType.Shuffle).ToString();
        reverseText.text = userData.GetToolCount(ToolType.Reverse).ToString();  // 翻转数量
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
        var userData = GameDataManager.Instance.UserData;
        int count = userData.GetToolCount(toolType);
        
        if (count > 0)
        {
            // 数量足够，直接执行道具逻辑
            ExecuteTool(toolType);
            // 使用后扣除一个道具并保存
            userData.UseTool(toolType);
        }
        else
        {
            // 数量不足，打开购买面板
            var toolPanel = UIManager.Instance.ShowPanel(PanelType.UseToolPanel) as UseToolPanel;
            if (toolPanel != null)
            {
                toolPanel.SetToolType(toolType);
            }
        }

        InitUI();
    }

    /// <summary>
    /// 直接执行道具效果（不检查数量，不扣除，只执行逻辑）
    /// </summary>
    private void ExecuteTool(ToolType toolType)
    {
        switch (toolType)
        {
            case ToolType.Remove:
                ExecuteRemoveTool();
                break;
            case ToolType.Shuffle:
                ExecuteShuffleTool();
                break;
            case ToolType.Reverse:
                ExecuteFlipTool();
                break;
        }
    }

    private void ExecuteRemoveTool()
    {
        // 获取可移除的动物列表（排除药牛、病驴等）
        List<AnimalBase> available = new List<AnimalBase>();
        AnimalBase[] all = FindObjectsOfType<AnimalBase>();
        foreach (var animal in all)
        {
            if (animal is MedicineCowItem || animal is SickDonkeyItem)
                continue;
            if (animal.MapItem == null)
                continue;
            available.Add(animal);
        }

        if (available.Count == 0)
        {
            MessageSystem.Instance.ShowTip("没有可移除的动物！");
            return;
        }

        // 开始移除模式（选择两个动物）
        SelectionModeManager.Instance.StartRemoveMode(
            maxCount: 2,
            onComplete: () => {
                // 移除完成，刷新UI
                InitUI();
            },
            onCancel: () => {
                MessageSystem.Instance.ShowTip("已取消移除操作");
            },
            filter: (animal) => {
                return !(animal is MedicineCowItem || animal is SickDonkeyItem);
            }
        );
    }

    private void ExecuteShuffleTool()
    {
        // 洗牌：随机翻转5只动物
        if (Map.Instance != null)
        {
            Map.Instance.ShuffleAnimals(5);
            MessageSystem.Instance.ShowTip("已随机翻转5只动物...");
            InitUI();  // 刷新UI（例如道具数量可能变化）
        }
    }

    private void ExecuteFlipTool()
    {
        // 翻转模式：选择一个动物并翻转其方向
        SelectionModeManager.Instance.StartFlipMode(
            onComplete: (animal) => {
                var placed = Map.Instance.GetPlacedItem(animal.MapItem.id);
                if (placed != null)
                {
                    Map.Instance.RotateAnimal180(placed);
                    MessageSystem.Instance.ShowTip("已翻转动物方向");
                }
                InitUI();
            },
            onCancel: () => {
                MessageSystem.Instance.ShowTip("已取消翻转操作");
            },
            filter: (animal) => {
                return !(animal is MedicineCowItem || animal is SickDonkeyItem);
            }
        );
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