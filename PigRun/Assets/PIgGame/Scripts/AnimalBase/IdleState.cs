// ========== 闲置状态 ==========

using UnityEngine;

public class IdleState : AnimalBase.IAnimalState
{
    private readonly AnimalBase animal;
    private float idleTimer;

    public IdleState(AnimalBase animal) { this.animal = animal; }

    public void Enter()
    {
        idleTimer = 0f;
        animal.animator.SetBool("IsRun", false);
        animal.idleFidgetDelay = Random.Range(10, 200);
    }

    public void Update()
    {
        idleTimer += Time.deltaTime;
        if (idleTimer >= animal.idleFidgetDelay)
        {
            animal.ChangeState(new FidgetState(animal));
        }
    }

    public void Exit() { }

    public void HandleClick()
    {
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
}