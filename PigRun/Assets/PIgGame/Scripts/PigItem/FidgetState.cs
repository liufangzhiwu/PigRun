// ========== 闲置小动作状态 ==========

using System.Collections;
using UnityEngine;

public class FidgetState : PigItem.IPigState
{
    private readonly PigItem pig;

    public FidgetState(PigItem pig) { this.pig = pig; }

    public void Enter()
    {
        pig.animator.SetBool("IsFidget", true);
        pig.StartCoroutine(ResetAfterDelay(0.5f));
    }

    private IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        pig.animator.SetBool("IsFidget", false);
        pig.ChangeState(new IdleState(pig));
    }

    public void Update() { }
    public void Exit() { }
}