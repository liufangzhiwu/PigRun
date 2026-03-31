using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

/// <summary>
/// 选择模式管理器（单例）
/// </summary>
public class SelectionModeManager : MonoBehaviour
{
    public static SelectionModeManager Instance;
    
    public GameObject mask;
    
    public bool IsInSelectionMode { get; private set; } = false;
    public int MaxSelectCount { get; private set; } = 2;
    public int CurrentSelectCount => selectedAnimals.Count;
    
    private List<AnimalBase> selectedAnimals = new List<AnimalBase>();
    private List<AnimalBase> availableAnimals = new List<AnimalBase>();
    private Action<List<AnimalBase>> onSelectionComplete;
    private Action onSelectionCancel;
    private Func<AnimalBase, bool> selectionFilter;
    
    private Dictionary<AnimalBase, Color> originalColors = new Dictionary<AnimalBase, Color>();
    private Dictionary<AnimalBase, GameObject> selectedHalos = new Dictionary<AnimalBase, GameObject>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            mask.gameObject.SetActive(false);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 开始选择模式
    /// </summary>
    /// <param name="maxCount">最大选择数量</param>
    /// <param name="onComplete">选择完成回调</param>
    /// <param name="onCancel">取消回调</param>
    /// <param name="filter">可选：筛选条件</param>
    public void StartSelectionMode(int maxCount, Action<List<AnimalBase>> onComplete, Action onCancel = null, Func<AnimalBase, bool> filter = null)
    {
        if (IsInSelectionMode)
        {
            Debug.LogWarning("已经在选择模式中，请先退出");
            return;
        }
        mask.gameObject.SetActive(true);
        IsInSelectionMode = true;
        MaxSelectCount = maxCount;
        onSelectionComplete = onComplete;
        onSelectionCancel = onCancel;
        selectionFilter = filter;
        selectedAnimals.Clear();
        
        // 刷新可用动物列表
        RefreshAvailableAnimals();
        
        if (availableAnimals.Count == 0)
        {
            MessageSystem.Instance.ShowTip("没有可选择的动物！");
            ExitSelectionMode();
            return;
        }
        
        // 注册动物点击事件
        RegisterAnimalClickEvents(true);
        
        MessageSystem.Instance.ShowTip($"请选择 {maxCount} 只动物");
    }
    
    /// <summary>
    /// 刷新可用动物列表
    /// </summary>
    private void RefreshAvailableAnimals()
    {
        availableAnimals.Clear();
        
        AnimalBase[] allAnimals = FindObjectsOfType<AnimalBase>();
        
        foreach (var animal in allAnimals)
        {
            // 应用筛选条件
            if (selectionFilter != null && !selectionFilter(animal))
            {
                continue;
            }
            
            // 排除已经跑出屏幕的
            if (animal.MapItem == null)
            {
                continue;
            }
            
            availableAnimals.Add(animal);
        }
    }
    
    /// <summary>
    /// 注册/取消注册动物点击事件
    /// </summary>
    private void RegisterAnimalClickEvents(bool register)
    {
        foreach (var animal in availableAnimals)
        {
            if (animal != null)
            {
                if (register)
                {
                    animal.OnAnimalClicked += OnAnimalSelected;
                }
                else
                {
                    animal.OnAnimalClicked -= OnAnimalSelected;
                }
            }
        }
    }
    
    /// <summary>
    /// 动物被选中
    /// </summary>
    private void OnAnimalSelected(AnimalBase animal)
    {
        if (!IsInSelectionMode) return;
        
        if (!availableAnimals.Contains(animal))
        {
            MessageSystem.Instance.ShowTip("这个动物不能被选择！");
            return;
        }
        
        if (selectedAnimals.Contains(animal))
        {
            // 取消选中
            selectedAnimals.Remove(animal);
            RemoveSelectedEffect(animal);
            MessageSystem.Instance.ShowTip($"已取消选中 ({selectedAnimals.Count}/{MaxSelectCount})");
        }
        else
        {
            if (selectedAnimals.Count >= MaxSelectCount)
            {
                MessageSystem.Instance.ShowTip($"最多只能选择 {MaxSelectCount} 只动物！");
                return;
            }
            
            selectedAnimals.Add(animal);
            AddSelectedEffect(animal);
            MessageSystem.Instance.ShowTip($"已选中 ({selectedAnimals.Count}/{MaxSelectCount})");
        }
        
        if (selectedAnimals.Count >= MaxSelectCount)
        {
            CompleteSelection();
        }
    }
    
    /// <summary>
    /// 完成选择
    /// </summary>
    private void CompleteSelection()
    {
        if (!IsInSelectionMode) return;
        
        // 清除选中效果
        foreach (var animal in selectedAnimals)
        {
            RemoveSelectedEffect(animal);
        }
        
        // 取消注册事件
        RegisterAnimalClickEvents(false);
        
        // 执行回调
        onSelectionComplete?.Invoke(new List<AnimalBase>(selectedAnimals));
        
        // 退出选择模式
        ExitSelectionMode();
    }
    
