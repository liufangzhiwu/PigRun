using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugPanel : UIBase
{
    [SerializeField] private Button jumpLevelButton;
    [SerializeField] private Button closeButton;
    
    
    protected override void OnEnable()
    {
        base.OnEnable();
        InitUI();
        InitUIData();
    }

    protected override void InitButtonEvents()
    {
        base.InitButtonEvents();
        jumpLevelButton.AddClickAction(ClickjumpLevelButton);
        closeButton.AddClickAction(ClickCloseButton);
    }


    private void InitUI()
    {
        
    }
    
    private void InitUIData()
    {
        InitBtnData(jumpLevelButton,GameDataManager.Instance.UserData.LevelIndex.ToString());
        
    }
    
    private void InitBtnData(Button button, string count)
    {
        InputField Stagenumtxt = button.GetComponentInChildren<InputField>();
        string value = Stagenumtxt.text;
        if (string.IsNullOrEmpty(value))
        {
            Stagenumtxt.text = count;
        }
    }

    private void ClickjumpLevelButton()
    {
        InputField Stagenumtxt = jumpLevelButton.GetComponentInChildren<InputField>();
        int Stagenum = int.Parse(Stagenumtxt.text);
        
        if (Stagenum < 1)
        {
            Debug.LogError("关卡序号无效！");
            return;
        }
        
        //设置关卡数据 向前跳转关卡后，进度需要跟关卡同步；向后跳关不需要同步
        //if (Stagenum > GameDataManager.Instance.UserData.LevelIndex)
        {
            GameDataManager.Instance.UserData.UpdateLevelIndex(Stagenum,true);
        }

        GameManager.instance.StartGamePanel();

        ClickCloseButton();
    }

    private void ClickCloseButton()
    {
        base.Close();
    }
   
}
