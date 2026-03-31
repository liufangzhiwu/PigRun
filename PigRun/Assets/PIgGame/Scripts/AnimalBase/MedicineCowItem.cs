using System.Collections;
using UnityEngine;

public class MedicineCowItem : AnimalBase
{
    [Header("药牛设置")]
    [SerializeField] private bool isMedicine = true;
    [SerializeField] private string moveSound = "cow_move";
    [SerializeField] private string healSound = "heal_sound";
    
    [Header("视觉特效")]
    [SerializeField] private Color medicineGlowColor = Color.green;
    
    private bool isRunningToExit = false;
    private SickDonkeyItem linkedDonkey = null;
    
    public bool IsRunningToExit => isRunningToExit;
    public SickDonkeyItem LinkedDonkey => linkedDonkey;
    
    protected override void Start()
    {
        base.Start();
        
        if (mapItem != null)
        {
            mapItem.animalType = (int)AnimalType.Cattle;
        }
        
        // 添加药牛特效
        StartCoroutine(MedicineGlowEffect());
        
        Debug.Log("药牛已激活，点击它会跑向终点并治愈病驴");
    }
    
    private IEnumerator MedicineGlowEffect()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null) yield break;
        
        while (!isRunningToExit)
        {
            float alpha = 0.5f + Mathf.Sin(Time.time * 2f) * 0.3f;
            renderer.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
    }
    
    /// <summary>
    /// 设置关联的病驴
    /// </summary>
    public void SetLinkedDonkey(SickDonkeyItem donkey)
    {
        linkedDonkey = donkey;
        Debug.Log($"药牛与病驴建立关联");
    }
    
    /// <summary>
    /// 重写点击方法 - 单击时跑向终点
    /// </summary>
    protected override void OnMouseUpAsButton()
    {
        if (UIManager.Instance.IsPanelTypeShowing() || !UIManager.Instance.PanelIsShowing(PanelType.GamePanel))
        {
            return;
        }
        
        if (!isRunningToExit)
        {
            StartRunToExit();
        }
        else
        {
            ShowTip("🐄 药牛正在跑向终点...");
        }
    }
    
    /// <summary>
    /// 开始跑向终点（使用现有的MovingState逻辑）
    /// </summary>
    private void StartRunToExit()
    {
        isRunningToExit = true;
        
        // 播放移动音效
        AudioManager.Instance.PlaySoundEffect(moveSound);
        
        // 显示跑向终点提示
        ShowRunEffect();
        
        Debug.Log("药牛开始跑向终点");
        
        // 使用现有的移动逻辑
        HandleClick();
    }
    
    /// <summary>
    /// 处理点击移动（复用IdleState的点击逻辑）
    /// </summary>
    private void HandleClick()
    {
        bool hasObstacle = CalculateTargetPosition(out Vector3 targetPos);
        
        if (hasObstacle)
        {
            if (targetPos != Vector3.zero)
            {
                // 有障碍物，移动到障碍物前
                ChangeState(new MovingState(this, targetPos, false));
            }
            else
            {
                // 紧邻障碍，自身受击
                HitSelf();
                BehitItem?.BeHit();
            }
        }
        else
        {
            // 无障碍，直线向前移动
            ChangeState(new MovingState(this, Vector3.zero, true));
            StartCoroutine(WaitForMoveComplete());
        }
    }
    
    /// <summary>
    /// 等待移动完成，然后治愈病驴
    /// </summary>
    private IEnumerator WaitForMoveComplete()
    {
        yield return new WaitForEndOfFrame();
        HealLinkedDonkey();
          
    }
    
    /// <summary>
    /// 治愈关联的病驴
    /// </summary>
    private void HealLinkedDonkey()
    {
        if (linkedDonkey != null && !linkedDonkey.IsHealed)
        {
            linkedDonkey.Heal();
            
            // 播放治愈音效
            AudioManager.Instance.PlaySoundEffect(healSound);
            
            Debug.Log("药牛跑出，病驴被治愈");
        }
    }
    
    /// <summary>
    /// 重写跑出屏幕检测，触发治愈
    /// </summary>
    public bool IsOutOfScreen()
    {
        bool isOut = base.IsOutOfScreen();
        
        if (isOut && isRunningToExit && linkedDonkey != null && !linkedDonkey.IsHealed)
        {
            // 跑出屏幕时治愈病驴
            HealLinkedDonkey();
        }
        
        return isOut;
    }
    
    private void ShowRunEffect()
    {
        GameObject effectObj = new GameObject("RunEffect");
        effectObj.transform.position = transform.position + Vector3.up * 0.5f;
        
        var textMesh = effectObj.AddComponent<TextMesh>();
        textMesh.text = "🏃 药牛跑向终点！ 🏃";
        textMesh.color = Color.green;
        textMesh.fontSize = 35;
        textMesh.characterSize = 0.05f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        effectObj.AddComponent<Billboard>();
        StartCoroutine(FadeAndDestroy(effectObj, 0.8f));
    }
    
    private void ShowTip(string tip)
    {
        GameObject tipObj = new GameObject("Tip");
        tipObj.transform.position = transform.position + Vector3.up * 1.2f;
        
        var textMesh = tipObj.AddComponent<TextMesh>();
        textMesh.text = tip;
        textMesh.color = Color.yellow;
        textMesh.fontSize = 25;
        textMesh.characterSize = 0.05f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        tipObj.AddComponent<Billboard>();
        StartCoroutine(FadeAndDestroy(tipObj, 1f));
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
    
    public override void BeHit()
    {
        ShowTip("💊 点击药牛可以跑向终点治愈病驴！");
        base.BeHit();
    }
}