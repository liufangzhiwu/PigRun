
    // ========== 移动状态 ==========

    using UnityEngine;

    public class MovingState : AnimalBase.IAnimalState
    {
        private readonly AnimalBase animal;
        private readonly Vector3 targetPosition; // 仅当 movingToTarget 为 true 时有效
        private readonly bool movingForward;     // true: 直线前进；false: 移动到目标格子

        public MovingState(AnimalBase pig, Vector3 target, bool forward)
        {
            this.animal = pig;
            targetPosition = target;
            movingForward = forward;
        }

        public void Enter()
        {
            animal.animator.SetBool("IsRun", true);
            if (movingForward)
            {
                // 直线前进时通知地图更新区域（原有逻辑）
                Map.Instance.UpdateMapItemArea(animal.MapItem);
                AudioManager.Instance.PlaySoundEffect("pig-run");
            }
            else
            {
                AudioManager.Instance.PlaySoundEffect("pig-run");
            }
        }

        public void Update()
        {
            if (movingForward)
            {
                // 直线前进
                animal.transform.Translate(Vector3.forward * animal.Speed * Time.deltaTime);
                // 新增：如果当前是闲置状态且站在跑道上，立即进入跑道状态
                // if (Map.Instance.IsRunwayCell(pig.MapItem.gridPos))
                // {
                //     pig.ChangeState(new RunwayState(pig));
                // }
                //if (pig.IsOutOfScreen())
                // {
                //     Map.Instance.RemoveItem(pig.MapItem);
                //     pig.ChangeState(new IdleState(pig));
                // }
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
                        animal.HitSelf();                // 自身受击
                        animal.BehitItem?.BeHit();       // 被撞物体受击
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
            //pig.animator.SetBool("IsRun", false);
        }

        // 移动中不响应点击，无需实现 HandleClick
    }