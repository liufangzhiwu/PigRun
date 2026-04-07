using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 选择模式管理器（单例）
/// 支持两种模式：
/// 1. 移除模式：连续选择指定数量的动物并立即移除
/// 2. 翻转模式：选择一只动物并执行翻转回调（不删除）
/// </summary>
public class SelectionModeManager : MonoBehaviour
{
    public static SelectionModeManager Instance;

    [Header("UI")]
    public GameObject mask;                 // 半透明遮罩，选择模式时显示

    // 状态
    public bool IsInSelectionMode { get; private set; } = false;
    public int MaxSelectCount { get; private set; } = 2;
    public int CurrentSelectCount => removedCount;

    // 移除模式专用
    private int removedCount = 0;
    private Action onSelectionComplete;     // 移除完成回调（无参）
    private Action onSelectionCancel;       // 取消回调

    // 翻转模式专用
    private Action<AnimalBase> onFlipComplete;  // 翻转完成回调，返回选中的动物

    // 通用
    private List<AnimalBase> availableAnimals = new List<AnimalBase>();
    private Func<AnimalBase, bool> selectionFilter;

    // 视觉效果
    private Dictionary<AnimalBase, Color> originalColors = new Dictionary<AnimalBase, Color>();
    private Dictionary<AnimalBase, GameObject> selectedHalos = new Dictionary<AnimalBase, GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (mask != null) mask.gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ==================== 移除模式 ====================
    /// <summary>
    /// 开始移除模式（连续选择并移除指定数量的动物）
    /// </summary>
    /// <param name="maxCount">需要移除的数量</param>
    /// <param name="onComplete">全部移除完成回调</param>
    /// <param name="onCancel">取消回调</param>
    /// <param name="filter">可选筛选条件</param>
    public void StartRemoveMode(int maxCount, Action onComplete, Action onCancel = null, Func<AnimalBase, bool> filter = null)
    {
        if (IsInSelectionMode)
        {
            Debug.LogWarning("已经在选择模式中，请先退出");
            return;
        }

        InitializeMode(maxCount, filter);
        onSelectionComplete = onComplete;
        onSelectionCancel = onCancel;

        if (availableAnimals.Count == 0)
        {
            MessageSystem.Instance.ShowTip("没有可移除的动物！");
            ExitSelectionMode();
            return;
        }

        RegisterAnimalClickEvents(true);
        MessageSystem.Instance.ShowTip($"请选择第 1 只动物（共 {maxCount} 只）");
    }

    // ==================== 翻转模式 ====================
    /// <summary>
    /// 开始翻转模式（选择一只动物并执行翻转）
    /// </summary>
    /// <param name="onComplete">选择完成回调，参数为选中的动物</param>
    /// <param name="onCancel">取消回调</param>
    /// <param name="filter">可选筛选条件</param>
    public void StartFlipMode(Action<AnimalBase> onComplete, Action onCancel = null, Func<AnimalBase, bool> filter = null)
    {
        if (IsInSelectionMode)
        {
            Debug.LogWarning("已经在选择模式中，请先退出");
            return;
        }

        InitializeMode(1, filter);
        onFlipComplete = onComplete;
        onSelectionCancel = onCancel;

        if (availableAnimals.Count == 0)
        {
            MessageSystem.Instance.ShowTip("没有可翻转的动物！");
            ExitSelectionMode();
            return;
        }

        RegisterAnimalClickEvents(true);
        MessageSystem.Instance.ShowTip("请选择一只动物进行翻转");
    }

    // ==================== 通用初始化 ====================
    private void InitializeMode(int maxCount, Func<AnimalBase, bool> filter)
    {
        if (mask != null) mask.gameObject.SetActive(true);
        IsInSelectionMode = true;
        MaxSelectCount = maxCount;
        removedCount = 0;
        selectionFilter = filter;
        onFlipComplete = null;
        onSelectionComplete = null;

        RefreshAvailableAnimals();
    }

