using DG.Tweening;
//using Hot2;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThreePeakGame
{

    public class SceneMask : MonoBehaviour
    {

        [SerializeField] CanvasGroup group;

        [SerializeField] Text PowerCount;

        [SerializeField] Text ReduceCount;

        Material mat => transform.GetComponent<Image>().material;
        //Material mat;

        void OnEnable()
        {
            group.gameObject.Hide();
            group.alpha = 0;
            ReduceCount.SetAlpha(0);
        }

        public void EnterGameScene(int cost = -1)
        {

            OnEnable();
            
            transform.GetComponent<CanvasGroup>().alpha = 1;
            
            //PowerCount.text = (PowerRoot.self.energySave.GamePower - cost).ToString();
            ReduceCount.text = "-" + Mathf.Abs(cost);

            Sequence sequence = DOTween.Sequence();
            // 修改 EnterGameScene 中的第一句动画
            sequence.Append(mat.DOFloat(8f, "_Float0", 0.75f).From(0).SetEase(Ease.InSine));
            sequence.Append(group.DOFade(1, 0.5f).From(0).SetEase(Ease.Linear).OnStart(() =>
            {
                group.gameObject.Show();
                SceneManager.LoadScene("Level");
            }));
            // sequence.AppendInterval(0.1f);
            sequence.Append(mat.DOFloat(0, "_Float0", 0.75f).From(8f).SetEase(Ease.OutSine));
            sequence.AppendCallback(() =>
            {
                UIManager.Instance.ShowPanel(PanelType.MenuPanel);
                gameObject.Hide();
            });
        }

        public void BackHomeScene()
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Append(mat.DOFloat(8f, "_Float0", 0.75f).From(0).SetEase(Ease.InSine).OnComplete(() =>
            {
                //TrackEventSenderTemplate.SendStageEndEvent(SaveManager.levelData.CurrentLevel);
                SceneManager.LoadScene("Home");
                //LoaderHome();
            }));
            sequence.AppendInterval(0.1f);
            sequence.Append(mat.DOFloat(0, "_Float0", 0.75f).From(8f).SetEase(Ease.OutSine));
            sequence.AppendCallback(() => gameObject.Hide());
        }

        //调用加载器
        public void LoaderHome()
        {
            //Hot2LoadAB2File.LoadLocalAB2File(Hot2.Hot2AESTool.GetHXFileName("HomeUIBase"));
            //Hot2LoadAB2File.LoadLocalAB2File(Hot2.Hot2AESTool.GetHXFileName("SpritesCommon"));
            //Hot2LoadAB2File.LoadLocalAB2File(Hot2.Hot2AESTool.GetHXFileName("Sprites"));
            // 加载目标的热更的启动包
            SceneManager.LoadScene("Home");
        }

    }
}
