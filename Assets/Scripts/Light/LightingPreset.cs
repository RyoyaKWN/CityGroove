using UnityEngine;

/// <summary>
/// ライティングの設定を管理するScriptableObject
/// このファイルを右クリック→Create→CityGroove→LightingPresetで作成できます
/// </summary>
[CreateAssetMenu(menuName = "CityGroove/LightingPreset")]
public class LightingPreset : ScriptableObject
{
    [Header("Intensity")]
    /// <summary>ライトの基本強度（常に適用される強度）</summary>
    public float baseIntensity = 1.2f;
    /// <summary>低周波数帯域の強度ゲイン（低音が強くなるほど明るく）</summary>
    public float lowGain = 30f;
    /// <summary>ビート検出時に瞬間的に加算される強度</summary>
    public float beatFlash = 2.0f;
    /// <summary>フラッシュ効果の減衰速度（大きいほど早く消える）</summary>
    public float flashDecay = 5f;
    /// <summary>ビート強調効果の減衰速度（フラッシュより長続き）</summary>
    public float beatEmphasisDecay = 8f;

    [Header("Emission")]
    /// <summary>マテリアルエミッションの基本強度</summary>
    public float emissionBase = 1.0f;
    /// <summary>マテリアルエミッションのゲイン倍率</summary>
    public float emissionGain = 8f;

    [Header("Color (HSV)")]
    /// <summary>基本色相（0-1の範囲、0=赤、0.33=緑、0.66=青）</summary>
    [Range(0f, 1f)] public float baseHue = 0.6f;
    /// <summary>高周波数帯域による色相変化速度</summary>
    public float hueSpeed = 0.1f;
    /// <summary>彩度（0=白、1=純色）</summary>
    [Range(0f, 1f)] public float sat = 0.9f;
    /// <summary>明度（0=黒、1=通常、2=明るい）</summary>
    [Range(0f, 2f)] public float val = 1.0f;

    [Header("Response Curves")]
    /// <summary>低周波数帯域から強度への変換カーブ（AnimationCurveで調整）</summary>
    public AnimationCurve lowToIntensity = AnimationCurve.EaseInOut(0, 0, 1, 1);
    /// <summary>高周波数帯域から色相変化速度への変換カーブ</summary>
    public AnimationCurve highToHueSpeed = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Advanced Settings")]
    /// <summary>動的閾値の感度（大きいほど音の変化に敏感）</summary>
    [Range(0.5f, 3f)] public float dynamicThresholdSensitivity = 2f;
    /// <summary>変化量検出の感度（大きいほど急激な変化に反応）</summary>
    [Range(10f, 200f)] public float deltaSensitivity = 100f;
    /// <summary>ビート強調効果の強度倍率</summary>
    [Range(0.1f, 2f)] public float beatEmphasisMultiplier = 0.5f;
}
