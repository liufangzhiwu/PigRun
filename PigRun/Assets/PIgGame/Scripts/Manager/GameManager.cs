using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    [SerializeField] private GameObject GamePanel;

    private void Awake()
    {
        instance = this;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void StartGamePanel()
    {
        MapData level1 = LevelManager.Instance.GetLevel("level1");
        GamePanel.SetActive(true);
    }
    
    public void BackHomePanel()
    {
        GamePanel.SetActive(false);
    }
}
