using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 大象动物 - 可同时撞击前方两个格子中的动物
/// </summary>
public class ElephantItem : AnimalBase
{
    /// <summary>
    /// 获取撞击目标列表（大象可同时撞击两个动物）
    /// </summary>
    public override bool GetHitTargets(out List<AnimalBase> hitAnimals, out List<Vector3> targetPositions)
    {
        hitAnimals = new List<AnimalBase>();
        targetPositions = new List<Vector3>();
        
        var map = Map.Instance;
        int rows = map.rows;
        int cols = map.cols;
        Vector3 target = Vector3.zero;

        // 获取前方两个检测格子的世界坐标
        List<Vector2Int> frontCells = GetElephantFrontCells(out List<Vector2Int> currentGrid,out Vector2Int forwardOffset);
        int index = 0;
        foreach (Vector2Int checkGrid in frontCells)
        {
            // 边界检查
            if (checkGrid.x < 0 || checkGrid.x >= rows || checkGrid.y < 0 || checkGrid.y >= cols)
                continue;

            int occupantId = Map.Instance.GetOccupantIdAtCell(checkGrid);
            if (occupantId != -1 && occupantId != mapItem.id)
            {
                AnimalBase animal = Map.Instance.GetPlacedItem(occupantId)?.instance.GetComponent<AnimalBase>();
               
                if (animal != null)
                {
                    hitAnimals.Add(animal);
                    // 紧邻障碍
                    if (checkGrid - forwardOffset == currentGrid[index])
                    {
                        targetPositions.Add(target);
                        index++;
                    }
                    else
                    {
                        Vector2Int vector = Vector2Int.zero;
                        // 根据旋转调整目标格子（原有逻辑）
                        switch (mapItem.rotIndex)
                        {
                            case 0: vector = new Vector2Int(checkGrid.x, checkGrid.y-1); break;
                            case 1: vector = new Vector2Int(checkGrid.x + 2, checkGrid.y); break;
                            case 2: vector = new Vector2Int(checkGrid.x + 2, checkGrid.y + 2); break;
                            default: vector = new Vector2Int(checkGrid.x - 1, checkGrid.y + 1); break;
                        }

                        Vector2Int obstacleGrid = new Vector2Int(vector.x, vector.y);
                        map.TryMoveItemTargetCell(mapItem, obstacleGrid, out target);
                        targetPositions.Add(target);
                        index++;
                    }
                }
            }
        }

        return hitAnimals.Count > 0;
    }

    /// <summary>
    /// 根据大象的旋转方向获取前方两个检测格子的网格坐标
    /// </summary>
    private List<Vector2Int> GetElephantFrontCells(out List<Vector2Int> currentGrid, out Vector2Int forwardOffset)
    {
        List<Vector2Int> frontCells = new List<Vector2Int>();
        currentGrid = new List<Vector2Int>();
        switch (mapItem.rotIndex)
        {
            case 0: forwardOffset = new Vector2Int(1, 0); // 右
                currentGrid.Add(new Vector2Int(mapItem.gridPos.x, mapItem.gridPos.y+1));
                currentGrid.Add(new Vector2Int(mapItem.gridPos.x, mapItem.gridPos.y+3));
                break;
            case 1:
                currentGrid.Add(new Vector2Int(mapItem.gridPos.x - 2, mapItem.gridPos.y));
                currentGrid.Add(new Vector2Int(mapItem.gridPos.x, mapItem.gridPos.y));
                forwardOffset = new Vector2Int(0, 1); // 下
                break;
            case 2:
                currentGrid.Add(new Vector2Int(mapItem.gridPos.x, mapItem.gridPos.y - 2));
                currentGrid.Add(new Vector2Int(mapItem.gridPos.x, mapItem.gridPos.y));
                forwardOffset = new Vector2Int(-1, 0); // 左
                break;
            default:
                currentGrid.Add(new Vector2Int(mapItem.gridPos.x+1, mapItem.gridPos.y)); 
                currentGrid.Add(new Vector2Int(mapItem.gridPos.x+3, mapItem.gridPos.y)); 
                forwardOffset = new Vector2Int(0, -1); // 上
                frontCells.Add(currentGrid[0]+forwardOffset);
                frontCells.Add(currentGrid[1]+forwardOffset);
                break;
        }
        return frontCells;
        
    }


    // 可选：重写点击行为，使大象也能单独移动（但通常大象通过撞击触发）
    // 这里保持基类行为即可
}