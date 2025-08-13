// Assets/Scripts/Audio/AudioAnalyzer.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// オーディオスペクトラムを解析してビート検出と周波数帯域分析を行うコンポーネント
/// AudioSourceコンポーネントが必要です
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioAnalyzer : MonoBehaviour
{
    /// <summary>音声解析の設定パラメータ</summary>
    [SerializeField] private AudioAnalysisSettings settings;

    /// <summary>ビート検出時に発火するイベント</summary>
    [System.Serializable] public class BeatEvent : UnityEvent { }
    public BeatEvent OnBeat;

    // 解析結果（読み取り専用プロパティ）
    /// <summary>低周波数帯域（通常200Hz以下）の強度</summary>
    public float Low { get; private set; }
    /// <summary>中周波数帯域（通常200Hz-2000Hz）の強度</summary>
    public float Mid { get; private set; }
    /// <summary>高周波数帯域（通常2000Hz以上）の強度</summary>
    public float High { get; private set; }
    /// <summary>スペクトラムフラックス（音の変化量）</summary>
    public float Flux { get; private set; }

    // 内部変数
    /// <summary>音声ソースコンポーネント</summary>
    AudioSource src;
    /// <summary>現在のスペクトラムデータ</summary>
    float[] spectrum;
    /// <summary>前フレームのスペクトラムデータ（フラックス計算用）</summary>
    float[] prevSpec;
    /// <summary>フラックス履歴バッファ（移動平均計算用）</summary>
    float[] fluxBuf;
    /// <summary>フラックスバッファの現在位置</summary>
    int fluxIdx;
    /// <summary>最後にビートを検出した時間（連続検出防止）</summary>
    float lastBeatTime; 

    void Awake()
    {
        // AudioSourceコンポーネントを取得
        src = GetComponent<AudioSource>();

        // FFTサイズに基づいてバッファを初期化
        int n = Mathf.Max(256, settings.fftSize);
        spectrum = new float[n];
        prevSpec = new float[n];
        fluxBuf = new float[Mathf.Clamp(settings.fluxHistory, 16, 256)];
    }

    void Update()
    {
        if (settings == null) return;

        // FFTでスペクトラムデータを取得
        src.GetSpectrumData(spectrum, 0, settings.window);

        // 周波数帯域をLow/Mid/Highに分割（ナイキスト定理を使用してbin数を計算）
        float nyquist = AudioSettings.outputSampleRate * 0.5f;
        int n = spectrum.Length;
        int lowMaxBin = Mathf.Clamp(Mathf.RoundToInt(settings.lowMaxHz / nyquist * n), 1, n - 2);
        int midMaxBin = Mathf.Clamp(Mathf.RoundToInt(settings.midMaxHz / nyquist * n), lowMaxBin + 1, n - 1);

        // 各周波数帯域の強度を計算
        float low = 0, mid = 0, high = 0;
        for (int i = 0; i < n; i++)
        {
            float v = spectrum[i];
            if (i <= lowMaxBin) low += v;
            else if (i <= midMaxBin) mid += v;
            else high += v;
        }

        // スペクトラムフラックスの計算
        // スペクトラムフラックス：前フレームとの差分の合計（音の変化量を表す）
        float flux = 0f;
        for (int i = 0; i < n; i++)
        {
            float diff = spectrum[i] - prevSpec[i];

            // 正の差分のみを採用（音の増加のみを検出）
            if (diff > 0) flux += diff;

            // 現在値を次フレーム用に保存
            prevSpec[i] = spectrum[i];
        }

        // 各解析結果を更新
        Low = low;
        Mid = mid;
        High = high;
        Flux = flux;

        // フラックスの移動平均を計算し、閾値と比較してビートイベントを検出
        fluxBuf[fluxIdx] = flux; 
        fluxIdx = (fluxIdx + 1) % fluxBuf.Length;

        // 移動平均を計算
        float avg = 0f;
        for (int i = 0; i < fluxBuf.Length; i++) avg += fluxBuf[i];
        avg /= fluxBuf.Length;

        // 閾値を設定し、ビートを検出
        float threshold = avg * settings.fluxThresholdMul + 0.0005f;    // 最小閾値
        if (flux > threshold && Time.time - lastBeatTime > settings.beatCooldown)
        {
            lastBeatTime = Time.time;
            OnBeat?.Invoke();   // イベントを発火
        }
    }
}
