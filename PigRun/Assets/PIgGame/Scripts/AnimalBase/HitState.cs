// ========== 自身撞击状态 ==========
using DG.Tweening;
using UnityEngine;

public class HitState : AnimalBase.IAnimalState
{
    private readonly AnimalBase animal;
    private static readonly int IsHitHash = Animator.StringToHash("IsHit");

    public HitState(AnimalBase animal) 
    { 
        this.animal = animal; 
    }

    public void Enter()
    {
        animal.animator.SetBool(IsHitHash, true);
        AudioManager.Instance.PlaySoundEffect("jump");
        
        // 延迟 0.5s 关闭受击动画
        DOVirtual.DelayedCall(0.5f, () => {
            animal.animator.SetBool(IsHitHash, false);
            // 再延迟 0.5s 切换回闲置状态
            DOVirtual.DelayedCall(0.5f, () => {
                animal.ChangeState(new IdleState(animal));
            });
        });
    }

    public void Update() { }
    public void Exit() { }
}