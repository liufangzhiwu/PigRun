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

        protected override void OnEnable()
        {
            base.OnEnable();
            AudioManager.Instance.PlaySoundEffect("win");
        }

        private void ClickLevelButton()
        {
            UIManager.Instance.HidePanel(PanelType.FinishPanel);
            
            // if(LevelManager.Instance!=null)
            //     LevelManager.Instance.LoadLevel(GameDataManager.Instance.UserData.LevelIndex);

            if (Map.Instance != null)
            {
                Map.Instance.OnLoadNewMapEvent();
            }

            GameManager.instance.StartGamePanel();

        }
    }

}
