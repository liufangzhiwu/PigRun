using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// UI系统管理器 - 负责所有UI面板的加载、显示、隐藏和层级管理
/// </summary>
public class UIManager : MonoBehaviour
{
    #region 枚举和事件定义
    
    /// <summary>
    /// 面板状态枚举
    /// </summary>
    public enum PanelState
    {
        Null,   // 空状态
        Show,   // 显示状态
        Hide    // 隐藏状态
    }

    /// <summary>
    /// UI面板事件委托
    /// </summary>
    public delegate void PanelSystemEventHandler(object sender, PanelEventArgs args);
    
    /// <summary>
    /// UI面板事件参数类
    /// </summary>
    public class PanelEventArgs : EventArgs
    {
        public string PanelName;    // 面板名称
        public PanelState State;    // 面板状态
        public string PanelType;    // 面板类型
    }

    #endregion

    #region 单例实现

    public static UIManager Instance;
  
    #endregion

    #region 成员变量

    private Dictionary<string, UIBase> _loadedPanels = new Dictionary<string, UIBase>(); // 已加载面板字典
    private List<string> _pendingShowPanels = new List<string>(); // 等待显示的面板队列
    private GamePanels _panelConfig; // UI配置数据
    private Transform _uiRoot; // UI根节点

    public event PanelSystemEventHandler PanelEvent; // UI面板事件

    #endregion

    #region 相机自适应配置

    [Header("Camera Adaptation (by Aspect Ratio)")]
    [SerializeField] private bool enableCameraAdaptation = true;
    [Tooltip("宽高比阈值：大于等于此值视为宽屏，使用低参数；否则视为窄屏，使用高参数")]
    [SerializeField] private float aspectRatioThreshold = 0.5f;
    [Tooltip("窄屏（竖屏/iPad）使用的参数（原2688对应）")]
    [SerializeField] private float orthoSizeNarrow = 6.5f;
    [SerializeField] private float cameraZNarrow = -1.34f;
    [Tooltip("宽屏（横屏/全面屏）使用的参数（原2208对应）")]
    [SerializeField] private float orthoSizeWide = 5.2f;
    [SerializeField] private float cameraZWide = -1.85f;

    private Camera _mainCamera;

    #endregion

    #region 生命周期

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetPanelRootOnLoadScene();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void SetPanelRootOnLoadScene()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        var all = FindObjectsOfType<Canvas>();
        foreach (var item in all)
        {
            if (item.name == "Canvas")
            {
                _uiRoot = item.transform;
                break;
            }
        }

