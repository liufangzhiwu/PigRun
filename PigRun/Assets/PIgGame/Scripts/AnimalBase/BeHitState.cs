// ========== 被撞击状态 ==========
using DG.Tweening;
using UnityEngine;

public class BeHitState : AnimalBase.IAnimalState
{
    private readonly AnimalBase animal;
    private static readonly int IsBeHitHash = Animator.StringToHash("IsBeHit");

    public BeHitState(AnimalBase animal)
    {
        this.animal = animal;
    }

    public void Enter()
    {
        animal.animator.SetBool(IsBeHitHash, true);
        
        // 延迟 0.5 秒关闭受击动画
        DOVirtual.DelayedCall(0.5f, () => {
            animal.animator.SetBool(IsBeHitHash, false);
            // 再延迟 0.5 秒切换回闲置状态
            DOVirtual.DelayedCall(0.5f, () => {
                animal.ChangeState(new IdleState(animal));
            });
        });
    }

    public void Update() { }
    public void Exit() { }
}