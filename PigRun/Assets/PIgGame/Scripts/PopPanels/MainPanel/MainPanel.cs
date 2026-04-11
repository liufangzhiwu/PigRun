using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PigGame
{
    
    public class MainPanel : UIBase
    {
        
        [SerializeField] private Button LevelButton;
        [SerializeField] private Text LevetTest;
        
        // Start is called before the first frame update
        void Start()
        {
            LevelButton.AddClickAction(ClickLevelButton);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            LevetTest.text=$"第{GameDataManager.Instance.UserData.LevelIndex}关";
        }

        private void ClickLevelButton()
        {
            GameManager.instance.StartGamePanel();
        }
    }

}
