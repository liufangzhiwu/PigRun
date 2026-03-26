// ========== 被撞击状态 ==========

using System.Collections;
using UnityEngine;

public class BeHitState : AnimalBase.IAnimalState
{
    private readonly AnimalBase pig;

    public BeHitState(AnimalBase pig) { this.pig = pig; }

    public void Enter()
    {
        pig.animator.SetBool("IsBeHit", true);
        pig.StartCoroutine(ResetAfterDelay(0.5f));
    }

    private IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        pig.animator.SetBool("IsBeHit", false);
        yield return new WaitForSeconds(delay);
        pig.ChangeState(new IdleState(pig));
    }

    public void Update() { }
    public void Exit() { }
}