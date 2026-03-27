using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuPanel : UIBase
{
    [SerializeField] private Text GoldText;
    [SerializeField] private Button gmButton;
    [SerializeField] private Button setButton;
    [SerializeField] private Button ShopBtn;


    protected override void InitButtonEvents()
    {
        base.InitButtonEvents();
        gmButton.AddClickAction(ClickGMButton);
        setButton.AddClickAction(ClickSetButton);
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        InitUI();
        
        EventDispatcher.instance.OnUpdateLayerCoin += UpdateCoinLayer;
      
    }

    
    private void InitUI(int value=0,bool isanim=false)
    {
        if(value>0&&isanim)
        {
            StartCoroutine(AnimateCoinAddition(value));
        }
        else
        {
            GoldText.text = GameDataManager.Instance.UserData.Gold.ToString();
        }
        
        // redpoint.SetActive(!GameDataManager.Instance.UserData.isHideShopRedPoint);
        // sale.SetActive(!GameDataManager.Instance.UserData.isHideShopRedPoint);
    }
    
    private IEnumerator AnimateCoinAddition(int amount)
    {
        int startValue = GameDataManager.Instance.UserData.Gold-amount;
        int targetValue = GameDataManager.Instance.UserData.Gold;
        float duration = 0.2f; // 动画持续时间
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration); // 归一化
            int currentValue = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, t));
            GoldText.text = currentValue.ToString();
            yield return null;
        }
        GoldText.text = targetValue.ToString(); // 确保最终值正确显示
    }
    
    private void ClickSetButton()
    {
        UIManager.Instance.ShowPanel(PanelType.SetPanel);
    }

    private void ClickGMButton()
    {
        UIManager.Instance.ShowPanel(PanelType.DebugPanel);
    }
    
    /// <summary>
    /// 更改金币显示层级
    /// </summary>
    private void UpdateCoinLayer(bool istop,bool isshopbtnEnable=true,bool isshowPupa=false)
    {
        GameObject coinObj = ShopBtn.gameObject;
        Canvas canvas= coinObj.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas= coinObj.AddComponent<Canvas>();
            coinObj.AddComponent<GraphicRaycaster>();
        }
        
           
        if (istop)
        {
            coinObj.gameObject.SetActive(true);
            canvas.overrideSorting=true;
            canvas.sortingLayerName="RewardPanel";
            canvas.sortingOrder=100;
        }
        else
        {
            coinObj.gameObject.SetActive(false);
            canvas.overrideSorting=true;
            canvas.sortingLayerName="TopPanel";
            canvas.sortingOrder=0;
            
        }
   
        ShopBtn.enabled = isshopbtnEnable;
       
    }
    
    protected override void OnDisable()
    {
        base.OnDisable();
        EventDispatcher.instance.OnUpdateLayerCoin -= UpdateCoinLayer;
        EventDispatcher.instance.OnChangeGoldUI -= InitUI;
        
    }

}
