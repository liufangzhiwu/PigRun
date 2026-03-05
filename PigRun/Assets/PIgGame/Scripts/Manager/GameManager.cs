using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    //[SerializeField] private GameObject GamePanel;

    private void Awake()
    {
        instance = this;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        UIManager.Instance.ShowPanel(PanelType.MainPanel);
        UIManager.Instance.ShowPanel(PanelType.MenuPanel);
    }

    public void StartGamePanel()
    {
        UIManager.Instance.HidePanel(PanelType.MainPanel);
        UIManager.Instance.ShowPanel(PanelType.GamePanel);
        
        LevelManager.Instance.LoadLevel(GameDataManager.Instance.UserData.LevelIndex);
        AudioManager.Instance.PlayBackgroundMusic("game-bgm"); // 播放默认音乐
        //GamePanel.SetActive(true);
    }
    
    public void BackHomePanel()
    {
        //GamePanel.SetActive(false);
    }
}
