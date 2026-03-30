using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class NumberTurtleItem : AnimalBase
{
    [Header("数字乌龟设置")]
    private int requiredEscapes;     // 需要跑出去的动物数量（显示的数字）
    [SerializeField] private int currentEscapes = 0;      // 当前已跑出去的数量
    [SerializeField] private TextMesh numberText;         // 显示数字的文本
   
    [Header("视觉效果")]
    [SerializeField] private Color completedColor = Color.green; // 完成后的颜色
    [SerializeField] private float progressFillSpeed = 0.5f;     // 进度填充速度
    
    
    private bool isComplete = false;      // 是否已完成任务
    private int currentEscapesCount = 0;  // 当前跑出的动物计数
    private List<AnimalBase> trackedAnimals = new List<AnimalBase>(); // 追踪的动物
    
    // 公共属性
    public bool IsComplete => isComplete;
    public int RequiredEscapes => requiredEscapes;
    public int CurrentEscapes => currentEscapes;
    
    private void OnEnable()
    {
        StartCoroutine(GetDateTime());
    }

    IEnumerator GetDateTime()
    {
        yield return new WaitForSeconds(0.2f);
        
        // 设置数量
        requiredEscapes = MapItem.boomTime;
        
        // 更新数字显示
        UpdateNumberDisplay();
        
        // 开始监听动物跑出事件
        StartListeningToEscapes();
        
        Debug.Log($"数字乌龟初始化，需要 {requiredEscapes} 只动物跑出去才能移动");
    }
    
    protected void OnDestroy()
    {
        // 停止监听
        StopListeningToEscapes();
    }
    
    /// <summary>
    /// 开始监听动物跑出事件
    /// </summary>
    private void StartListeningToEscapes()
    {
        // 通过 Map 的事件系统监听动物跑出
        if (Map.Instance != null)
        {
            Map.Instance.OnAnimalEscaped += OnAnimalEscaped;
        }
    }
    
    /// <summary>
    /// 停止监听动物跑出事件
    /// </summary>
    private void StopListeningToEscapes()
    {
        if (Map.Instance != null)
        {
            Map.Instance.OnAnimalEscaped -= OnAnimalEscaped;
        }
    }
    
    /// <summary>
    /// 动物跑出时的回调
    /// </summary>
    private void OnAnimalEscaped(AnimalBase animal)
    {
        if (isComplete) return;
        
        // 检查是否是有效的动物（排除自己）
        if (animal == this) return;
        
        // 避免重复计数同一只动物
        if (trackedAnimals.Contains(animal)) return;
        
        // 记录这只动物
        trackedAnimals.Add(animal);
        
        // 增加计数
        currentEscapes++;
        UpdateNumberDisplay();
        
        // 播放跑出音效
        //AudioManager.Instance.PlaySoundEffect(escapeSound);
        
        // 显示进度特效
        ShowProgressEffect();
        
        Debug.Log($"乌龟见证第 {currentEscapes}/{requiredEscapes} 只动物跑出");
        
        // 检查是否达到所需数量
        if (currentEscapes >= requiredEscapes)
        {
            CompleteTurtle();
        }
    }
    
    
    /// <summary>
    /// 更新数字显示
    /// </summary>
    private void UpdateNumberDisplay()
    {
        if (numberText != null)
        {
            if (isComplete)
            {
                numberText.text = "✓";
                numberText.color = completedColor;
            }
            else
            {
                int remaining = requiredEscapes - currentEscapes;
                numberText.text = remaining.ToString();
                
                // 根据剩余数量改变颜色
                if (remaining <= 1)
                    numberText.color = Color.green;
                else if (remaining <= 2)
                    numberText.color = Color.yellow;
            }
        }
    }
    
    /// <summary>
    /// 显示进度特效
    /// </summary>
    private void ShowProgressEffect()
    {
        // 创建进度提示文字
        // GameObject effectObj = new GameObject("ProgressEffect");
        // effectObj.transform.position = transform.position + Vector3.up * 1.5f;
        //
        // var textMesh = effectObj.AddComponent<TextMesh>();
        // textMesh.text = $"+1 ({currentEscapes}/{requiredEscapes})";
        // textMesh.color = Color.cyan;
        // textMesh.fontSize = 35;
        // textMesh.characterSize = 0.05f;
        // textMesh.anchor = TextAnchor.MiddleCenter;
        // textMesh.alignment = TextAlignment.Center;
        //
        // effectObj.AddComponent<Billboard>();
        // 播放进度音效
        //AudioManager.Instance.PlaySoundEffect(progressSound);
        
        // 可选：创建粒子效果
        // EffectManager.Instance.Play("ProgressParticle", transform.position);
    }
    
    /// <summary>
    /// 完成乌龟解锁
    /// </summary>
    private void CompleteTurtle()
    {
        isComplete = true;
        UpdateNumberDisplay();
        
        // 播放完成特效
        ShowCompleteEffect();
        
        // 播放完成音效
        //AudioManager.Instance.PlaySoundEffect(completeSound);
        
        // 播放庆祝动画
        StartCoroutine(CelebrateAnimation());
        
        Debug.Log($"乌龟完成任务！{requiredEscapes} 只动物已跑出，乌龟现在可以移动了！");
    }
    
    /// <summary>
    /// 庆祝动画
    /// </summary>
    private IEnumerator CelebrateAnimation()
    {
        Vector3 originalScale = transform.localScale;
        
        // 弹跳庆祝
        for (int i = 0; i < 3; i++)
        {
            transform.DOScale(originalScale * 1.2f, 0.1f);
            yield return new WaitForSeconds(0.1f);
            transform.DOScale(originalScale, 0.1f);
            yield return new WaitForSeconds(0.1f);
        }
        
        // 旋转庆祝
        //transform.DORotate(new Vector3(0, 360, 0), 0.5f, RotateMode.LocalAxisAdd);
    }
    
    /// <summary>
    /// 显示完成特效
    /// </summary>
    private void ShowCompleteEffect()
    {
        // 创建完成特效文字
        // GameObject effectObj = new GameObject("CompleteEffect");
        // effectObj.transform.position = transform.position + Vector3.up * 1.5f;
        //
        // var textMesh = effectObj.AddComponent<TextMesh>();
        // textMesh.text = "✨ 任务完成！可以移动了 ✨";
        // textMesh.color = Color.green;
        // textMesh.fontSize = 40;
        // textMesh.characterSize = 0.05f;
        // textMesh.anchor = TextAnchor.MiddleCenter;
        // textMesh.alignment = TextAlignment.Center;
        //
        // effectObj.AddComponent<Billboard>();
        
        // 创建围绕乌龟的粒子效果
        StartCoroutine(CreateSurroundingParticles());
    }
    
    /// <summary>
    /// 创建围绕粒子效果
    /// </summary>
    private IEnumerator CreateSurroundingParticles()
    {
        for (int i = 0; i < 10; i++)
        {
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particle.transform.localScale = Vector3.one * 0.1f;
            particle.transform.position = transform.position + Random.insideUnitSphere * 1.5f;
            
            var renderer = particle.GetComponent<Renderer>();
            renderer.material.color = Color.yellow;
            
            Destroy(particle, 0.5f);
            yield return new WaitForSeconds(0.05f);
        }
    }
    
    /// <summary>
    /// 重写点击处理方法
    /// </summary>
    protected override void OnMouseUpAsButton()
    {
        if (UIManager.Instance.IsPanelTypeShowing() || !UIManager.Instance.PanelIsShowing(PanelType.GamePanel))
        {
            return;
        }
        
        // 只有完成任务后才能移动
        if (isComplete)
        {
            currentState?.HandleClick();
        }
        else
        {
            // 未完成时，显示提示
            ShowCannotMoveTip();
        }
    }
    
    /// <summary>
    /// 重写计算目标位置方法，只有完成时才能移动
    /// </summary>
    public override bool CalculateTargetPosition(out Vector3 target)
    {
        if (!isComplete)
        {
            target = Vector3.zero;
            return false;
        }
        
        return base.CalculateTargetPosition(out target);
    }
    
    /// <summary>
    /// 显示无法移动提示
    /// </summary>
    private void ShowCannotMoveTip()
    {
        int remaining = requiredEscapes - currentEscapes;
        
        // 提示文字
      
        string tips = $"需要 {remaining} 只动物跑出去才能移动！";
        MessageSystem.Instance.ShowTip(tips);
        // 播放提示音效
        //AudioManager.Instance.PlaySoundEffect("cannot_move");
    }
    
    /// <summary>
    /// 重置乌龟（用于关卡重置）
    /// </summary>
    public void ResetTurtle()
    {
        currentEscapes = 0;
        isComplete = false;
        trackedAnimals.Clear();
        UpdateNumberDisplay();
        Debug.Log($"乌龟已重置，需要 {requiredEscapes} 只动物跑出去才能移动");
    }
    
    /// <summary>
    /// 设置所需跑出数量
    /// </summary>
    public void SetRequiredEscapes(int escapes)
    {
        requiredEscapes = escapes;
        currentEscapes = 0;
        isComplete = false;
        trackedAnimals.Clear();
        UpdateNumberDisplay();
    }
    
    /// <summary>
    /// 获取剩余需要跑出的数量
    /// </summary>
    public int GetRemainingEscapes()
    {
        return Mathf.Max(0, requiredEscapes - currentEscapes);
    }
    
    /// <summary>
    /// 获取当前进度（0-1）
    /// </summary>
    public float GetProgress()
    {
        return Mathf.Clamp01((float)currentEscapes / requiredEscapes);
    }
     
}