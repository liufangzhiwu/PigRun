using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PigGame
{
    
    public class FailPanel : UIBase
    {
        
        [SerializeField] private Button RestartButton;
        
        protected override void OnEnable()
        {
            EventDispatcher.instance.TriggerUpdateLayerCoin(true,false);
        }
        
        protected override void InitButtonEvents()
        {
            base.InitButtonEvents();
            RestartButton.AddClickAction(ClickRestartButton);
        }

        private void ClickRestartButton()
        {
            UIManager.Instance.HidePanel(PanelType.FailPanel);
            GameManager.instance.StartGamePanel();

            if (Map.Instance != null)
            {
                Map.Instance.OnLoadNewMapEvent();
            }
           
        }
        
        protected override void OnDisable() 
        {
            EventDispatcher.instance.TriggerUpdateLayerCoin(true,true);
        }
    }

}
