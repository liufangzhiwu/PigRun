// ========== 移动状态 ==========
using UnityEngine;

public class MovingState : AnimalBase.IAnimalState
{
    private readonly AnimalBase animal;
    private readonly Vector3 targetPosition;
    private readonly bool movingForward;
    private bool hasStarted = false;

    public MovingState(AnimalBase animal, Vector3 target, bool forward)
    {
        this.animal = animal;
        targetPosition = target;
        movingForward = forward;
    }

    public void Enter()
    {
        animal.animator.SetBool(animal.IsRunHash, true);
        animal.runParticleSystem.Play();

        if (movingForward)
        {
            Map.Instance.UpdateMapItemArea(animal.MapItem);
            AudioManager.Instance.PlaySoundEffect("animal-run");
        }

        hasStarted = true;
    }

    public void Update()
    {
        if (!hasStarted) return;

        if (movingForward)
        {
            // 直线前进
            animal.transform.Translate(Vector3.forward * animal.Speed * Time.deltaTime);
        }
        else
        {
            // 移动到目标格子
            if (targetPosition != Vector3.zero)
            {
                float step = animal.Speed * Time.deltaTime;
                animal.transform.position = Vector3.MoveTowards(animal.transform.position, targetPosition, step);

                if (Vector3.Distance(animal.transform.position, targetPosition) < 0.05f)
                {
                    animal.HitSelf();
                    animal.BehitItem?.BeHit();
                    animal.runParticleSystem.Stop();
                }
            }
            else
            {
                // 目标位置无效（容错）
                animal.HitSelf();
                animal.BehitItem?.BeHit();
            }
        }
    }

    public void Exit()
    {
        animal.animator.SetBool(animal.IsRunHash, false);
    }
}