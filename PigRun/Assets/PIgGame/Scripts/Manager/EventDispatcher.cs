using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 集中管理游戏内事件的发布与订阅
/// </summary>
public class EventDispatcher:MonoBehaviour
{
    public static EventDispatcher instance;
    
    #region 事件声明区域
    private Action<int, bool> _onChangeGoldUI;
    private Action<bool> _onUpdateRewardPuzzle;
    private Action<bool, bool,bool> _onUpdateLayerCoin;
    
    
    #endregion

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }    
    }

    #region 公共事件接口
    /// <summary>金币数量更新事件</summary>
    public  event Action<int, bool> OnChangeGoldUI
    {
        add => _onChangeGoldUI += value;
        remove => _onChangeGoldUI -= value;
    }

   
    /// <summary>更新奖励词语事件</summary>
    public event Action<bool> OnUpdateRewardPuzzle
    {
        add => _onUpdateRewardPuzzle += value;
        remove => _onUpdateRewardPuzzle -= value;
    }

    /// <summary>更新金币层级事件</summary>
    public event Action<bool, bool,bool> OnUpdateLayerCoin
    {
        add => _onUpdateLayerCoin += value;
        remove => _onUpdateLayerCoin -= value;
    }

    public void TriggerChangeGoldUI(int amount, bool animate)
        => _onChangeGoldUI?.Invoke(amount, animate);

    public void TriggerUpdateRewardPuzzle(bool state)
        => _onUpdateRewardPuzzle?.Invoke(state);

    public void TriggerUpdateLayerCoin(bool immediate, bool animate,bool isshowpupa=false)
        => _onUpdateLayerCoin?.Invoke(immediate, animate,isshowpupa);
  
    
    
    #endregion
}