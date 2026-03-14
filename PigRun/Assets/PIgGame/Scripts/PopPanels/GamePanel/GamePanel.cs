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
        removeButton.AddClickAction(ClickRemoveButton);
        shuffleButton.AddClickAction(ClickRemoveButton);
        reverseButton.AddClickAction(ClickRemoveButton);
    }

    private void InitUI()
    {
        LevelText.text ="关卡"+ GameDataManager.Instance.UserData.LevelIndex;
    }
    
    
    void Start()
    {
        if (Map.Instance != null)
        {
            Map.Instance.OnAllItemsDestroyed += ShowLevelComplete;
        }
    }

    void ShowLevelComplete()
    {
        GameDataManager.Instance.UserData.UpdateLevelIndex();
        UIManager.Instance.ShowPanel(PanelType.FinishPanel);
    }

    private void ClickRemoveButton()
    {
        UIManager.Instance.ShowPanel(PanelType.UseToolPanel);
    }

    void OnDestroy()
    {
        if (Map.Instance != null)
        {
            Map.Instance.OnAllItemsDestroyed -= ShowLevelComplete;
        }
    }
}
