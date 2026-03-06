using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PigItem : MonoBehaviour
{
    public Animator animator;
    
    Rigidbody pigRigidbody;
    float speed = 5f;

    MapItem mapItem;
    bool isMoving = false;

    void Start()
    {
        pigRigidbody = GetComponent<Rigidbody>();
        mapItem = GetComponent<MapItem>();
    }

    void Update()
    {
        // 只有已经开始移动时才检测屏幕边界
        if (isMoving && Map.Instance != null)
        {
            if (IsOutOfScreen())
            {
                Map.Instance.RemoveItem(mapItem);
            }
        }
    }

    void OnMouseUpAsButton()
    {
        animator.SetBool("IsRun",true);
        pigRigidbody.isKinematic = false;
        Debug.Log("PigItem Had Mouse Button");
        pigRigidbody.AddForce(transform.forward * speed, ForceMode.Impulse);
        isMoving = true;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // 可选：只对特定标签（如 "Obstacle"）的对象做出反应
        if (collision.gameObject.CompareTag("Pig"))
        {
            if (isMoving)
            {
                animator.SetBool("IsRun",false);
                animator.SetBool("IsHit",true);
                pigRigidbody.isKinematic = true;
                isMoving = false;
                Debug.Log("Pig collided with obstacle, stopped moving.");
            }
        }
    }

    /// <summary>
    /// 检查小猪是否跑出屏幕（摄像机视野外）
    /// </summary>
    bool IsOutOfScreen()
    {
        // 将世界坐标转换为视口坐标 (0~1)
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);

        // 如果物体在摄像机后方（z<0）或者超出视口范围（加上微小容差），则认为出界
        if (viewportPos.z < 0 ||
            viewportPos.x < -0.01f || viewportPos.x > 1.01f ||
            viewportPos.y < -0.01f || viewportPos.y > 1.01f)
        {
            return true;
        }
        return false;
    }
}