
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
            //没有障碍物时
            if (movingForward)
            {
                Map.Instance.UpdateMapItemArea(animal.MapItem);
                AudioManager.Instance.PlaySoundEffect("animal-run");
            }
            else //有障碍物时
            {
                //AudioManager.Instance.PlaySoundEffect("animal-run");
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
                        // 到达目标，触发碰撞
                        //animal.animator.SetBool("IsRun", false);
                        animal.HitSelf();                // 自身受击
                        animal.BehitItem?.BeHit();       // 被撞物体受击
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
            // 退出时停止移动
            animal.animator.SetBool(animal.IsRunHash, false);
        }
    }