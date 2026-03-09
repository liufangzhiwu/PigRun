using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePanel : UIBase
{
    
    void Start()
    {
        if (Map.Instance != null)
        {
            //Map.Instance.OnAllItemsDestroyed += ShowLevelComplete;
        }
    }

    void ShowLevelComplete()
    {
        //GameDataManager.Instance.UserData.UpdateLevelIndex();
       UIManager.Instance.ShowPanel(PanelType.FinishPanel);
    }

    void OnDestroy()
    {
        if (Map.Instance != null)
        {
            //Map.Instance.OnAllItemsDestroyed -= ShowLevelComplete;
        }
    }
}
