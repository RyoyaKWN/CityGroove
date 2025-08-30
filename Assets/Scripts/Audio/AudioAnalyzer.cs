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
    /// <summary>低周波数帯域の変化量（ビート検出の補助）</summary>
    public float LowDelta { get; private set; }

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
    
    /// <summary>低周波数帯域履歴（ビート検出用）</summary>
    float[] lowHistory;
    /// <summary>低周波数帯域履歴の現在位置</summary>
    int lowHistoryIdx;
    /// <summary>低周波数帯域の移動平均</summary>
    float lowAverage;
    /// <summary>前フレームの低周波数帯域値</summary>
    float prevLow;
    /// <summary>ビート検出の複合スコア</summary>
    float beatScore;
    /// <summary>ビート検出の閾値</summary>
    float beatThreshold = 0.6f;

    void Awake()
    {
        // AudioSourceコンポーネントを取得
        src = GetComponent<AudioSource>();

        // FFTサイズに基づいてバッファを初期化
        int n = Mathf.Max(256, settings.fftSize);
        spectrum = new float[n];
        prevSpec = new float[n];
        fluxBuf = new float[Mathf.Clamp(settings.fluxHistory, 16, 256)];
        
        // ビート検出システムの初期化
        lowHistory = new float[60]; // 1秒間の履歴（60FPS想定）
        lowHistoryIdx = 0;
        lowAverage = 0f;
        prevLow = 0f;
        beatScore = 0f;
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

        // ビート検出システム
        UpdateBeatDetection(low, flux);
    }

    /// <summary>
    /// ビート検出システム
    /// </summary>
    void UpdateBeatDetection(float low, float flux)
    {
        // 低周波数帯域の変化量を計算
        LowDelta = low - prevLow;
        prevLow = low;

        // 低周波数帯域履歴を更新
        lowHistory[lowHistoryIdx] = low;
        lowHistoryIdx = (lowHistoryIdx + 1) % lowHistory.Length;

        // 低周波数帯域の移動平均を計算
        float sum = 0f;
        for (int i = 0; i < lowHistory.Length; i++)
        {
            sum += lowHistory[i];
        }
        lowAverage = sum / lowHistory.Length;

        // フラックスの移動平均を計算
        fluxBuf[fluxIdx] = flux;
        fluxIdx = (fluxIdx + 1) % fluxBuf.Length;

        float fluxSum = 0f;
        for (int i = 0; i < fluxBuf.Length; i++) fluxSum += fluxBuf[i];
        float fluxAverage = fluxSum / fluxBuf.Length;

        // 複合ビート検出スコアを計算
        float lowRelative = 0f;
        if (lowAverage > 0.001f)
        {
            lowRelative = Mathf.Clamp01((low / lowAverage - 1f) * 2f);
        }

        float fluxRelative = 0f;
        if (fluxAverage > 0.001f)
        {
            fluxRelative = Mathf.Clamp01((flux / fluxAverage - 1f) * 1.5f);
        }

        float deltaRelative = Mathf.Clamp01(Mathf.Abs(LowDelta) * 50f);

        // 複合スコア（低周波数帯域の突出 + フラックスの突出 + 変化量）
        beatScore = (lowRelative * 0.4f + fluxRelative * 0.4f + deltaRelative * 0.2f);

        // ビート検出の閾値チェック
        float threshold = beatThreshold;
        if (fluxAverage > 0.001f)
        {
            // フラックス平均に基づいて動的閾値を調整
            threshold = Mathf.Max(0.3f, Mathf.Min(0.8f, fluxAverage * 10f));
        }

        // ビート検出条件
        bool isBeat = beatScore > threshold && 
                     Time.time - lastBeatTime > settings.beatCooldown &&
                     low > lowAverage * 1.2f; // 低周波数帯域が平均より20%以上高い

        if (isBeat)
        {
            lastBeatTime = Time.time;
            OnBeat?.Invoke();   // イベントを発火
        }
    }

    /// <summary>
    /// デバッグ用：現在のビート検出状態を取得
    /// </summary>
    public float GetBeatScore() => beatScore;
    
    /// <summary>
    /// デバッグ用：現在のビート閾値を取得
    /// </summary>
    public float GetBeatThreshold() => beatThreshold;
    
    /// <summary>
    /// デバッグ用：ビート閾値を設定
    /// </summary>
    public void SetBeatThreshold(float threshold) => beatThreshold = Mathf.Clamp01(threshold);
}
