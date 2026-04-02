using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System;
using Object = UnityEngine.Object;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 自定义扩展方法
/// </summary>
public static class CusExpand
{

    #region -------------------------------------------------------------UI相关-------------------------------------------------------------

    public static void Show(this GameObject gameObject)
    {
        gameObject.SetActive(true);
    }

    public static void Hide(this GameObject gameObject)
    {
        gameObject.SetActive(false);
    }

    public static void Show(this Transform transform)
    {
        transform.gameObject.SetActive(true);
    }

    public static void Hide(this Transform transform)
    {
        transform.gameObject.SetActive(false);
    }

    public static void Show(this RectTransform rectTransform)
    {
        rectTransform.gameObject.SetActive(true);
    }

    public static void Hide(this RectTransform rectTransform)
    {
        rectTransform.gameObject.SetActive(false);
    }

    public static void Show(this Button button)
    {
        button.gameObject.SetActive(true);
    }

    public static void Hide(this Button button)
    {
        button.gameObject.SetActive(false);
    }

    public static void Show(this Text text)
    {
        text.transform.gameObject.SetActive(true);
    }

    public static void Hide(this Text text)
    {
        text.transform.gameObject.SetActive(false);
    }

    public static void Show(this Image image)
    {
        image.gameObject.SetActive(true);
    }

    public static void Hide(this Image image)
    {
        image.gameObject.SetActive(false);
    }

    public static bool IsShow(this GameObject gameObject)
    {
        return gameObject.activeSelf;
    }

    public static bool IsShow(this Transform transform)
    {
        return transform.gameObject.activeSelf;
    }

    public static bool IsShow(this RectTransform rectTransform)
    {
        return rectTransform.gameObject.activeSelf;
    }

    public static bool IsShow(this Text text)
    {
        return text.gameObject.activeSelf;
    }

    public static bool IsShow(this Button button)
    {
        return button.gameObject.activeSelf;
    }

    public static GameObject FindObject(this GameObject parent, string name)
    {
        return parent.transform.Find(name) != null ? parent.transform.Find(name).gameObject : null;
    }

    public static List<Transform> GetChildren(this Transform transform)
    {
        var list = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            list.Add(transform.GetChild(i));
        }

