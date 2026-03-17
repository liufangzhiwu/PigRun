// ========== 闲置状态 ==========

using UnityEngine;

public class IdleState : PigItem.IPigState
{
    private readonly PigItem pig;
    private float idleTimer;

    public IdleState(PigItem pig) { this.pig = pig; }

    public void Enter()
    {
        idleTimer = 0f;
        pig.animator.SetBool("IsRun", false);
        // 随机化下次闲置触发时间（原有逻辑）
        pig.idleFidgetDelay = Random.Range(10, 400);
    }

    public void Update()
    {
        idleTimer += Time.deltaTime;
        if (idleTimer >= pig.idleFidgetDelay)
        {
            pig.ChangeState(new FidgetState(pig));
        }
    }

    public void Exit() { }

    public void HandleClick()
    {
        // 点击处理：计算移动目标
        bool hasObstacle = pig.CalculateTargetPosition(out Vector3 targetPos);

        if (hasObstacle)
        {
            if (targetPos != Vector3.zero)
            {
                // 非紧邻障碍：移动到目标点
                pig.ChangeState(new MovingState(pig, targetPos, false));
            }
            else
            {
                // 紧邻障碍：自身受击
                pig.HitSelf(); // 直接切换状态
                pig.BehitItem?.BeHit();       // 被撞物体受击
            }
        }
        else
        {
            // 无障碍：直线向前移动
            pig.ChangeState(new MovingState(pig, Vector3.zero, true));
        }
    }
}