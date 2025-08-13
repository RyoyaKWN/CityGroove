using UnityEngine;

[CreateAssetMenu(menuName = "CityGroove/LightingPreset")]
public class LightingPreset : ScriptableObject
{
    [Header("Intensity")]
    /// <summary>明るさのベース値</summary>
    public float baseIntensity = 1.2f;
    /// <summary>Lowが強いほど上乗せ</summary>
    public float lowGain = 30f;
    /// <summary>オンビート時に瞬間的に上乗せ</summary>
    public float beatFlash = 2.0f;
    /// <summary>フラッシュの減衰速度</summary>
    public float flashDecay = 5f;

    [Header("Emission")]
    /// <summary>ネオンの発光ベース</summary>
    public float emissionBase = 1.0f;
    /// <summary>ネオンの発光ゲイン</summary>
    public float emissionGain = 8f;

    [Header("Color (Hue)")]
    /// <summary>色相</summary>
    [Range(0f, 1f)] public float baseHue = 0.6f;
    public float hueSpeed = 0.1f; // Highに乗算して回転速度を設定
    /// <summary>彩度</summary>
    [Range(0f, 1f)] public float sat = 0.9f;
    /// <summary>明度</summary>
    [Range(0f, 2f)] public float val = 1.0f;

    [Header("Response Curves")]
    /// <summary>AnmationCurveで反応のカーブ</summary>
    public AnimationCurve lowToIntensity = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve highToHueSpeed = AnimationCurve.Linear(0, 0, 1, 1);
}
