using System;
using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System.Text;

public class PerformanceMonitor : MonoBehaviour
{
    [Header("监控设置")]
    public bool showUI = true;               // 是否在屏幕上显示性能信息
    public bool logToConsole = false;        // 是否输出到控制台
    public float updateInterval = 0.5f;      // 数据采集间隔（秒）
    public int frameHistorySize = 60;        // 保留的帧数（用于计算平均 FPS）

    [Header("UI 设置")]
    public Vector2 uiPosition = new Vector2(10, 10);
    public int fontSize = 20;
    public Color textColor = Color.green;

    // 数据
    private float deltaTime;
    private float fps;
    private float minFps = float.MaxValue;
    private float maxFps = float.MinValue;
    private float avgFps;
    private float totalMemoryMB;
    private float usedMemoryMB;
    private float gcAllocPerFrame;
    private long lastGcTotalAlloc;

    private List<float> fpsHistory = new List<float>();
    private float nextCollectTime;
    private GUIStyle guiStyle;
    private StringBuilder displayText = new StringBuilder();

    void Start()
    {
        if (showUI)
        {
            guiStyle = new GUIStyle();
            guiStyle.fontSize = fontSize;
            guiStyle.normal.textColor = textColor;
        }

        lastGcTotalAlloc = GC.GetTotalMemory(false);
        nextCollectTime = Time.unscaledTime + updateInterval;
    }

    void Update()
    {
        // 计算实时帧率
        deltaTime = Time.unscaledDeltaTime;
        fps = 1f / deltaTime;

        // 更新 FPS 历史
        fpsHistory.Add(fps);
        while (fpsHistory.Count > frameHistorySize)
            fpsHistory.RemoveAt(0);

        // 定期采集数据
        if (Time.unscaledTime >= nextCollectTime)
        {
            CollectMetrics();
            nextCollectTime = Time.unscaledTime + updateInterval;
        }

        // 输出到控制台
        if (logToConsole && Time.frameCount % 60 == 0) // 每秒输出一次
        {
            Debug.Log(GetFormattedMetrics());
        }
    }

    void CollectMetrics()
    {
        // 计算平均 FPS
        float sum = 0;
        foreach (var f in fpsHistory) sum += f;
        avgFps = sum / fpsHistory.Count;

        // 更新极值
        if (fps < minFps) minFps = fps;
        if (fps > maxFps) maxFps = fps;

        // 内存
        totalMemoryMB = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
        usedMemoryMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);

        // GC 分配（每帧平均）
        long currentGc = GC.GetTotalMemory(false);
        gcAllocPerFrame = (currentGc - lastGcTotalAlloc) / (1024f * 1024f) / (fpsHistory.Count / frameHistorySize);
        lastGcTotalAlloc = currentGc;
    }

    string GetFormattedMetrics()
    {
        return $"[Perf] FPS: {fps:F1} (avg:{avgFps:F1} min:{minFps:F1} max:{maxFps:F1}) | " +
               $"Mem: {usedMemoryMB:F1}/{totalMemoryMB:F1} MB | " +
               $"GC: {gcAllocPerFrame:F2} KB/frame";
    }

    void OnGUI()
    {
        if (!showUI) return;

        // 构建显示文本
        displayText.Clear();
        displayText.AppendLine($"FPS: {fps:F1} (avg:{avgFps:F1})");
        displayText.AppendLine($"Mem: {usedMemoryMB:F1}/{totalMemoryMB:F1} MB");
        displayText.AppendLine($"GC: {gcAllocPerFrame:F2} KB/frame");
        displayText.AppendLine($"Min: {minFps:F1}  Max: {maxFps:F1}");

        GUI.Label(new Rect(uiPosition.x, uiPosition.y, 300, 100), displayText.ToString(), guiStyle);
    }

    // ---------- 自定义性能标记 ----------
    public static PerformanceMonitor Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 简易计时器（用于标记关键代码段）
    private Dictionary<string, float> timers = new Dictionary<string, float>();
    private Dictionary<string, float> accumulatedTimes = new Dictionary<string, float>();
    private Dictionary<string, int> callCounts = new Dictionary<string, int>();

    public void BeginSample(string name)
    {
        timers[name] = Time.realtimeSinceStartup;
    }

    public void EndSample(string name)
    {
        if (!timers.ContainsKey(name)) return;
        float duration = Time.realtimeSinceStartup - timers[name];
        if (!accumulatedTimes.ContainsKey(name))
        {
            accumulatedTimes[name] = 0;
            callCounts[name] = 0;
        }
        accumulatedTimes[name] += duration;
        callCounts[name]++;
        timers.Remove(name);
    }

    public void LogSamples()
    {
        foreach (var kv in accumulatedTimes)
        {
            float avg = kv.Value / callCounts[kv.Key];
            Debug.Log($"[Perf Sample] {kv.Key}: total={kv.Value * 1000:F2} ms, avg={avg * 1000:F2} ms, calls={callCounts[kv.Key]}");
        }
    }

    public void ResetSamples()
    {
        accumulatedTimes.Clear();
        callCounts.Clear();
    }
}