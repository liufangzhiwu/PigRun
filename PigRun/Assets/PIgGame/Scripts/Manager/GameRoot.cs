using ThreePeakGame;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 注释：游戏管理器
/// </summary>		
public class GameRoot : MonoBehaviour
{

    public static GameRoot self;

    [SerializeField] SceneMask sceneMask;

    private void Awake()
    {
        if (self == null)
        {
            self = this;
            DontDestroyOnLoad(this);
        }      

        Application.targetFrameRate = 60;
    }

    public void EnterGameScene(int cost = -1)
    {
        self.sceneMask.gameObject.SetActive(true);
        self.sceneMask.EnterGameScene(cost);
    }

    public void BackHomeScene()
    {
        self.sceneMask.gameObject.SetActive(true);
        self.sceneMask.BackHomeScene();
    }


    void Quit()
    {
        Application.Quit();
    }
}
