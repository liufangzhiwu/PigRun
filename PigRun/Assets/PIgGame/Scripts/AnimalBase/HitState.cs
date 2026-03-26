// ========== 自身撞击状态 ==========

using System.Collections;
using UnityEngine;

public class HitState : AnimalBase.IAnimalState
{
    private readonly AnimalBase animal;

    public HitState(AnimalBase pig) { this.animal = pig; }

    public void Enter()
    {
        animal.animator.SetBool("IsHit", true);
        AudioManager.Instance.PlaySoundEffect("jump");
        animal.StartCoroutine(ResetAfterDelay(0.5f));
    }

    private IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        animal.animator.SetBool("IsHit", false);
        yield return new WaitForSeconds(delay);
        animal.ChangeState(new IdleState(animal));
    }

    public void Update() { } // 无需每帧逻辑
    public void Exit() { }
}