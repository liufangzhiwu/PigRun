using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UseToolPanel : UIBase
{

    [SerializeField] private Text tipText;
    [SerializeField] private Button useButton;
    [SerializeField] private Button closeButton;
    
    
    protected override void OnEnable()
    {
        base.OnEnable();
        InitUI();
        EventDispatcher.instance.TriggerUpdateLayerCoin(true,false);
    }

    protected override void InitButtonEvents()
    {
        base.InitButtonEvents();
        useButton.AddClickAction(ClickUseButton);
        closeButton.AddClickAction(ClickCloseButton);
    }


    private void InitUI()
    {
        
    }

    private void ClickUseButton()
    {
        
    }

    private void ClickCloseButton()
    {
        base.Close();
    }
    
    protected override void OnDisable() 
    {
        EventDispatcher.instance.TriggerUpdateLayerCoin(true,true);
    }
   
}
