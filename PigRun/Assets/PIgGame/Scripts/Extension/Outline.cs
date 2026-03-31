using UnityEngine;

/// <summary>
/// 简单的轮廓效果组件
/// </summary>
public class Outline : MonoBehaviour
{
    public Color OutlineColor = Color.yellow;
    public float OutlineWidth = 5f;
    
    private Material outlineMaterial;
    private Renderer targetRenderer;
    private Material originalMaterial;
    
    void Start()
    {
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            originalMaterial = targetRenderer.material;
            
            // 创建轮廓材质
            outlineMaterial = new Material(Shader.Find("Sprites/Default"));
            outlineMaterial.color = OutlineColor;
        }
    }
    
    void OnEnable()
    {
        if (targetRenderer != null && outlineMaterial != null)
        {
            // 这里可以根据需要实现真正的轮廓效果
            // 简单实现：改变材质颜色
            targetRenderer.material.color = OutlineColor;
        }
    }
    
    void OnDisable()
    {
        if (targetRenderer != null && originalMaterial != null)
        {
            targetRenderer.material = originalMaterial;
        }
    }
}