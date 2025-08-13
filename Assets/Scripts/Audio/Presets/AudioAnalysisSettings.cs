using UnityEngine;

/// <summary>
/// 音解析のチューニング値をアセット（Inspector）で管理するための設定SO
/// </summary>
[CreateAssetMenu(menuName = "CityGroove/AudioAnalysisSettings")]
public class AudioAnalysisSettings : ScriptableObject
{
    [Header("FFT")]
    /// <summary>FFT(高速フーリエ変換)のサンプル数</summary>
    [Min(256)] public int fftSize = 1024;
    /// <summary>FFTの窓関数</summary>
    public FFTWindow window = FFTWindow.BlackmanHarris;

    [Header("Bands (Hz)")]
    /// <summary>低域帯の最大境界値</summary>
    [Min(50)] public int lowMaxHz = 200;
    /// <summary>中域帯の最大境界値</summary>
    [Min(1000)] public int midMaxHz = 2000;

    [Header("Beat Detection")]
    /// <summary>ビート検出の閾値倍率</summary>
    [Range(0.5f, 3f)] public float fluxThresholdMul = 1.5f;
    /// <summary>連打防止</summary>
    [Range(0.05f, 0.3f)] public float beatCooldown = 0.12f;
    [Range(16, 96)] public int fluxHistory = 43;

    [Header("Post Processing")]
    /// <summary>ノイズ抑制や平滑化用</summary>
    public AnimationCurve bandResponse = AnimationCurve.Linear(0, 1, 1, 1);
}
