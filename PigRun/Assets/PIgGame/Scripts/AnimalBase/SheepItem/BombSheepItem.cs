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