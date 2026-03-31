using System.Collections;
using UnityEngine;

public class SickDonkeyItem : AnimalBase
{
    [Header("病驴设置")]
    [SerializeField] private bool isSick = true;
    
    [Header("视觉效果")]
    [SerializeField] private Color sickColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color healColor = Color.white;
    
    private bool isHealed;
    
    public bool IsHealed => isHealed;
    
    protected override void Start()
    {
        base.Start();
        
        if (mapItem != null)
        {
            mapItem.animalType = (int)AnimalType.Donkey;
        }
        
        // 添加生病特效
        StartCoroutine(SickEffect());
        
        // 播放生病音效
        //AudioManager.Instance.PlaySoundEffect(sickSound);
        
        Debug.Log("病驴已激活，需要药牛跑出后才能治愈");
    }
    
    private IEnumerator SickEffect()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null) yield break;
        
        renderer.color = sickColor;
        
        while (!isHealed)
        {
            // 颤抖效果
            Vector3 originalPos = transform.position;
            float elapsed = 0;
            float shakeDuration = 0.1f;
            
            while (elapsed < shakeDuration && !isHealed)
            {
                elapsed += Time.deltaTime;
                float x = originalPos.x + Random.Range(-0.1f, 0.1f);
                float z = originalPos.z + Random.Range(-0.1f, 0.1f);
                transform.position = new Vector3(x, originalPos.y, z);
                yield return null;
            }
            
            transform.position = originalPos;
            yield return new WaitForSeconds(0.5f);
        }
        
        // 治愈后恢复颜色
        if (renderer != null)
        {
            renderer.color = healColor;
        }
    }

    
    /// <summary>
    /// 治愈病驴
    /// </summary>
    public void Heal()
    {
        if (isHealed) return;
        
        isHealed = true;
        
        // 播放治愈音效
        //AudioManager.Instance.PlaySoundEffect(healSound);
        
        // 显示治愈特效
        ShowHealEffect();
        
        // 停止生病特效协程
        StartCoroutine(HealedEffect());
        
        Debug.Log("病驴已被治愈，现在可以点击移动了");
    }
    
    private IEnumerator HealedEffect()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        
        // 闪烁效果
        for (int i = 0; i < 3; i++)
        {
            if (renderer != null)
                renderer.color = Color.yellow;
            yield return new WaitForSeconds(0.1f);
            if (renderer != null)
                renderer.color = healColor;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private void ShowHealEffect()
    {
        string str = "✨ 病驴已被治愈！现在可以移动了 ✨";
        MessageSystem.Instance.ShowTip(str);
        
        // 粒子特效
        StartCoroutine(CreateHealParticles());
    }
    
    private IEnumerator CreateHealParticles()
    {
        for (int i = 0; i < 20; i++)
        {
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particle.transform.localScale = Vector3.one * 0.08f;
            particle.transform.position = transform.position + Random.insideUnitSphere * 1f;
            
            var renderer = particle.GetComponent<Renderer>();
            renderer.material.color = Color.yellow;
            
            Destroy(particle, 0.5f);
            yield return new WaitForSeconds(0.03f);
        }
    }
    
    /// <summary>
    /// 重写点击方法 - 治愈后才能点击移动
    /// </summary>
    protected override void OnMouseUpAsButton()
    {
        if (UIManager.Instance.IsPanelTypeShowing() || !UIManager.Instance.PanelIsShowing(PanelType.GamePanel))
        {
            return;
        }
        
        if (isHealed)
        {
            // 治愈后可以正常点击移动
            base.OnMouseUpAsButton();
        }
        else
        {
            // 未治愈时显示提示
            ShowCannotMoveTip();
        }
    }
    
    /// <summary>
    /// 重写计算目标位置 - 治愈后才能移动
    /// </summary>
    public override bool CalculateTargetPosition(out Vector3 target)
    {
        if (!isHealed)
        {
            target = Vector3.zero;
            return false;
        }
        
        return base.CalculateTargetPosition(out target);
    }
    
    /// <summary>
    /// 重写被撞击方法
    /// </summary>
    public override void BeHit()
    {
        if (!isHealed)
        {
            return;
        }
        
        base.BeHit();
    }
    
    /// <summary>
    /// 重写自身撞击方法
    /// </summary>
    public override void HitSelf()
    {
        if (!isHealed)
        {
            return;
        }
        
        base.HitSelf();
    }
    
    private void ShowCannotMoveTip()
    {
        string str = "🤒 病驴需要药牛跑出后才能移动！\n💊 点击药牛让它跑向终点吧！";
        MessageSystem.Instance.ShowTip(str);
    }

}