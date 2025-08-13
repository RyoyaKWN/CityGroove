using UnityEngine;

/// <summary>
/// 音声解析のパラメータ値をScriptableObject（Inspector）で管理するための設定ファイル
/// このファイルを右クリック→Create→CityGroove→AudioAnalysisSettingsで作成できます
/// </summary>
[CreateAssetMenu(menuName = "CityGroove/AudioAnalysisSettings")]
public class AudioAnalysisSettings : ScriptableObject
{
    [Header("FFT Settings")]
    /// <summary>FFT（高速フーリエ変換）のサンプル数（大きいほど高精度だが重い）</summary>
    [Min(256)] public int fftSize = 1024;
    /// <summary>FFTの窓関数（周波数解析の精度に影響）</summary>
    public FFTWindow window = FFTWindow.BlackmanHarris;

    [Header("Frequency Bands (Hz)")]
    /// <summary>低周波数帯域の最大値（この値以下がLow帯域）</summary>
    [Min(50)] public int lowMaxHz = 200;
    /// <summary>中周波数帯域の最大値（この値以下がMid帯域、以上がHigh帯域）</summary>
    [Min(1000)] public int midMaxHz = 2000;

    [Header("Beat Detection")]
    /// <summary>ビート検出の閾値倍率（移動平均の何倍でビートと判定するか）</summary>
    [Range(0.5f, 3f)] public float fluxThresholdMul = 1.5f;
    /// <summary>ビート検出のクールダウン時間（連続検出を防ぐ）</summary>
    [Range(0.05f, 0.3f)] public float beatCooldown = 0.12f;
    /// <summary>フラックス履歴の保存フレーム数（移動平均計算用）</summary>
    [Range(16, 96)] public int fluxHistory = 43;

    [Header("Post Processing")]
    /// <summary>周波数帯域の応答カーブ（音の強度を調整）</summary>
    public AnimationCurve bandResponse = AnimationCurve.Linear(0, 1, 1, 1);
}
