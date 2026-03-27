using System;
using System.Collections;
using PigGame;
using UnityEngine;

public class BombSheepItem : AnimalBase
{
    private int hitCount = 3;
    [SerializeField] private TextMesh countText;

    private void OnEnable()
    {
        //CreateCountDisplay();
    }

    public override void BeHit()
    {
        hitCount--;
        UpdateCountDisplay();
        if (hitCount <= 0)
        {
            Explode();
            return;
        }
        base.BeHit();
    }

    public override void HitSelf()
    {
        hitCount--;
        UpdateCountDisplay();
        if (hitCount <= 0)
        {
            Explode();
            return;
        }
        base.HitSelf();
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
        textMesh.text = "-1";
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
    
    
    private void UpdateCountDisplay()
    {
        if (countText != null)
        {
            countText.text = $"{hitCount}";
            
            // 根据碰撞次数改变颜色
            if (hitCount == 3)
                countText.color = Color.green;
            else if (hitCount == 2)
            {
                countText.color = Color.yellow;
                UIManager.Instance.ShowPanel(PanelType.TipAnimalPanel);
            }
            else
                countText.color = Color.red;
        }
    }
    
    private void CreateCountDisplay()
    {
        GameObject textObj = new GameObject("HitCountDisplay");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0, 1.5f, 0);
        
        countText = textObj.AddComponent<TextMesh>();
        countText.text = "3";
        countText.fontSize = 40;
        countText.characterSize = 0.05f;
        countText.anchor = TextAnchor.MiddleCenter;
        countText.alignment = TextAlignment.Center;
        countText.color = Color.white;
        
        // 让文字面向摄像机
        textObj.AddComponent<Billboard>();
    }

    private void Explode()
    {
        // 播放特效
        // EffectManager.Instance.Play("SheepExplode", transform.position);
        // 播放音效
        //AudioManager.Instance.PlaySoundEffect("sheep_explode");
        // 从地图移除
        Map.Instance.RemoveItem(mapItem);
        // 游戏结束（失败）
        // 销毁自身
        Destroy(gameObject);
        UIManager.Instance.ShowPanel(PanelType.BombPanel);
    }
}

// 简单的 Billboard 脚本，让物体始终面向摄像机
public class Billboard : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
            transform.LookAt(Camera.main.transform);
    }
}