using System.Collections;
using DG.Tweening;
using UnityEngine;

public class KangarooItem : AnimalBase
{
    [Header("袋鼠特殊属性")]
    [SerializeField] private float bounceHeight = 1f;      // 弹跳高度
    [SerializeField] private float bounceDuration = 0.3f;  // 弹跳持续时间
    // 袋鼠专用：记录起始位置
    protected Vector3 startPosition;          // 点击移动前的起始位置
    protected bool isKangaroo = false;        // 是否为袋鼠

    
    protected override void Start()
    {
        base.Start();
        
        // 确保袋鼠标识正确
        if (mapItem != null)
        {
            mapItem.animalType = (int)AnimalType.Kangaroo;
            isKangaroo = true;
        }
    }
    
    /// <summary>
    /// 重写受击方法，确保袋鼠在反弹时不会播放受击动画
    /// </summary>
    public override void HitSelf()
    {
        // 袋鼠在移动状态下撞击物体时反弹
        if (currentState is MovingState)
        {
            StartCoroutine(BounceBackToStartPosition());
        }
        
        // 其他情况正常处理
        base.HitSelf();
    }
    
    /// <summary>
    /// 袋鼠特有的弹跳效果（重写反弹方法）
    /// </summary>
    protected IEnumerator BounceBackToStartPosition()
    {
        yield return new WaitForSeconds(bounceDuration);
        Debug.Log("袋鼠弹跳回起始位置！");
        
        // 播放袋鼠叫声
        //AudioManager.Instance.PlaySoundEffect("kangaroo_jump");
        
        // 使用抛物线弹跳效果
        Vector3 startPos = transform.position;
        Vector3 endPos = startPosition;
        
        // 计算中间点（抛物线顶点）
        Vector3 midPoint = Vector3.Lerp(startPos, endPos, 0.5f);
        midPoint.y += bounceHeight;
       
        transform.DOMove(endPos, 1.5f).OnComplete(() =>
        {
            Vector3 newPos = Vector3.zero;
            ChangeState(new IdleState(this));
            Map.Instance.TryMoveItemTargetCell(mapItem, startGrid, out newPos);
        });
    }
    
    
    protected override void OnMouseUpAsButton()
    {
        if (UIManager.Instance.IsPanelTypeShowing() || !UIManager.Instance.PanelIsShowing(PanelType.GamePanel))
        {
            Debug.Log("进入弹窗界面，不触发动物逻辑");
            return;
        }
        
        // 记录起始位置（用于袋鼠反弹）
        if (isKangaroo)
        {
            startPosition = transform.position;
            Debug.Log($"袋鼠记录起始位置: {startPosition}");
        }
        
        currentState?.HandleClick();
    }

}