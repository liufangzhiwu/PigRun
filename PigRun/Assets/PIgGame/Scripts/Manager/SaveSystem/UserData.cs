using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
// using ThinkingAnalytics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 用户游戏数据管理类
/// 负责处理用户数据的加载、保存、初始化及日常管理
/// 使用JSON序列化和加密存储用户数据
/// </summary>
public class UserData
{
    #region 用户基础数据
    public string PlayerId;              // 玩家ID
    public int Gold;                     // 当前金币数量
    public int LevelIndex;               // 关卡序号

    // 道具数量（初始均为1）
    public int RemoveToolCount = 1;      // 移除道具数量
    public int FlipToolCount = 1;        // 翻转道具数量
    public int ShuffleToolCount = 1;     // 打乱道具数量
    #endregion

    #region 系统设置数据
    public bool IsMusicOn = true;        // 背景音乐开关
    public bool IsSoundOn = true;        // 音效开关
    //public bool IsVibrationOn ;         // 震动反馈开关
    public bool IsAgreePrivacy;          // 同意用户隐私协议
    #endregion

    #region 文件路径管理
    /// <summary>
    /// 获取用户数据保存路径
    /// </summary>
    public string Getfilepath
    {
        get => Path.Combine(Application.persistentDataPath, "userData.json");
    }
    #endregion

    #region 数据初始化方法

    /// <summary>
    /// 加载用户数据
    /// </summary>
    public void LoadData()
    {
        string filePath = Getfilepath;

        if (!File.Exists(filePath))
        {
            Debug.LogWarning("未找到用户数据文件，使用默认数据初始化");
            InitData();
            return;
        }

        try
        {
            string encryptedJson = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            // 解密
            string json = SecurityProvider.RestoreData(encryptedJson);

            Debug.Log($"加载用户数据: {json}");
            UserData loadedData = JsonConvert.DeserializeObject<UserData>(json);

            if (loadedData.LevelIndex <= 0)
            {
                Debug.LogError($"关卡数据异常: {json}");
                InitData();
                //AnalyticMgr.BugRecord("关卡存档异常",json);
                return;
            }

            InitData(loadedData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"加载用户数据异常: {ex.Message}");
            InitData();
        }
    }

    /// <summary>
    /// 保存用户数据
    /// </summary>
    public void SaveData()
    {
        try
        {
            if (LevelIndex <= 0) return;

            // 序列化并加密数据
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            string encryptedJson = SecurityProvider.ProtectData(json);

            // 写入文件
            File.WriteAllText(Getfilepath, encryptedJson);
            Debug.Log("用户数据保存成功");
        }
        catch (Exception ex)
        {
            Debug.LogError($"保存用户数据失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 初始化为默认数据
    /// </summary>
    public void InitData()
    {
        // 用户基础数据
        PlayerId = null;
        LevelIndex = AppGameSettings.FirstLevel;
        Gold = AppGameSettings.StartingGold;

        // 系统设置
        IsMusicOn = true;
        IsSoundOn = true;
        IsAgreePrivacy = false;

        // 道具初始数量为1
        RemoveToolCount = 1;
        FlipToolCount = 1;
        ShuffleToolCount = 1;
    }

    /// <summary>
    /// 从现有用户数据初始化
    /// </summary>
    /// <param name="user">源用户数据</param>
    public void InitData(UserData user)
    {
        if (user == null) return;

        // 基础数据
        PlayerId = user.PlayerId;
        Gold = user.Gold;
        LevelIndex = user.LevelIndex;
        IsMusicOn = user.IsMusicOn;
        IsSoundOn = user.IsSoundOn;
        IsAgreePrivacy = user.IsAgreePrivacy;

        // 道具数量
        RemoveToolCount = user.RemoveToolCount;
        FlipToolCount = user.FlipToolCount;
        ShuffleToolCount = user.ShuffleToolCount;
    }

    /// <summary>
    /// 更新关卡进度
    /// </summary>
    /// <param name="value">变化值</param>
    /// <param name="isSet">是否直接设置值</param>
    public void UpdateLevelIndex(int value = 1, bool isSet = false)
    {
        LevelIndex = isSet ? value : LevelIndex + value;
    }

    #endregion

    #region 道具统一管理方法

    /// <summary>
    /// 增加指定道具的数量（自动保证不小于0）
    /// </summary>
    /// <param name="type">道具类型</param>
    /// <param name="delta">变化量（正数增加，负数减少）</param>
    public void AddToolCount(ToolType type, int delta)
    {
        switch (type)
        {
            case ToolType.Remove:
                RemoveToolCount = Mathf.Max(0, RemoveToolCount + delta);
                break;
            case ToolType.Reverse:
                FlipToolCount = Mathf.Max(0, FlipToolCount + delta);
                break;
            case ToolType.Shuffle:
                ShuffleToolCount = Mathf.Max(0, ShuffleToolCount + delta);
                break;
            // case ToolType.Reset:
            // case ToolType.Hint:
            // case ToolType.Butterfly:
            //     // 如果需要支持这些道具，可以继续扩展
            //     Debug.LogWarning($"道具 {type} 暂未实现数量管理");
            //     break;
            default:
                Debug.LogError($"未知的道具类型: {type}");
                break;
        }
        SaveData(); // 每次修改后自动保存
    }

    /// <summary>
    /// 获取指定道具的当前数量
    /// </summary>
    public int GetToolCount(ToolType type)
    {
        return type switch
        {
            ToolType.Remove => RemoveToolCount,
            ToolType.Reverse => FlipToolCount,
            ToolType.Shuffle => ShuffleToolCount,
            _ => 0
        };
    }

    /// <summary>
    /// 设置指定道具的精确数量
    /// </summary>
    public void SetToolCount(ToolType type, int count)
    {
        count = Mathf.Max(0, count);
        switch (type)
        {
            case ToolType.Remove:
                RemoveToolCount = count;
                break;
            case ToolType.Reverse:
                FlipToolCount = count;
                break;
            case ToolType.Shuffle:
                ShuffleToolCount = count;
                break;
            default:
                Debug.LogWarning($"无法设置道具 {type} 的数量");
                return;
        }
        SaveData();
    }

    /// <summary>
    /// 使用一个道具（数量减1）
    /// </summary>
    /// <returns>是否使用成功（数量充足）</returns>
    public bool UseTool(ToolType type)
    {
        int current = GetToolCount(type);
        if (current <= 0) return false;

        AddToolCount(type, -1);
        return true;
    }

    #endregion
}