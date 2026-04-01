using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private void Awake()
    {
        instance = this;
        //Application.targetFrameRate = 90;
    }
    
    void Start()
    {
        UIManager.Instance.ShowPanel(PanelType.MainPanel);
        UIManager.Instance.ShowPanel(PanelType.MenuPanel);

        // 订阅关卡加载完成事件
        LevelManager.Instance.OnLevelLoaded += OnLevelLoaded;
    }

    public void StartGamePanel()
    {
        UIManager.Instance.HidePanel(PanelType.MainPanel);
        // 先不显示 GamePanel，等加载完成后再显示
        // UIManager.Instance.ShowPanel(PanelType.GamePanel);
        
        LevelManager.Instance.LoadLevel(GameDataManager.Instance.UserData.LevelIndex);
        // 音乐在加载完成后播放，避免加载时播放导致卡顿
        // AudioManager.Instance.PlayBackgroundMusic("game-bgm");
    }
    
    private void OnLevelLoaded()
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
        if (LevelManager.Instance != null)
            LevelManager.Instance.OnLevelLoaded -= OnLevelLoaded;
    }
}