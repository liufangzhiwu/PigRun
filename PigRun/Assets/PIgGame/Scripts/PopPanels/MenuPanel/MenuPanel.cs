using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuPanel : UIBase
{
    [SerializeField] private Text GoldText;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }
    

    protected override void OnEnable()
    {
        base.OnEnable();
        InitUI();
    }

    private void InitUI()
    {
        GoldText.text = GameDataManager.Instance.UserData.Gold.ToString();
    }

}
