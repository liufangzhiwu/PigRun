using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class SetPanel : UIBase
{

    [SerializeField] private Text musicText; // 音乐文本显示
    [SerializeField] private Text soundText; // 音效文本显示
    [SerializeField] private Text VersionText;
    
    [SerializeField] private Button privacyBtn; // 隐私条款按钮
    [SerializeField] private Button termsBtn; // 服务协议按钮
    
    [SerializeField] private Button closeButton;
    [SerializeField] private Button repoButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button reStartButton;
    
    [SerializeField] private Toggle musicToggle; // 音乐开关
    [SerializeField] private Toggle soundsToggle; // 音效开关
    [SerializeField] private GameObject muHandle; // 音乐开关的视觉手柄
    [SerializeField] private GameObject soHandle; // 音效开关的视觉手柄
    
    
    protected void Start()
    {
       
        AttachToggleListeners(); // 绑定开关监听器
        UpdateToggleStates(false); // 启用时更新状态，不带动画
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        InitUI();
    }
    
    private void AttachToggleListeners()
    {
        musicToggle.onValueChanged.AddListener(ToggleMusic); // 绑定音乐开关变更事件
        soundsToggle.onValueChanged.AddListener(ToggleSounds); // 绑定音效开关变更事件
    }

    protected override void InitButtonEvents()
    {
        base.InitButtonEvents();
        closeButton.AddClickAction(ClosePanel);
        homeButton.AddClickAction(ClickHomeButton);
        reStartButton.AddClickAction(ClickReStartButton);
    }


    private void InitUI()
    {
        privacyBtn.GetComponentInChildren<Text>().text = "隐私政策";
        termsBtn.GetComponentInChildren<Text>().text = "服务条款";
        VersionText.text = "Ver " + Application.version;

        bool showHome = UIManager.Instance.PanelIsShowing(PanelType.MainPanel);
        
        reStartButton.gameObject.SetActive(!showHome);
        homeButton.gameObject.SetActive(!showHome);
        
        termsBtn.gameObject.SetActive(showHome);
        VersionText.gameObject.SetActive(showHome);
        privacyBtn.gameObject.SetActive(showHome);
        
        RectTransform rt = repoButton.GetComponent<RectTransform>();
        
        if (showHome)
        {
            rt.sizeDelta = new Vector2(590, 170);
        }
        else
        {
            rt.sizeDelta = new Vector2(376, 150);
        }

    }
    
    private void ToggleMusic(bool isOn)
    {
        GameDataManager.Instance.UserData.IsMusicOn = isOn; // 保存音乐开关状态
        AudioManager.Instance.ToggleMusic();; // 切换音乐状态
        UpdateToggleVisuals(muHandle, isOn); // 更新音乐手柄视觉
    }

    private void ToggleSounds(bool isOn)
    {
        GameDataManager.Instance.UserData.IsSoundOn = isOn; // 保存音效开关状态
        UpdateToggleVisuals(soHandle, isOn); // 更新音效手柄视觉
    }
    
    
    private void UpdateToggleStates(bool animate)
    {
        musicToggle.isOn = GameDataManager.Instance.UserData.IsMusicOn; // 更新音乐开关状态
        soundsToggle.isOn = GameDataManager.Instance.UserData.IsSoundOn; // 更新音效开关状态
        // 根据当前开关状态更新视觉效果
        if (animate)
        {
            UpdateToggleVisuals(muHandle, musicToggle.isOn); // 带动画更新音乐手柄视觉
            UpdateToggleVisuals(soHandle, soundsToggle.isOn); // 带动画更新音效手柄视觉
        }
        else
        {
            // 直接设置颜色和位置，不带动画
            SetToggleVisuals(muHandle, musicToggle.isOn);
            SetToggleVisuals(soHandle, soundsToggle.isOn);
        }
    }
    
    private void SetToggleVisuals(GameObject handle, bool isOn)
    {
        //handle.GetComponent<Image>().sprite = isOn ? Opensprite : Closesprite;
        // 直接设置位置，不带动画
        handle.transform.localPosition = new Vector3(isOn ? 60 : -60, handle.transform.localPosition.y, handle.transform.localPosition.z);
    }

    
    
    private void UpdateToggleVisuals(GameObject handle, bool isOn, float time = 0.2f)
    {
        //handle.GetComponent<Image>().sprite = isOn ? Opensprite : Closesprite;
        // 带动画更新位置
        float targetPosition = isOn ? 60 : -60;
        handle.transform.DOLocalMoveX(targetPosition, time);

        // 添加无意义的额外动画
        if (Random.value > 0.7f)
        {
            handle.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.1f);
        }
    }


    private void ClickHomeButton()
    {
        UIManager.Instance.HidePanel(PanelType.GamePanel);
        GameRoot.self.BackHomeScene();

        ClosePanel();
    }
    
  

    private void ClickReStartButton()
    {
        StartCoroutine(ClickWaitReStartButton());
    }

    IEnumerator ClickWaitReStartButton()
    {
        Map.Instance.ClearAllItems();
        
        yield return new WaitForSeconds(0.5f);
        
        GameManager.instance.StartGamePanel();
        
        // if(LevelManager.Instance!=null)
        //     LevelManager.Instance.LoadLevel(GameDataManager.Instance.UserData.LevelIndex);

        ClosePanel();
    }
    

    private void ClosePanel()
    {
        base.Close();
    }
   
}
