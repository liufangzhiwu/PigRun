using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetPanel : UIBase
{

    [SerializeField] private Text tipText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button reStartButton;
    
    
    protected override void OnEnable()
    {
        base.OnEnable();
        InitUI();
    }

    protected override void InitButtonEvents()
    {
        base.InitButtonEvents();
        closeButton.AddClickAction(ClosePanel);
        homeButton.AddClickAction(ClickHomeButton);
        reStartButton.AddClickAction(ClickReStartButton);
    }


    private void InitUI()
    {
        
    }

    private void ClickHomeButton()
    {
        UIManager.Instance.HidePanel(PanelType.GamePanel);
        GameRoot.self.BackHomeScene();

        ClosePanel();
    }
    
  

    private void ClickReStartButton()
    {
        StartCoroutine(ClickWaitReStartButton());
    }

    IEnumerator ClickWaitReStartButton()
    {
        Map.Instance.ClearAllItems();
        
        yield return new WaitForSeconds(0.5f);
        
        //GameManager.instance.StartGamePanel();
        
        if(LevelManager.Instance!=null)
            LevelManager.Instance.LoadLevel(GameDataManager.Instance.UserData.LevelIndex);

        ClosePanel();
    }
    

    private void ClosePanel()
    {
        base.Close();
    }
   
}
