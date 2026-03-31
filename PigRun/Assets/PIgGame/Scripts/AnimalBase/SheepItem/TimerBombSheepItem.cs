using System;
using System.Collections;
using PigGame;
using UnityEngine;

public class TimerBombSheepItem : AnimalBase
{
    
    [Header("倒计时设置")]
    [SerializeField] private float currentTime;         // 当前剩余时间
    [SerializeField] private bool enableTimerMode = true; // 是否启用倒计时模式
    
    [Header("UI显示")]
    [SerializeField] private TextMesh countText;        // 显示文本（同时显示倒计时和碰撞次数）
    
    [Header("提示设置")]
    [SerializeField] private bool showWarningTips = true;  // 是否显示警告提示
    [SerializeField] private float warningTimeThreshold = 3f; // 警告时间阈值（秒）
    
    private Coroutine countdownCoroutine;
    private bool isExploded = false;
    
    private void OnEnable()
    {
        StartCoroutine(GetDateTime());
    }

    IEnumerator GetDateTime()
    {
        yield return new WaitForSeconds(0.2f);
        
        if (enableTimerMode)
        {
            currentTime = MapItem.boomTime;
            StartCountdown();
        }
        UpdateCountDisplay();
    }
    
    private void OnDisable()
    {
        StopCountdown();
    }
    
    /// <summary>
    /// 开始倒计时
    /// </summary>
    private void StartCountdown()
    {
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);
        
        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }
    
    /// <summary>
    /// 停止倒计时
    /// </summary>
    private void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }
    
    /// <summary>
    /// 倒计时协程
    /// </summary>
    private IEnumerator CountdownCoroutine()
    {
        while (currentTime > 0 && !isExploded)
        {
            yield return new WaitForSeconds(1f);
            currentTime--;
            UpdateCountDisplay();
            
            // 检查倒计时结束
            if (currentTime <= 0)
            {
                ExplodeByTimer();
                yield break;
            }
            
            // 警告提示
            if (showWarningTips && currentTime <= warningTimeThreshold && currentTime > 0)
            {
                ShowTimerWarning();
            }
        }
    }
    
    /// <summary>
    /// 显示倒计时警告
    /// </summary>
    private void ShowTimerWarning()
    {
        // 显示警告面板或提示
        if (UIManager.Instance != null)
        {
            // 可以显示倒计时警告面板
            // UIManager.Instance.ShowTipPanel($"⚠️ 炸弹将在 {currentTime:F0} 秒后爆炸！");
            
            // 或者使用更友好的提示
            if (currentTime <= 3)
            {
                Debug.Log($"<color=red>[警告]</color> 倒计时炸弹羊将在 {currentTime} 秒后爆炸！");
            }
        }
        
        // 播放警告音效（可选）
        // AudioManager.Instance.PlaySoundEffect("timer_warning");
    }
    
    /// <summary>
    /// 倒计时结束爆炸
    /// </summary>
    private void ExplodeByTimer()
    {
        if (isExploded) return;
        isExploded = true;
        
        Debug.Log("倒计时结束，炸弹羊爆炸！");
        StopCountdown();
        
        // 显示倒计时结束提示
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPanel(PanelType.BombPanel);
            // 延迟显示爆炸面板，让提示更明显
            StartCoroutine(DelayedShowBombPanel(0.5f));
        }
        
        // 播放爆炸特效和音效
        // EffectManager.Instance.Play("TimerBombExplode", transform.position);
        // AudioManager.Instance.PlaySoundEffect("timer_explode");
        
        // 从地图移除
        Map.Instance.RemoveItem(mapItem);
        
        // 销毁自身
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 被撞击时重写逻辑（倒计时减少）
    /// </summary>
    public override void BeHit()
    {
        if (isExploded) return;
      
        UpdateCountDisplay();
        
        // 显示撞击效果
        //ShowHitEffect();
        
        // 调用基类的受击逻辑（播放动画等）
        base.BeHit();
    }
    
    /// <summary>
    /// 自身撞击障碍时重写
    /// </summary>
    public override void HitSelf()
    {
        if (isExploded) return;
       
        UpdateCountDisplay();
        
        base.HitSelf();
    }
    
    
    /// <summary>
    /// 延迟显示爆炸面板
    /// </summary>
    private IEnumerator DelayedShowBombPanel(float delay)
    {
        yield return new WaitForSeconds(delay);
        UIManager.Instance.ShowPanel(PanelType.BombPanel);
    }
    
    /// <summary>
    /// 更新显示（同时显示倒计时和碰撞次数）
    /// </summary>
    private void UpdateCountDisplay()
    {
        if (countText != null)
        {
            if(enableTimerMode)
            {
                // 只显示倒计时
                int displayTime = Mathf.Max(0, Mathf.CeilToInt(currentTime));
                countText.text = $"{displayTime}s";
            }
            
            // 根据危险程度改变颜色
            UpdateTextColor();
        }
    }
    
    /// <summary>
    /// 更新文本颜色
    /// </summary>
    private void UpdateTextColor()
    {
        if (countText == null) return;
        
        if (enableTimerMode)
        {
            // 倒计时模式下，根据时间决定颜色
            if (currentTime <= 3)
                countText.color = Color.red;
            else if (currentTime <= 5)
                countText.color = new Color(1f, 0.5f, 0f); // 橙色
            else
                countText.color = Color.yellow;
        }
    }
    
    /// <summary>
    /// 显示撞击效果
    /// </summary>
    private void ShowHitEffect()
    {
        // 创建浮动文字显示"-2s"
        GameObject textObj = new GameObject("TimeReduction");
        textObj.transform.position = transform.position + Vector3.up * 1.5f;
        var textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = "-2s";
        textMesh.color = Color.red;
        textMesh.fontSize = 40;
        textMesh.characterSize = 0.05f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        // 添加 Billboard 效果
        textObj.AddComponent<Billboard>();
        
        // 淡出并销毁
        StartCoroutine(FadeAndDestroy(textObj, 0.5f));
    }
    
    /// <summary>
    /// 显示时间减少提示
    /// </summary>
    private void ShowTimeReductionEffect()
    {
        // 播放时间减少音效
        // AudioManager.Instance.PlaySoundEffect("time_reduction");
        
        // 屏幕震动效果（可选）
        // CameraShake.Instance.Shake(0.1f, 0.2f);
    }
    
    /// <summary>
    /// 淡出并销毁物体
    /// </summary>
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
    
    /// <summary>
    /// 创建显示文本（如果未在 Inspector 中指定）
    /// </summary>
    private void CreateCountDisplay()
    {
        if (countText != null) return;
        
        GameObject textObj = new GameObject("TimerCountDisplay");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0, 1.5f, 0);
        
        countText = textObj.AddComponent<TextMesh>();
        countText.fontSize = 40;
        countText.characterSize = 0.05f;
        countText.anchor = TextAnchor.MiddleCenter;
        countText.alignment = TextAlignment.Center;
        countText.color = Color.white;
        
        // 让文字面向摄像机
        textObj.AddComponent<Billboard>();
        
        UpdateCountDisplay();
    }
    
    /// <summary>
    /// 重置炸弹羊（用于重新开始游戏）
    /// </summary>
    public void ResetBomb()
    {
        isExploded = false;
        //hitCount = 3;
        currentTime = mapItem.boomTime;
        UpdateCountDisplay();
        
        if (enableTimerMode)
        {
            StartCountdown();
        }
    }
    
    /// <summary>
    /// 获取当前倒计时时间（供外部调用）
    /// </summary>
    public float GetCurrentTime()
    {
        return currentTime;
    }
  
}
