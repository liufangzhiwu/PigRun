using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PigGame
{
    
    public class MainPanel : UIBase
    {
        
        [SerializeField] private Button LevelButton;
        
        // Start is called before the first frame update
        void Start()
        {
            LevelButton.onClick.AddListener(ClickLevelButton);
        }

        private void ClickLevelButton()
        {
            GameManager.instance.StartGamePanel();
        }
    }

}