        // 场景加载后重新获取主相机并应用自适应
        _mainCamera = null;
        ApplyCameraAdaptation();
    }

    public void ClearUIBase()
    {
        _loadedPanels.Clear();
    }

    void OnSceneUnloaded(Scene arg0)
    {
        _loadedPanels.Clear();
    }

    private void Start()
    {
        InitializePanelEvents();
        LoadPanelConfiguration();
        ApplyCameraAdaptation();              // 首次应用相机自适应
        //StartCoroutine(MonitorResolutionChange()); // 监听分辨率变化
    }

    private void OnEnable()
    {
        // 确保启用了自适应时开始监听
        if (enableCameraAdaptation)
        {
            //StartCoroutine(MonitorResolutionChange());
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 显示指定面板
    /// </summary>
    public UIBase ShowPanel(string panelName)
    {
        if (string.IsNullOrEmpty(panelName)) return null;
        
        LoadPanelConfiguration();

        UIBase panel;
        

        if (_loadedPanels.TryGetValue(panelName, out panel))
        {
            panel.gameObject.SetActive(true);
        }
        else
        {
            panel = LoadAndInstantiatePanel(panelName);
            if (panel == null) return null;
            
            _loadedPanels.Add(panelName, panel);
            InitializePanelInfo(panel, panelName);
        }
       
        RaisePanelEvent(panel, PanelState.Show);
        return panel;
    }

    /// <summary>
    /// 隐藏指定面板
    /// </summary>
    public void HidePanel(string panelName, bool useAnimation = true, UnityAction onComplete = null)
    {
        if (!_loadedPanels.ContainsKey(panelName)) return;

        UIBase panel = _loadedPanels[panelName];
        
        if (onComplete != null)
            panel.AddCloseListener(onComplete);

        if (useAnimation)
            panel.Close();
        else
            panel.OnHideAnimationEnd();
    }

    /// <summary>
    /// 检查指定类型面板是否正在显示
    /// </summary>
    public bool IsPanelTypeShowing(string excludePanel = "")
    {
        foreach (var panel in _loadedPanels.Values)
        {
            if (!string.IsNullOrEmpty(excludePanel) && panel.WindowName == excludePanel) 
                continue;
                
            if (panel.IsWindowVisible && panel.WindowCategory == UIPanelLayer.UpPopPanel)
                return true;
            
            if (panel.IsWindowVisible && panel.WindowCategory == UIPanelLayer.PopPanel)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 检查指定面板是否正在显示
    /// </summary>
    public bool PanelIsShowing(string panelName)
    {
        return _loadedPanels.ContainsKey(panelName) && 
               _loadedPanels[panelName].IsWindowVisible;
    }
    
    
    /// <summary>
    /// 获取指定的面板
    /// </summary>
    public UIBase GetPanel(string panelName)
    {
        UIBase panel;
        if (!_loadedPanels.TryGetValue(panelName, out panel))
        {
            panel = LoadAndInstantiatePanel(panelName);
            if (panel == null) return null;
            
            _loadedPanels.Add(panelName, panel);
            InitializePanelInfo(panel, panelName);
        }
        return panel;
    }

    #endregion

    #region 私有方法

    private void LoadPanelConfiguration()
    {
        if (_panelConfig == null)
        {
            _panelConfig = AssetBundleLoader.SharedInstance
                .LoadScriptableObject("objects", "GamePanels") as GamePanels;
        }
    }

    private void InitializePanelEvents()
    {
        PanelEvent += (sender, args) =>
        {
            if (args.PanelType == UIPanelLayer.PopPanel.ToString() && 
                args.State == PanelState.Hide)
            {
                HandlePopupPanelHidden(sender as UIBase);
            }
        };
    }

    private void HandlePopupPanelHidden(UIBase closedPanel)
    {
        var visiblePopups = new List<UIBase>();
        
        foreach (var panel in _loadedPanels.Values)
        {
            if (panel.IsWindowVisible&& 
                panel.WindowCategory == UIPanelLayer.PopPanel && 
                panel != closedPanel)
            {
                visiblePopups.Add(panel);
            }
        }

        float delay = 0.1f;
        DOTween.To(() => delay, x => delay = x, 0, 1f).OnComplete(() =>
        {
            if (visiblePopups.Count == 0 && _pendingShowPanels.Count > 0)
            {
                ShowPanel(_pendingShowPanels[0]);
                _pendingShowPanels.RemoveAt(0);
            }
        });
    }

    private UIBase LoadAndInstantiatePanel(string panelName)
    {
        var panelData = _panelConfig.GetViewsData(panelName);
        if (panelData.prefab == null)
        {
            AssetBundleLoader.SharedInstance.LoadAtlas(
                panelData.spriteAtlasName.ToLower(), 
                panelName);
                
            panelData.prefab = AssetBundleLoader.SharedInstance.LoadGameObject(
                panelName.ToLower(), 
                panelName);
        }

        if (panelData.prefab == null)
        {
            Debug.LogError($"Failed to load panel: {panelName}");
            return null;
        }
        
        GameObject panelObj = Instantiate(panelData.prefab, _uiRoot);
        return panelObj.GetComponent<UIBase>();
    }

    private void InitializePanelInfo(UIBase panel, string panelName)
    {
        if (string.IsNullOrEmpty(panel.WindowName))
            panel.SetWindowName(panelName);
            
        if (panel.WindowCategory == null)
        {
            var panelData = _panelConfig.GetViewsData(panelName);
            panel.SetWindowCategory(panelData.panelLayer);
        }
    }

    private void RaisePanelEvent(UIBase panel, PanelState state)
    {
        PanelEvent?.Invoke(panel, new PanelEventArgs
        {
            PanelName = panel.WindowName,
            State = state,
            PanelType = panel.WindowCategory.ToString()
        });
    }
  
    #endregion

  

    #region 相机自适应逻辑

    /// <summary>
    /// 根据屏幕宽高比调整主相机参数：宽屏用2208参数，窄屏用2688参数
    /// </summary>
    private void ApplyCameraAdaptation()
    {
        if (!enableCameraAdaptation) return;
    
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogWarning("UIManager: 未找到主相机，无法应用自适应逻辑");
                return;
            }
        }
    
        if (!_mainCamera.orthographic)
        {
            Debug.LogWarning("UIManager: 主相机不是正交相机，无法设置 orthographicSize");
            return;
        }
    
        float currentAspect = (float)Screen.width / Screen.height;
        float targetSize;
        float targetZ;
        bool isWide = currentAspect >= aspectRatioThreshold;
    
        if (isWide)
        {
            targetSize = orthoSizeWide;
            targetZ = cameraZWide;
            Debug.Log($"相机自适应: 宽屏模式, 宽高比={currentAspect:F2} >= {aspectRatioThreshold}, Size={targetSize:F2}, Z={targetZ:F2}");
        }
        else
        {
            targetSize = orthoSizeNarrow;
            targetZ = cameraZNarrow;
            Debug.Log($"相机自适应: 窄屏模式, 宽高比={currentAspect:F2} < {aspectRatioThreshold}, Size={targetSize:F2}, Z={targetZ:F2}");
        }
    
        _mainCamera.orthographicSize = targetSize;
        Vector3 pos = _mainCamera.transform.localPosition;
        pos.z = targetZ;
        _mainCamera.transform.localPosition = pos;
    }

    /// <summary>
    /// 监听分辨率变化（适用于窗口大小改变或屏幕旋转）
    /// </summary>
    // private IEnumerator MonitorResolutionChange()
    // {
    //     while (enableCameraAdaptation)
    //     {
    //         ApplyCameraAdaptation();
    //         yield return new WaitForSeconds(0.2f);
    //     }
    // }

    #endregion

    
}