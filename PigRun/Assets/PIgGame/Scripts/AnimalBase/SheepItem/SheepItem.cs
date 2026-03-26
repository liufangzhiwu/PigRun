public class SheepItem : AnimalBase
{
    private int hitCount = 0;
    private const int explodeLimit = 3;

    public override void BeHit()
    {
        hitCount++;
        if (hitCount >= explodeLimit)
        {
            Explode();
            return;
        }
        base.BeHit();
    }

    public override void HitSelf()
    {
        base.HitSelf();
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
        UIManager.Instance.ShowPanel(PanelType.FinishPanel);
    }
}