    /// <summary>
    /// 退出选择模式（取消）
    /// </summary>
    public void ExitSelectionMode()
    {
        if (!IsInSelectionMode) return;
        
        mask.gameObject.SetActive(false);
        // 取消注册事件
        RegisterAnimalClickEvents(false);
        
        // 清除所有选中效果
        foreach (var animal in selectedAnimals)
        {
            RemoveSelectedEffect(animal);
        }
        
        selectedAnimals.Clear();
        originalColors.Clear();
        selectedHalos.Clear();
        
        IsInSelectionMode = false;
        
        // 执行取消回调
        onSelectionCancel?.Invoke();
        
        onSelectionComplete = null;
        onSelectionCancel = null;
        selectionFilter = null;
    }
    
    private void AddSelectedEffect(AnimalBase animal)
    {
        // 高亮效果
        var renderer = animal.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (!originalColors.ContainsKey(animal))
            {
                originalColors[animal] = renderer.material.color;
            }
            renderer.material.color = Color.yellow;
        }
        
        // 选中光环
        GameObject halo = new GameObject("SelectedHalo");
        halo.transform.SetParent(animal.transform);
        halo.transform.localPosition = Vector3.up * 0.5f;
        
        var textMesh = halo.AddComponent<TextMesh>();
        textMesh.text = "✓";
        textMesh.color = Color.yellow;
        textMesh.fontSize = 30;
        textMesh.characterSize = 0.05f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        halo.AddComponent<Billboard>();
        selectedHalos[animal] = halo;
    }
    
    private void RemoveSelectedEffect(AnimalBase animal)
    {
        if (originalColors.ContainsKey(animal))
        {
            var renderer = animal.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = originalColors[animal];
            }
            originalColors.Remove(animal);
        }
        
        if (selectedHalos.ContainsKey(animal))
        {
            Destroy(selectedHalos[animal]);
            selectedHalos.Remove(animal);
        }
    }
    
    /// <summary>
    /// 移除动物（带特效）
    /// </summary>
    public void RemoveAnimals(List<AnimalBase> animalsToRemove)
    {
        if (animalsToRemove == null || animalsToRemove.Count == 0) return;
        
        StartCoroutine(RemoveAnimalsCoroutine(animalsToRemove));
    }
    
    private IEnumerator RemoveAnimalsCoroutine(List<AnimalBase> animalsToRemove)
    {
        int removedCount = 0;
        
        foreach (var animal in animalsToRemove)
        {
            if (animal != null && animal.gameObject != null)
            {
                // 显示移除特效
                ShowRemoveEffect(animal.transform.position);
                
                // 播放音效
                //AudioManager.Instance.PlaySoundEffect("tool_remove");
                
                // 延迟一下，让特效更明显
                yield return new WaitForSeconds(0.2f);
                
                // 从地图移除动物
                if (animal.MapItem != null)
                {
                    Map.Instance.RemoveItem(animal.MapItem);
                }
                else
                {
                    Destroy(animal.gameObject);
                }
                
                removedCount++;
            }
        }
        
        // 显示成功提示
        if (removedCount > 0)
        {
            MessageSystem.Instance.ShowTip($"成功移除了 {removedCount} 个动物！");
        }
    }
    
    private void ShowRemoveEffect(Vector3 position)
    {
        // 创建特效文字
        GameObject effectObj = new GameObject("RemoveEffect");
        effectObj.transform.position = position + Vector3.up * 0.5f;
        
        var textMesh = effectObj.AddComponent<TextMesh>();
        textMesh.text = "✨ 移除 ✨";
        textMesh.color = Color.cyan;
        textMesh.fontSize = 35;
        textMesh.characterSize = 0.05f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        // 添加Billboard效果
        effectObj.AddComponent<Billboard>();
        
        // 淡出并销毁
        StartCoroutine(FadeAndDestroy(effectObj, 0.8f));
        
        // 创建粒子特效
        StartCoroutine(CreateRemoveParticles(position));
    }
    
    private IEnumerator CreateRemoveParticles(Vector3 position)
    {
        for (int i = 0; i < 15; i++)
        {
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particle.transform.localScale = Vector3.one * 0.08f;
            particle.transform.position = position + Random.insideUnitSphere * 0.8f;
            
            var renderer = particle.GetComponent<Renderer>();
            renderer.material.color = Color.cyan;
            
            Destroy(particle, 0.5f);
            yield return new WaitForSeconds(0.02f);
        }
    }
    
    private IEnumerator FadeAndDestroy(GameObject obj, float duration)
    {
        float elapsed = 0;
        TextMesh textMesh = obj.GetComponent<TextMesh>();
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (textMesh != null)
            {
                Color color = textMesh.color;
                color.a = 1 - (elapsed / duration);
                textMesh.color = color;
            }
            obj.transform.Translate(Vector3.up * Time.deltaTime * 0.5f);
            yield return null;
        }
        
        Destroy(obj);
    }
}