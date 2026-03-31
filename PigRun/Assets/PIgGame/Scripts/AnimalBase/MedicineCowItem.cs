using System.Collections;
using UnityEngine;

public class MedicineCowItem : AnimalBase
{
    [Header("药牛设置")]
    [SerializeField] private bool isMedicine = true;
    
   
    private SickDonkeyItem linkedDonkey;
    
    protected override void Start()
    {
        base.Start();
        
        if (mapItem != null)
        {
            mapItem.animalType = (int)AnimalType.Cattle;
        }
    }
    

    /// <summary>
    /// 设置关联的病驴
    /// </summary>
    public void SetLinkedDonkey(SickDonkeyItem donkey)
    {
        linkedDonkey = donkey;
        Debug.Log($"药牛与病驴建立关联");
    }
    
    /// <summary>
    /// 重写点击方法 - 单击时跑向终点
    /// </summary>
    protected override void OnMouseUpAsButton()
    {
        if (UIManager.Instance.IsPanelTypeShowing() || !UIManager.Instance.PanelIsShowing(PanelType.GamePanel))
        {
            return;
        }
        
        StartRunToExit();
        
    }
    
    /// <summary>
    /// 开始跑向终点（使用现有的MovingState逻辑）
    /// </summary>
    private void StartRunToExit()
    {
        // 播放移动音效
        //AudioManager.Instance.PlaySoundEffect(moveSound);
        Debug.Log("药牛开始跑向终点");
        
        // 使用现有的移动逻辑
        HandleClick();
    }
    
    /// <summary>
    /// 处理点击移动（复用IdleState的点击逻辑）
    /// </summary>
    private void HandleClick()
    {
        bool hasObstacle = CalculateTargetPosition(out Vector3 targetPos);
        
        if (hasObstacle)
        {
            if (targetPos != Vector3.zero)
            {
                // 有障碍物，移动到障碍物前
                ChangeState(new MovingState(this, targetPos, false));
            }
            else
            {
                // 紧邻障碍，自身受击
                HitSelf();
                BehitItem?.BeHit();
            }
        }
        else
        {
            // 无障碍，直线向前移动
            ChangeState(new MovingState(this, Vector3.zero, true));
            StartCoroutine(WaitForMoveComplete());
        }
    }
    
    /// <summary>
    /// 等待移动完成，然后治愈病驴
    /// </summary>
    private IEnumerator WaitForMoveComplete()
    {
        yield return new WaitForEndOfFrame();
        HealLinkedDonkey();
          
    }
    
    /// <summary>
    /// 治愈关联的病驴
    /// </summary>
    private void HealLinkedDonkey()
    {

        if (linkedDonkey == null)
        {
            linkedDonkey = Map.Instance.sickDonkeyItem;
        }
        
        if (linkedDonkey != null && !linkedDonkey.IsHealed)
        {
            linkedDonkey.Heal();
            
            // 播放治愈音效
            //AudioManager.Instance.PlaySoundEffect(healSound);
            
            Debug.Log("药牛跑出，病驴被治愈");
        }
    }

    public override void BeHit()
    {
        MessageSystem.Instance.ShowTip("💊 点击药牛可以跑向终点治愈病驴！");
        base.BeHit();
    }
}