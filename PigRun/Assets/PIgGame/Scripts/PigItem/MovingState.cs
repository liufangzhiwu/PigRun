
    // ========== 移动状态 ==========

    using UnityEngine;

    public class MovingState : PigItem.IPigState
    {
        private readonly PigItem pig;
        private readonly Vector3 targetPosition; // 仅当 movingToTarget 为 true 时有效
        private readonly bool movingForward;     // true: 直线前进；false: 移动到目标格子

        public MovingState(PigItem pig, Vector3 target, bool forward)
        {
            this.pig = pig;
            targetPosition = target;
            movingForward = forward;
        }

        public void Enter()
        {
            pig.animator.SetBool("IsRun", true);
            if (movingForward)
            {
                // 直线前进时通知地图更新区域（原有逻辑）
                Map.Instance.UpdateMapItemArea(pig.MapItem);
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
                pig.transform.Translate(Vector3.forward * pig.Speed * Time.deltaTime);
                // 新增：如果当前是闲置状态且站在跑道上，立即进入跑道状态
                if (Map.Instance.IsRunwayCell(pig.MapItem.gridPos))
                {
                    pig.ChangeState(new RunwayState(pig));
                }
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
                    float step = pig.Speed * Time.deltaTime;
                    pig.transform.position = Vector3.MoveTowards(pig.transform.position, targetPosition, step);
                    if (Vector3.Distance(pig.transform.position, targetPosition) < 0.05f)
                    {
                        // 到达目标，触发碰撞
                        pig.HitSelf();                // 自身受击
                        pig.BehitItem?.BeHit();       // 被撞物体受击
                    }
                }
                else
                {
                    // 目标位置无效（容错）
                    pig.HitSelf();
                    pig.BehitItem?.BeHit();
                }
            }
        }

        public void Exit()
        {
            pig.animator.SetBool("IsRun", false);
        }

        // 移动中不响应点击，无需实现 HandleClick
    }