    /// <summary>
    /// 刷新可用动物列表（根据筛选条件）
    /// </summary>
    private void RefreshAvailableAnimals()
    {
        availableAnimals.Clear();
        AnimalBase[] allAnimals = FindObjectsOfType<AnimalBase>();

        foreach (var animal in allAnimals)
        {
            if (selectionFilter != null && !selectionFilter(animal))
                continue;
            if (animal.MapItem == null) // 已经跑出屏幕的忽略
                continue;
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
                    animal.OnAnimalClicked += OnAnimalSelected;
                else
                    animal.OnAnimalClicked -= OnAnimalSelected;
            }
        }
    }

    // ==================== 选择回调 ====================
    private void OnAnimalSelected(AnimalBase animal)
    {
        if (!IsInSelectionMode) return;
        if (!availableAnimals.Contains(animal)) return;

        if (onFlipComplete != null)
        {
            // 翻转模式：直接完成，不删除
            CompleteFlipSelection(animal);
        }
        else
        {
            // 移除模式：开始移除流程
            StartCoroutine(RemoveSingleAnimal(animal));
        }
    }

    // ==================== 翻转模式完成 ====================
    private void CompleteFlipSelection(AnimalBase animal)
    {
        if (!IsInSelectionMode) return;

        // 清除高亮效果
        RemoveSelectedEffect(animal);

        // 取消注册所有事件
        RegisterAnimalClickEvents(false);

        // 执行回调
        onFlipComplete?.Invoke(animal);

        // 退出模式
        ExitSelectionMode();
    }

    // ==================== 移除模式逻辑 ====================
    private IEnumerator RemoveSingleAnimal(AnimalBase animal)
    {
        if (animal == null || animal.gameObject == null) yield break;
        if (!availableAnimals.Contains(animal)) yield break;

        // 显示移除特效
        ShowRemoveEffect(animal.transform.position);
        // 播放音效（如有）
        // AudioManager.Instance.PlaySoundEffect("tool_remove");

        // 从可用列表中移除，避免重复点击
        availableAnimals.Remove(animal);
        // 移除高亮效果
        RemoveSelectedEffect(animal);
        // 取消注册该动物的点击事件
        animal.OnAnimalClicked -= OnAnimalSelected;

        // 延迟一点，让特效可见
        yield return new WaitForSeconds(0.2f);

        // 真正移除动物
        if (animal.MapItem != null)
            Map.Instance.RemoveItem(animal.MapItem);
        else
            Destroy(animal.gameObject);

        removedCount++;

        if (removedCount >= MaxSelectCount)
        {
            // 全部移除完成
            MessageSystem.Instance.ShowTip($"成功移除了 {removedCount} 只动物！");
            CompleteRemoveSelection();
        }
        else
        {
            // 移除后刷新可用动物列表
            RefreshAvailableAnimals();
            // 重新注册点击事件
            RegisterAnimalClickEvents(true);

            int next = removedCount + 1;
            MessageSystem.Instance.ShowTip($"已移除 {removedCount} 只，请选择第 {next} 只动物");
        }
    }

    private void CompleteRemoveSelection()
    {
        if (!IsInSelectionMode) return;

        // 清除所有高亮
        foreach (var kv in selectedHalos)
        {
            if (kv.Key != null) RemoveSelectedEffect(kv.Key);
        }
        selectedHalos.Clear();
        originalColors.Clear();

        // 取消注册事件
        RegisterAnimalClickEvents(false);

        // 执行完成回调
        onSelectionComplete?.Invoke();

        // 退出模式
        ExitSelectionMode();
    }

    // ==================== 退出选择模式（通用） ====================
    public void ExitSelectionMode()
    {
        if (!IsInSelectionMode) return;

        if (mask != null) mask.gameObject.SetActive(false);
        RegisterAnimalClickEvents(false);

        // 清除所有高亮效果
        foreach (var animal in new List<AnimalBase>(selectedHalos.Keys))
        {
            RemoveSelectedEffect(animal);
        }
        selectedHalos.Clear();
        originalColors.Clear();

        IsInSelectionMode = false;
        removedCount = 0;

        // 执行取消回调（如果存在）
        onSelectionCancel?.Invoke();

        // 清空回调
        onFlipComplete = null;
        onSelectionComplete = null;
        onSelectionCancel = null;
        selectionFilter = null;
    }

    // ==================== 高亮效果 ====================
    private void AddSelectedEffect(AnimalBase animal)
    {
        // 高亮颜色
        var renderer = animal.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (!originalColors.ContainsKey(animal))
                originalColors[animal] = renderer.material.color;
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
                renderer.material.color = originalColors[animal];
            originalColors.Remove(animal);
        }

        if (selectedHalos.ContainsKey(animal))
        {
            Destroy(selectedHalos[animal]);
            selectedHalos.Remove(animal);
        }
    }

    // ==================== 特效 ====================
    private void ShowRemoveEffect(Vector3 position)
    {
        GameObject effectObj = new GameObject("RemoveEffect");
        effectObj.transform.position = position + Vector3.up * 0.5f;
        var textMesh = effectObj.AddComponent<TextMesh>();
        textMesh.text = "✨ 移除 ✨";
        textMesh.color = Color.cyan;
        textMesh.fontSize = 35;
        textMesh.characterSize = 0.05f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        effectObj.AddComponent<Billboard>();
        StartCoroutine(FadeAndDestroy(effectObj, 0.8f));
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