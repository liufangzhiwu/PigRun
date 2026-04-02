using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScene : MonoBehaviour
{
    public Text SliderText;          // 用于显示百分比的文本
    public Slider progressBar;
    public float minLoadTime = 5f;    // 最小加载时长（秒），进度条将在此时达到100%

    void Start()
    {
        StartCoroutine(LoadMainScene());
    }

    IEnumerator LoadMainScene()
    {
        float startTime = Time.time;
        AsyncOperation operation = SceneManager.LoadSceneAsync("Home");
        operation.allowSceneActivation = false; // 先不自动激活

        while (!operation.isDone)
        {
            float elapsed = Time.time - startTime;

            // 虚拟进度：基于时间线性增长，在minLoadTime秒后达到1（缓慢增加）
            float virtualProgress = Mathf.Clamp01(elapsed / minLoadTime);

            // 更新进度条（始终使用虚拟进度，保证平滑）
            progressBar.value = virtualProgress;

            // 更新百分比文本
            if (SliderText != null)
            {
                int percent = Mathf.RoundToInt(virtualProgress * 100f);
                SliderText.text = percent + "%";
            }

            // 当真实加载完成且达到最小时间时，允许激活场景
            if (operation.progress >= 0.9f && elapsed >= minLoadTime)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}