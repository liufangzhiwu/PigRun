// ========== 闲置小动作状态 ==========

using System.Collections;
using UnityEngine;

public class FidgetState : AnimalBase.IAnimalState
{
    private readonly AnimalBase animal;

    public FidgetState(AnimalBase pig) { this.animal = pig; }

    public void Enter()
    {
        animal.animator.SetBool("IsFidget", true);
        animal.StartCoroutine(ResetAfterDelay(0.5f));
    }

    private IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        animal.animator.SetBool("IsFidget", false);
        animal.ChangeState(new IdleState(animal));
    }
    
    public void HandleClick()
    {
        if (!(animal.CurrentState is FidgetState)) return;
        
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
    public void Exit() { }
}