        return list;
    }

    public static void ShowAllChildren(this Transform transform)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
        }
    }

    public static void HideAllChildren(this Transform transform)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
    }

    public static void Destroy(this GameObject gameObject)
    {
        Object.Destroy(gameObject);
    }

    public static void DestroyAllChildren(this Transform transform)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Object.Destroy(transform.GetChild(i).gameObject);
        }
    }

    public static void SetImageSizeByWidth(this Image image, float width)
    {
        image.SetNativeSize();
        var rect = image.GetComponent<RectTransform>().rect;
        float ratio = rect.width / rect.height;
        float height = width / ratio;
        image.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
    }

    public static void SetImageSizeByHeight(this Image image, float height)
    {
        image.SetNativeSize();
        var rect = image.GetComponent<RectTransform>().rect;
        float ratio = rect.height / rect.width;
        float width = height / ratio;
        image.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
    }

    /// <summary>
    /// 给予图片一个尺寸，将按照图片原长宽比例尽可能在该限制尺寸下放大
    /// </summary>
    /// <param name="image"></param>
    /// <param name="size"></param>
    public static void SetImageScaleMaxSize(this Image image, Vector2 size)
    {
        image.SetNativeSize();
        var rect = image.GetComponent<RectTransform>().rect;
        float ratio = rect.width / rect.height;
        float constrainRatio = size.x / size.y;
        if (ratio >= constrainRatio)//width较长，按宽度适配
        {
            float height = size.x / ratio;
            image.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, height);
        }
        else
        {
            float width = ratio * size.y;
            image.GetComponent<RectTransform>().sizeDelta = new Vector2(width, size.y);
        }
    }

    public static float CalcTextWidth(this Text text)
    {
        TextGenerator tg = text.cachedTextGeneratorForLayout;
        TextGenerationSettings setting = text.GetGenerationSettings(Vector2.zero);
        float width = tg.GetPreferredWidth(text.text, setting) / text.pixelsPerUnit;
        return width;
    }

    public static Vector2 GetSize(this Transform transform)
    {
        return transform.GetComponent<RectTransform>().sizeDelta;
    }

    public static void SetSize(this Transform transform, Vector2 size)
    {
        transform.GetComponent<RectTransform>().sizeDelta = size;
    }

    public static void SetSize(this Image image, Vector2 size)
    {
        image.GetComponent<RectTransform>().sizeDelta = size;
    }

    public static void SetWidth(this Transform transform, float width)
    {
        transform.GetComponent<RectTransform>().sizeDelta = new Vector2(width, transform.GetComponent<RectTransform>().sizeDelta.y);
    }

    public static void SetHeight(this Transform transform, float height)
    {
        transform.GetComponent<RectTransform>().sizeDelta = new Vector2(transform.GetComponent<RectTransform>().sizeDelta.x, height);
    }

    public static void SetLocalPosX(this Transform transform, float posX)
    {
        transform.localPosition = new Vector2(posX, transform.localPosition.y);
    }

    public static void SetLocalPosY(this Transform transform, float posY)
    {
        transform.localPosition = new Vector2(transform.localPosition.x, posY);
    }

    public static Vector3 SetZ(this Vector3 vector, float z)
    {
        return new Vector3(vector.x, vector.y, z);
    }

    public static void SetAnchoredPosition(this Transform transform, Vector2 anchoredPosition)
    {
        transform.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
    }

    /// <summary>
    /// 适用于2D平面的
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="angleZ"></param>
    public static void SetLocalEulerAngles(this Transform transform, float angleZ)
    {
        transform.localEulerAngles = new Vector3(0, 0, angleZ);
    }

    public static Tween DOLocalRotateZ(this Transform transform, float angleZ, float duration)
    {
        return transform.DOLocalRotate(new Vector3(0, 0, angleZ), duration, RotateMode.FastBeyond360);
    }


    public static Tween DORotateZ(this Transform transform, float angleZ, float duration)
    {
        return transform.DORotate(new Vector3(0, 0, angleZ), duration, RotateMode.FastBeyond360);
    }

    public static void SetAlpha(this Text text, float alpha)
    {
        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    public static void SetAlpha(this Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    #endregion

    #region -------------------------------------------------------------DOTWEEN动画相关------------------------------------------------------------------

    /// <summary>
    /// 播放旋转一周动画
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="anglePerSecond"></param>
    /// <param name="forward"></param>
    /// <param name="loop"></param>
    /// <returns></returns>
    public static Tween PlayRotateAnimation(this Transform transform, float anglePerSecond = 30f, bool forward = true, bool loop = true)
    {
        float fullAngle = forward ? -360 : 360; //顺时针还是逆时针
        float duration = 360 / anglePerSecond;  //根据每秒的自旋转角度计算时间
        return transform.DORotate(new Vector3(0, 0, fullAngle), duration, RotateMode.LocalAxisAdd).SetLoops(loop ? -1 : 1).SetEase(Ease.Linear);
    }

    /// <summary>
    /// 播放缩放动画
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="startScale"></param>
    /// <param name="endScale"></param>
    /// <param name="duration"></param>
    /// <param name="loop"></param>
    /// <returns></returns>
    public static Tween PlayScaleAnimation(this Transform transform, Vector3 startScale, Vector3 endScale, float duration, bool yoyo = true, bool loop = true)
    {
        transform.localScale = startScale;
        return transform.DOScale(endScale, duration).SetEase(Ease.Linear).SetLoops(loop ? -1 : 1, yoyo ? LoopType.Restart : LoopType.Yoyo);
    }

    /// <summary>
    /// 渐显
    /// </summary>
    /// <param name="image"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public static Tween FadeIn(this Image image, float duration)
    {
        image.color = new Color(1, 1, 1, 0);
        return image.DOFade(1, duration).SetEase(Ease.Linear).SetAutoKill(true);
    }

    /// <summary>
    /// 渐隐
    /// </summary>
    /// <param name="image"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public static Tween FadeOut(this Image image, float duration)
    {
        image.color = new Color(1, 1, 1, 1);
        return image.DOFade(0, duration).SetEase(Ease.Linear).SetAutoKill(true);
    }

    /// <summary>
    /// 闪烁动画
    /// </summary>
    /// <param name="image"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public static Tween Flash(this Image image, float duration, float startAlpha = 0)
    {
        return image.DOFade(1, duration).From(startAlpha).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
    }


    public static Tween Shake(this Transform transform, float duration = 2f)
    {
        return transform.DOPunchRotation(new Vector3(0, 0, 20), duration, 3).SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart);
    }

    public static Tween Float(this Transform transform, float range, float duration = 2f)
    {
        return transform.DOLocalMoveY(transform.localPosition.y + range, duration).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
    }

    #endregion

    #region -------------------------------------------------------------数据相关-------------------------------------------------------------

    /// <summary>
    /// 字符串转换为整型数组
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static int[] ToIntArray(this string str, char separator = ',')
    {
        string[] strArray = str.Split(separator);
        int[] intArray = new int[strArray.Length];
        for (int i = 0; i < strArray.Length; i++)
        {
            intArray[i] = int.Parse(strArray[i]);
        }

        return intArray;
    }

    /// <summary>
    /// 字符串转换为浮点数组
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static float[] ToFloatArray(this string str, char separator = ',')
    {
        string[] strArray = str.Split(separator);
        float[] floatArray = new float[strArray.Length];
        for (int i = 0; i < strArray.Length; i++)
        {
            floatArray[i] = float.Parse(strArray[i]);
        }

        return floatArray;
    }

    #endregion

}