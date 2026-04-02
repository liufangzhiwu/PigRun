using System.Collections.Generic;
using Newtonsoft.Json;

// 对应关卡 JSON 的根对象
[System.Serializable]
public class LevelData
{
    public Size size;                      // 地图世界大小
    public float time;                      // 倒计时
    public float tap;                          // 点击次数
    public CameraPosition cameraPos;         // 相机位置
    public CameraAngle cameraAngle;          // 相机角度
    public List<PigData> pigGroup;           // 猪的列表
    public List<object> obstacleGroup;       // 障碍物（空数组）
    public float roadSpeed;                     // 道路速度
    public bool is2Dir;                        // 是否双方向
    public bool isEatAnim;                     // 是否有吃动画
}

[System.Serializable]
public class Size
{
    public float x;
    public float y;
}

[System.Serializable]
public class CameraPosition
{
    public float x, y, z;
}

[System.Serializable]
public class CameraAngle
{
    public float x, y, z;
}

[System.Serializable]
public class PigData
{
    public Position position;
    public float angle;      // 旋转角度（0/90/180/270）
    public float type;        // 猪的类型，对应不同预制体
    public float boomTime;    // 爆炸时间（可能未使用）
}

[System.Serializable]
public class Position
{
    public float x, y;
}