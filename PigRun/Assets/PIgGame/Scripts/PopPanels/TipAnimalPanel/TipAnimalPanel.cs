using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PigGame
{
    
    public class TipAnimalPanel : UIBase
    {
        
        [SerializeField] private Text titleText;
        [SerializeField] private Text tipText;
        [SerializeField] private Button OkBtn;
        [SerializeField] private Button CloseBtn;


        protected override void OnEnable()
        {
            base.OnEnable();
            Init();
        }

        private void Init()
        {
            titleText.text = titleText.text.ToLower();
            string tipMessage = "受到<color=red><size=50><b>3次</b></size></color>撞击后，羊羊就会爆炸并导致关卡失败哦~\n请保护好可爱的羊羊吧！🐑💕";
            tipText.text =tipMessage;
        }
        
        protected override void InitButtonEvents()
        {
            base.InitButtonEvents();
            OkBtn.AddClickAction(ClickOkBtn);
            CloseBtn.AddClickAction(ClickCloseBtn);
        }

        private void ClickOkBtn()
        {
            UIManager.Instance.HidePanel(PanelType.TipAnimalPanel);
           
        }


        private void ClickCloseBtn()
        {
            UIManager.Instance.HidePanel(PanelType.TipAnimalPanel);
        }
        
        protected override void OnDisable() 
        {
           
        }
        
    }

}
