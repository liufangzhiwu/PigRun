using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif


public class GameDataManager :MonoBehaviour
{
    public static GameDataManager Instance;
    
    #region 数据字段
    private UserData userData = new UserData();
  
    
    private bool dataInitialized = false;
    private bool requireFocusCheck = false;
    private DateTime lastSaveTime;
   
    #endregion

    #region 属性
    public UserData UserData { get { return userData; } }
   
    #endregion
    

    #region Unity生命周期方法

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
    }
    
    public void Start()
    {
        LoadPlayerDatas();
    }
   

    private void OnApplicationFocus(bool focusStatus)
    {
        HandleFocusChange(focusStatus);
    }

    private void OnApplicationPause(bool pauseState)
    {
        HandlePauseState(pauseState);
    }

    private void OnApplicationQuit()
    {
        HandleQuitEvent();
    }
    #endregion

    #region 初始化方法
    
    public bool PushServerCompleted { get; private set; } = false;
    private bool OnWantsToQuit()
    {
        if (dataInitialized)
        {
            Debug.Log("应用请求关闭，保存数据中...");
            CommitGameData();
        }
        return true;
    }


    public void LoadPlayerDatas()
    {
        userData.LoadData();
        dataInitialized = true;
    }
    #endregion

    #region 数据保存

    public int SaveNumber { get; private set; } = 0;
    public void CommitGameData()
    {
        SaveNumber = 0;
        userData.SaveData();
    }
    
    
    #endregion

    #region 应用程序状态处理
    private void HandleFocusChange(bool hasFocus)
    {
        // 应用进入后台
        if (!hasFocus)
        {
            //初始化完成后才可以保存，不然保存的数据都为默认数值
            if (dataInitialized)
                CommitGameData();
           
            requireFocusCheck = true;
            Debug.Log("应用进入后台，数据已保存");
        }
        else if (requireFocusCheck)
        {
            Debug.Log("应用回到前台，验证数据");
            requireFocusCheck = false;
        }
    }

    private void HandlePauseState(bool isPaused)
    {
        if (isPaused && dataInitialized)
        {
            CommitGameData();
            Debug.Log("应用暂停，数据已保存");
        }
    }

    private void HandleQuitEvent()
    {
        if (dataInitialized)
        {
            CommitGameData();
          
            Debug.Log("应用关闭，数据已保存");
        }
    }
    #endregion

    #region 数据清理
    public void WipeAllGameData()
    {
        PurgePersistentFiles();
        
        userData.InitData();
    }

    public void PurgePersistentFiles()
    {
        string storagePath = Application.persistentDataPath;

        if (Directory.Exists(storagePath))
        {
            try
            {
                string[] allFiles = Directory.GetFiles(storagePath);
                foreach (string filePath in allFiles)
                {
                    File.Delete(filePath);
                    Debug.Log($"已移除文件: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"清除存储数据时出错: {ex.Message}");
            }
        }
    }
    #endregion
}