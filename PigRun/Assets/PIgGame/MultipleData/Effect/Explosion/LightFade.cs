using UnityEngine;
using System.Collections;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

public class LightFade : MonoBehaviour
{

    [SerializeField] float range = 3f;
    [SerializeField] float intensity = 10f;
    [SerializeField] float life = 0.5f;

    private Light li;

    private void OnEnable()
    {
        if (gameObject.GetComponent<Light>())
        {
            li = gameObject.GetComponent<Light>();
            li.intensity = intensity;
            li.range = range;
            li.enabled = true;
        }
        else
            print("No light object found on " + gameObject.name);

        li.DOIntensity(0, life).From(intensity).SetEase(Ease.Linear);

        float v = 0;
        TweenerCore<float, float, FloatOptions> t = DOTween.To(() => v, x =>
        {
            v = x;
            li.range = v;
        }, range, life / 2f);
        t.SetTarget(li);
        t.SetLoops(2, LoopType.Yoyo).OnComplete(() =>
        {
            li.enabled = false;
        });
    }

}