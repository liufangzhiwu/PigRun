using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public enum ToolType
{
    Remove,     // 移除道具
    Shuffle,    // 洗牌道具
    Reverse     // 翻转道具
}

public class UseToolPanel : UIBase
{
    [SerializeField] private Text tipText;
    [SerializeField] private Button useButton;
    [SerializeField] private Button closeButton;
    
    [Header("道具类型")]
    [SerializeField] private ToolType currentToolType = ToolType.Remove;
    
    private List<AnimalBase> availableAnimals = new List<AnimalBase>();
    
    // 设置道具类型（由外部调用）
    public void SetToolType(ToolType type)
    {
        currentToolType = type;
        UpdateTipText();
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        InitUI();
        EventDispatcher.instance.TriggerUpdateLayerCoin(true, false);
        
        // 刷新可用动物列表
        RefreshAvailableAnimals();
    }
    
    protected override void InitButtonEvents()
    {
        base.InitButtonEvents();
        useButton.AddClickAction(ClickUseButton);
        closeButton.AddClickAction(ClickCloseButton);
    }
    
    private void InitUI()
    {
        UpdateTipText();
    }
    
    private void UpdateTipText()
    {
        switch (currentToolType)
        {
            case ToolType.Remove:
                tipText.text = "使用移除道具\n点击「使用」后，再点击2只动物即可移除它们";
                break;
            case ToolType.Shuffle:
                tipText.text = "使用洗牌道具\n可重新排列地图中的所有动物";
                break;
            case ToolType.Reverse:
                tipText.text = "使用翻转道具\n可翻转地图中所有动物的方向";
                break;
        }
    }
    
    /// <summary>
    /// 刷新可用动物列表（排除药牛和病驴等特殊动物）
    /// </summary>
    private void RefreshAvailableAnimals()
    {
        availableAnimals.Clear();
        
        // 获取地图中所有动物
        AnimalBase[] allAnimals = FindObjectsOfType<AnimalBase>();
        
        foreach (var animal in allAnimals)
        {
            // 排除药牛和病驴（根据需求，移除道具不能移除特殊动物）
            if (animal is MedicineCowItem || animal is SickDonkeyItem)
            {
                continue;
            }
            
            // 排除已经跑出屏幕的
            if (animal.MapItem == null)
            {
                continue;
            }
            
            availableAnimals.Add(animal);
        }
    }
    
    private void ClickUseButton()
    {
        switch (currentToolType)
        {
            case ToolType.Remove:
                StartRemoveMode();
                break;
            case ToolType.Shuffle:
                UseShuffleTool();
                break;
            case ToolType.Reverse:
                UseReverseTool();
                break;
        }
    }
    
    /// <summary>
    /// 开始移除模式（使用 SelectionModeManager）
    /// </summary>
    private void StartRemoveMode()
    {
        // 刷新可用动物列表
        RefreshAvailableAnimals();
        
        // 检查动物数量
        if (availableAnimals.Count == 0)
        {
            MessageSystem.Instance.ShowTip("没有可移除的动物！");
            Close();
            return;
        }
        
        // 关闭当前道具界面
        Close();
        
        // 使用选择模式管理器
        SelectionModeManager.Instance.StartSelectionMode(
            maxCount: 2,
            onComplete: OnRemoveComplete,
            onCancel: OnRemoveCancel,
            filter: (animal) => {
                // 筛选条件：不能是药牛和病驴
                return !(animal is MedicineCowItem || animal is SickDonkeyItem);
            }
        );
    }
    
    /// <summary>
    /// 移除完成回调
    /// </summary>
    private void OnRemoveComplete(List<AnimalBase> selectedAnimals)
    {
        // 使用 SelectionModeManager 执行移除
        SelectionModeManager.Instance.RemoveAnimals(selectedAnimals);
    }
    
    /// <summary>
    /// 移除取消回调
    /// </summary>
    private void OnRemoveCancel()
    {
        MessageSystem.Instance.ShowTip("已取消移除操作");
    }
    
    /// <summary>
    /// 使用洗牌道具
    /// </summary>
    private void UseShuffleTool()
    {
        // 关闭道具界面
        Close();
        
        // TODO: 实现洗牌功能
        MessageSystem.Instance.ShowTip("洗牌道具开发中...");
    }
    
    /// <summary>
    /// 使用翻转道具
    /// </summary>
    private void UseReverseTool()
    {
        // 关闭道具界面
        Close();
        
        // TODO: 实现翻转功能
        MessageSystem.Instance.ShowTip("翻转道具开发中...");
    }
    
    private void ClickCloseButton()
    {
        Close();
    }
    
    protected override void OnDisable()
    {
        EventDispatcher.instance.TriggerUpdateLayerCoin(true, true);
    }
}