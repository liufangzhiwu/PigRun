using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PigItem : MonoBehaviour
{
    Rigidbody pigRigidbody;
    float speed = 5f;

    // 缓存 MapItem 组件，用于后续移除
    MapItem mapItem;
    // 标记是否已经开始移动（点击后）
    bool isMoving = false;

    void Start()
    {
        pigRigidbody = GetComponent<Rigidbody>();
        mapItem = GetComponent<MapItem>(); // 获取地图项信息
    }

    void Update()
    {
        // 只有已经开始移动时才检测边界
        if (isMoving && Map.Instance != null)
        {
            if (IsOutOfBounds())
            {
                // 通过 Map 管理器移除自身（释放占用并销毁）
                Map.Instance.RemoveItem(mapItem);
            }
        }
    }

    void OnMouseUpAsButton()
    {
        pigRigidbody.isKinematic = false;
        Debug.Log("PigItem Had Mouse Button");
        pigRigidbody.AddForce(transform.forward * speed, ForceMode.Impulse);
        isMoving = true; // 标记开始移动
    }

    /// <summary>
    /// 检查小猪是否跑出地图网格边界
    /// </summary>
    bool IsOutOfBounds()
    {
        Vector3 pos = transform.position;
        // 将世界坐标转换到地图本地坐标系，并减去原点得到相对于地图左下角的偏移
        Vector3 localPos = Map.Instance.transform.InverseTransformPoint(pos) - Map.Instance.origin;
        float gridX = localPos.x; // 对应列方向
        float gridZ = localPos.z; // 对应行方向

        // 计算地图的总宽度和高度
        float totalWidth = Map.Instance.cols * Map.Instance.cellSize;
        float totalHeight = Map.Instance.rows * Map.Instance.cellSize;

        // 如果超出范围（加上微小容差），则认为出界
        if (gridX < -0.01f || gridX > totalWidth + 0.01f ||
            gridZ < -0.01f || gridZ > totalHeight + 0.01f)
        {
            return true;
        }
        return false;
    }
}