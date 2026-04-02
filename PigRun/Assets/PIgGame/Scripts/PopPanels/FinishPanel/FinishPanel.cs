using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PigGame
{
    
    public class FinishPanel : UIBase
    {
        
        [SerializeField] private Button LevelButton;
        

        
        protected override void InitButtonEvents()
        {
            base.InitButtonEvents();
            LevelButton.AddClickAction(ClickLevelButton);
        }

        private void ClickLevelButton()
        {
            UIManager.Instance.HidePanel(PanelType.FinishPanel);
            
            if(LevelManager.Instance!=null)
                LevelManager.Instance.LoadLevel(GameDataManager.Instance.UserData.LevelIndex);

            if (Map.Instance != null)
            {
                Map.Instance.OnLoadNewMapEvent();
            }
           
        }
    }

}
