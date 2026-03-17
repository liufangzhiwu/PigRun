// ========== 自身撞击状态 ==========

using System.Collections;
using UnityEngine;

public class HitState : PigItem.IPigState
{
    private readonly PigItem pig;

    public HitState(PigItem pig) { this.pig = pig; }

    public void Enter()
    {
        pig.animator.SetBool("IsHit", true);
        AudioManager.Instance.PlaySoundEffect("jump");
        pig.StartCoroutine(ResetAfterDelay(0.5f));
    }

    private IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        pig.animator.SetBool("IsHit", false);
        pig.ChangeState(new IdleState(pig));
    }

    public void Update() { } // 无需每帧逻辑
    public void Exit() { }
}