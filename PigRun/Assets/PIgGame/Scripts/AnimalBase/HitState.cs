// ========== 自身撞击状态 ==========
using DG.Tweening;
using UnityEngine;

public class HitState : AnimalBase.IAnimalState
{
    private readonly AnimalBase animal;
    private Tween firstDelay;   // 延迟回调

    public HitState(AnimalBase animal) 
    { 
        this.animal = animal; 
    }

    // public void Enter()
    // {
    //     // 取消之前的延迟
    //     if (firstDelay != null && firstDelay.IsActive())
    //         firstDelay.Kill();
    //
    //     // 强制重置动画，确保每次进入都重新播放
    //     animal.animator.SetBool(animal.IsHitHash, false);
    //     animal.animator.SetBool(animal.IsHitHash, true);
    //     AudioManager.Instance.PlaySoundEffect("jump");
    //
    //     // 短暂延迟后关闭动画并切换到闲置状态
    //     firstDelay = DOVirtual.DelayedCall(0.05f, () => {
    //         animal.animator.SetBool(animal.IsHitHash, false);
    //         if (animal.CurrentState is HitState)
    //             animal.ChangeState(new IdleState(animal));
    //     });
    // }
    
    public void Enter()
    {
        firstDelay?.Kill();

        // 直接播放指定状态，0 为层级，0 为过渡归一化时间（立即播放）
        animal.animator.Play("Hit", 0, 0f);
        AudioManager.Instance.PlaySoundEffect("jump");

        firstDelay = DOVirtual.DelayedCall(1f, () =>
        {
            if (animal.CurrentState is HitState)
                animal.ChangeState(new IdleState(animal));
        });
    }
    
    public void HandleClick()
    {
        // 点击时取消延迟，立即响应移动
        if (firstDelay != null && firstDelay.IsActive())
            firstDelay.Kill();
        firstDelay = null;

        // 先关闭动画参数，避免残留
        //animal.animator.SetBool(animal.IsHitHash, false);

        bool hasObstacle = animal.CalculateTargetPosition(out Vector3 targetPos);
        if (hasObstacle)
        {
            if (targetPos != Vector3.zero)
            {
                animal.ChangeState(new MovingState(animal, targetPos, false));
            }
            else
            {
                animal.HitSelf();
                animal.BehitItem?.BeHit();
                animal.BehitItem02?.BeHit();
            }
        }
        else
        {
            animal.ChangeState(new MovingState(animal, Vector3.zero, true));
        }
    }

    public void Update() { }

    public void Exit()
    {
        if (firstDelay != null && firstDelay.IsActive())
            firstDelay.Kill();
        firstDelay = null;
    }
}