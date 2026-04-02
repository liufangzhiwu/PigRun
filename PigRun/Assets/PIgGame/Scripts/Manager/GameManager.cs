using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    
    // 加载完成事件
    public event System.Action OnLevelLoaded;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }    
    }
    
    void OnEnable()
    {
        // 订阅关卡加载完成事件
        OnLevelLoaded += OnLevelLoadedEvent;

        StartCoroutine(ShowMainPanel());
    }

    IEnumerator ShowMainPanel()
    {
        yield return new WaitForSeconds(0.001f);
        UIManager.Instance.ShowPanel(PanelType.MainPanel);
        UIManager.Instance.ShowPanel(PanelType.MenuPanel);
    }

    public void OverLevelLoadedEvent()
    {
        OnLevelLoaded?.Invoke();
    }
    
    
    public void StartGamePanel()
    {
        //UIManager.Instance.HidePanel(PanelType.MainPanel);
        UIManager.Instance.HidePanel(PanelType.MenuPanel);

        GameRoot.self.EnterGameScene();
        // 先不显示 GamePanel，等加载完成后再显示
        // UIManager.Instance.ShowPanel(PanelType.GamePanel);
        // 音乐在加载完成后播放，避免加载时播放导致卡顿
        // AudioManager.Instance.PlayBackgroundMusic("game-bgm");
    }
    
    private void OnLevelLoadedEvent()
    {
        UIManager.Instance.ShowPanel(PanelType.GamePanel);
        AudioManager.Instance.PlayBackgroundMusic("game-bgm");
    }

    public void BackHomePanel()
    {
        // 可以在这里做清理工作
    }

    private void OnDestroy()
    {
          OnLevelLoaded -= OnLevelLoadedEvent;
    }
}