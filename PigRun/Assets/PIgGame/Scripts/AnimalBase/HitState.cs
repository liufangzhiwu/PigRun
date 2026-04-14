// ========== 自身撞击状态 ==========
using DG.Tweening;
using UnityEngine;

public class HitState : AnimalBase.IAnimalState
{
    private readonly AnimalBase animal;
    private static readonly int IsHitHash = Animator.StringToHash("IsHit");
    private Tween firstDelay;   // 关闭动画的延迟

    public HitState(AnimalBase animal) 
    { 
        this.animal = animal; 
    }

    public void Enter()
    {
        // 取消之前未完成的延迟
        if (firstDelay != null && firstDelay.IsActive())
            firstDelay.Kill();

        // 强制重置动画参数，确保每次撞击都重新播放动画
        animal.animator.SetBool(IsHitHash, false);
        animal.animator.SetBool(IsHitHash, true);
        AudioManager.Instance.PlaySoundEffect("jump");

        // 0.05秒后关闭受击动画
        firstDelay = DOVirtual.DelayedCall(0.03f, () => {
            animal.animator.SetBool(IsHitHash, false);
            // 再0.5秒后切换到闲置状态（仅当当前状态仍是HitState）
            if (animal.CurrentState is HitState)
                animal.ChangeState(new IdleState(animal));
        });
    }
    
    public void HandleClick()
    {
        // 点击时先强制关闭动画参数，避免残留
        animal.animator.SetBool(IsHitHash, false);
        // 取消延迟，防止后续切换回闲置
        if (firstDelay != null && firstDelay.IsActive())
            firstDelay.Kill();
        firstDelay = null;

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
        if (firstDelay != null && firstDelay.IsActive())
            firstDelay.Kill();
        firstDelay = null;
        animal.animator.SetBool(IsHitHash, false);
    }
}