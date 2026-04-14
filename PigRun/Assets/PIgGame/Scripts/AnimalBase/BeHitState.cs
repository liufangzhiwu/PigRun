// ========== 被撞击状态 ==========
using DG.Tweening;
using UnityEngine;

public class BeHitState : AnimalBase.IAnimalState
{
    private readonly AnimalBase animal;
    private static readonly int IsBeHitHash = Animator.StringToHash("IsBeHit");
    private Tween firstDelay;   // 第一个延迟（关闭动画）
    private Tween secondDelay;  // 第二个延迟（切换闲置）

    public BeHitState(AnimalBase animal)
    {
        this.animal = animal;
    }

    public void Enter()
    {
        // 取消之前未完成的延迟
        if (firstDelay != null && firstDelay.IsActive())
            firstDelay.Kill();
        
        animal.animator.SetBool(IsBeHitHash, true);

        // 延迟 0.5 秒关闭受击动画
        firstDelay = DOVirtual.DelayedCall(0.03f, () => {
            animal.animator.SetBool(IsBeHitHash, false);
                // 仅当当前状态仍然是 BeHitState 时才切换，避免覆盖其他状态
                if (animal.CurrentState is BeHitState)
                    animal.ChangeState(new IdleState(animal));
        });
    }

    public void HandleClick()
    {
        animal.animator.SetBool(IsBeHitHash, false);
        // 取消所有延迟回调，防止后续强制切换回闲置
        if (firstDelay != null && firstDelay.IsActive())
            firstDelay.Kill();
        if (secondDelay != null && secondDelay.IsActive())
            secondDelay.Kill();
        firstDelay = null;
        secondDelay = null;

        // 执行移动逻辑（与原来相同）
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
        // 退出状态时也取消延迟，避免残留
        if (firstDelay != null && firstDelay.IsActive())
            firstDelay.Kill();
        if (secondDelay != null && secondDelay.IsActive())
            secondDelay.Kill();
        firstDelay = null;
        secondDelay = null;
    }
}