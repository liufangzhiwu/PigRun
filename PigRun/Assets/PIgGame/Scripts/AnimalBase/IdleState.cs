// ========== 闲置状态 ==========

using System.Collections.Generic;
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
        animal.idleFidgetDelay = Random.Range(10, 100);
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
        if (animal.MapItem.animalType == (int)AnimalType.Elephant)
        {
            HandleElephantClick();
        }
        else
        {
            bool hasObstacle = animal.CalculateTargetPosition(out Vector3 targetPos);
            animal.runParticleSystem.Play();
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
                    animal.runParticleSystem.Stop();
                }
            }
            else
            {
                animal.ChangeState(new MovingState(animal, Vector3.zero, true));
            }
        }
    }
    
    public void HandleElephantClick()
    {
        animal.runParticleSystem.Play();
        bool hasObstacle = animal.GetHitTargets(out List<AnimalBase> hitAnimals, out List<Vector3> targetPositions);
        
        // 使用多目标检测接口（支持大象）
        if (hasObstacle)
        {
            if (targetPositions[0] != Vector3.zero)
            {
                animal.ChangeState(new MovingState(animal, targetPositions[0], false));
            }
            else
            {
                // 紧邻障碍，直接撞击
                animal.HitSelf();
                foreach (var hitAnimal in hitAnimals)
                {
                    hitAnimal.BeHit();
                }
                animal.runParticleSystem.Stop();
            }
        }
        else
        {
            // 无撞击目标，直线移动到边界
            animal.ChangeState(new MovingState(animal, Vector3.zero, true));
        }
    }
}