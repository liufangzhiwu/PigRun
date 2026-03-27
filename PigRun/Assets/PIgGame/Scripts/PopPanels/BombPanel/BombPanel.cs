using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PigGame
{
    
    public class BombPanel : UIBase
    {
        
        [SerializeField] private Button ContinueBtn;
        [SerializeField] private Button CloseBtn;


        protected override void OnEnable()
        {
            EventDispatcher.instance.TriggerUpdateLayerCoin(true,false);
        }
        
        protected override void InitButtonEvents()
        {
            base.InitButtonEvents();
            ContinueBtn.AddClickAction(ClickContinueBtn);
            CloseBtn.AddClickAction(ClickCloseBtn);
        }

        private void ClickContinueBtn()
        {
            UIManager.Instance.HidePanel(PanelType.BombPanel);
            GameManager.instance.StartGamePanel();

            if (Map.Instance != null)
            {
                Map.Instance.OnLoadNewMapEvent();
            }
        }


        private void ClickCloseBtn()
        {
            UIManager.Instance.HidePanel(PanelType.BombPanel);
            UIManager.Instance.ShowPanel(PanelType.FailPanel);
        }
        
        protected override void OnDisable() 
        {
            EventDispatcher.instance.TriggerUpdateLayerCoin(true,true);
        }
        
    }

}
