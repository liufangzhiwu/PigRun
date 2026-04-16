using System.Collections;
using DG.Tweening;
using UnityEngine;

public class KangarooItem : AnimalBase
{
    [Header("袋鼠特殊属性")]
    [SerializeField] private float bounceHeight = 1f;      // 弹跳高度
    [SerializeField] private float bounceDuration = 0.3f;  // 弹跳持续时间

    protected Vector3 startPosition;          // 点击移动前的起始位置
    protected bool isKangaroo = false;        // 是否为袋鼠
    public bool isMoveTarget = false;         // 是否存在有距离的目标对象

    protected override void Start()
    {
        base.Start();

        if (mapItem != null)
        {
            mapItem.animalType = (int)AnimalType.Kangaroo;
            isKangaroo = true;
            isMoveTarget = false;
        }
    }

    /// <summary>
    /// 重写受击方法，确保袋鼠在反弹时不会播放受击动画
    /// </summary>
    public override void TargetHitSelf()
    {
        isMoveTarget = true;
        AudioManager.Instance.PlaySoundEffect("jump");
        // 不调用 base.TargetHitSelf()，避免进入受击状态
        StartCoroutine(BounceBackToStartPosition());
    }
   
    protected IEnumerator BounceBackToStartPosition()
    {
        // 先前进一小段距离（例如 0.2 单位）
        Vector3 forwardPos = transform.position + transform.forward * 0.2f;
        Tween forwardTween = transform.DOMove(forwardPos, 0.1f).SetEase(Ease.OutQuad);
        yield return forwardTween.WaitForCompletion();

        // 触发跳跃动画
        animator.SetBool("IsJump", true);

        yield return new WaitForSeconds(0.2f);

        
        // 计算跳跃距离（从当前位置到起始位置）
        float distance = Vector3.Distance(transform.position, startPosition);
        float jumpSpeed = 5f;
        float jumpDuration = distance / jumpSpeed;
        jumpDuration = Mathf.Max(jumpDuration, 0.2f);

        // if (jumpDuration > 0.3f)
        // {
        //     
        // }
        
        Debug.Log("跳跃需要时间："+jumpDuration);

        // 平滑移动到起始位置
        Tween moveTween = transform.DOMove(startPosition, jumpDuration).SetEase(Ease.OutQuad);
        yield return moveTween.WaitForCompletion();

        // 精确更新地图网格占用
        Vector3 newPos = Vector3.zero;
        Map.Instance.TryMoveItemTargetCell(mapItem, startGrid, out newPos);
        transform.position = newPos;

        // 结束跳跃动画
        animator.SetBool("IsJump", false);
        isMoveTarget = false;
        // 切换回闲置状态
        ChangeState(new IdleState(this));
    }

    protected override void OnMouseUpAsButton()
    {
        if (UIManager.Instance.IsPanelTypeShowing() || !UIManager.Instance.PanelIsShowing(PanelType.GamePanel))
        {
            Debug.Log("进入弹窗界面，不触发动物逻辑");
            return;
        }

        // 记录起始位置和网格（用于反弹）
        if (isKangaroo)
        {
            startPosition = transform.position;
            startGrid = mapItem.gridPos; // 记录起始网格
            Debug.Log($"袋鼠记录起始位置: {startPosition}");
        }

        currentState?.HandleClick();
        isMoveTarget = false;
    }
}