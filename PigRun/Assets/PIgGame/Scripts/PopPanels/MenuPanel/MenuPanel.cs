using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuPanel : UIBase
{
    [SerializeField] private Text GoldText;
    [SerializeField] private Button gmButton;
    [SerializeField] private Button setButton;


    protected override void InitButtonEvents()
    {
        base.InitButtonEvents();
        gmButton.AddClickAction(ClickGMButton);
        setButton.AddClickAction(ClickSetButton);
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        InitUI();
    }

    private void InitUI()
    {
        GoldText.text = GameDataManager.Instance.UserData.Gold.ToString();
    }
    
    private void ClickSetButton()
    {
        UIManager.Instance.ShowPanel(PanelType.SetPanel);
    }

    private void ClickGMButton()
    {
        UIManager.Instance.ShowPanel(PanelType.DebugPanel);
    }

}
