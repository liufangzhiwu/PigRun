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

#region 数据结构定义

// /// <summary>
// /// 道具类型枚举
// /// </summary>
// public enum ToolType 
// { 
//     Reset,     // 重置道具
//     Hint,      // 提示道具
//     Butterfly, // 蝴蝶道具
//     Null       // 空类型
// }

#endregion

/// <summary>
/// 用户游戏数据管理类
/// 负责处理用户数据的加载、保存、初始化及日常管理
/// 使用JSON序列化和加密存储用户数据
/// </summary>
public class UserData
{
    #region 用户基础数据
    public string PlayerId;              // 玩家ID
    public int Gold;                // 当前金币数量
    public int LevelIndex;                // 关卡序号

    #endregion

    #region 系统设置数据

    public bool IsMusicOn = true;       // 背景音乐开关
    public bool IsSoundOn = true;        // 音效开关
    //public bool IsVibrationOn ;    // 震动反馈开关
    public bool IsAgreePrivacy;    // 同意用户隐私协议
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
            //解密
            string json = SecurityProvider.RestoreData(encryptedJson);
            
            Debug.Log($"加载用户数据: {json}");
            UserData loadedData = JsonConvert.DeserializeObject<UserData>(json);
                
            if (loadedData.LevelIndex <=0)
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
            if(LevelIndex<=0) return;
            
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
    /// 加载用户数据
    /// </summary>
    public void InitData()
    {
        // 基础数据
        // 用户基础数据
        PlayerId = null;
        LevelIndex = AppGameSettings.FirstLevel;
        Gold = AppGameSettings.StartingGold;
        IsMusicOn = true;
        IsSoundOn = true;
        IsAgreePrivacy = false;
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
        LevelIndex=user.LevelIndex;
        IsMusicOn = user.IsMusicOn;
        IsSoundOn = user.IsSoundOn;
        IsAgreePrivacy = user.IsAgreePrivacy;
     
    }


    public void UpdateLevelIndex()
    {
        LevelIndex += 1;
    }

    #endregion

}