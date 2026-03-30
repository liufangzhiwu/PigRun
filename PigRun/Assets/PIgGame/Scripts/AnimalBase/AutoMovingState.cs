using UnityEngine;

public class AutoMovingState : AnimalBase.IAnimalState
{
    private readonly ChickItem chick;
    private readonly Vector3 moveDirection;
    private bool hasStarted = false;
    
    public AutoMovingState(ChickItem chick, Vector3 direction)
    {
        this.chick = chick;
        this.moveDirection = direction;
    }
    
    public void Enter()
    {
        hasStarted = true;
        
        // 设置朝向
        if (moveDirection != Vector3.zero)
        {
            chick.transform.forward = moveDirection;
        }
        
        // 播放移动动画
        if (chick.animator != null)
        {
            chick.animator.SetBool("IsRun", true);
        }
    }
    
    public void Update()
    {
        if (!hasStarted) return;
        
        // 持续向指定方向移动
        chick.transform.Translate(moveDirection * chick.Speed * Time.deltaTime);
        
        // 可选：根据移动方向更新网格位置（如果需要）
        // UpdateGridPosition();
        
        // 检查是否跑出屏幕
        if (chick.IsOutOfScreen())
        {
            // 小鸡会在 ChickItem 的 Update 中处理跑出逻辑
            return;
        }
    }
    
    public void Exit()
    {
        // 停止移动动画
        if (chick.animator != null)
        {
            chick.animator.SetBool("IsRun", false);
        }
    }
    
    public void HandleClick()
    {
        // 自动移动状态不响应点击
    }
}