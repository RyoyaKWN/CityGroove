// Assets/Scripts/Audio/AudioAnalyzer.cs
using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(AudioSource))]
public class AudioAnalyzer : MonoBehaviour
{
    /// <summary>音解析の設定SO</summary>
    [SerializeField] private AudioAnalysisSettings settings;

    [System.Serializable] public class BeatEvent : UnityEvent { }
    /// <summary>ビートを外部に通知するイベント</summary>
    public BeatEvent OnBeat;

    // 解析結果（読み取り専用）
    public float Low { get; private set; }
    public float Mid { get; private set; }
    public float High { get; private set; }
    public float Flux { get; private set; }

    /// <summary>オーディオソース</summary>
    AudioSource src;
    /// <summary>現在のスペクトラム</summary>
    float[] spectrum;
    /// <summary>前回のスペクトラム</summary>
    float[] prevSpec;
    /// <summary>移動平均用バッファ</summary>
    float[] fluxBuf;
    /// <summary>リンクバッファの書き込み位置</summary>
    int fluxIdx;
    /// <summary>直近ビートの時間（連打防止）</summary>
    float lastBeatTime; 

    void Awake()
    {
        // オーディオソース読み込み
        src = GetComponent<AudioSource>();

        // FFTサイズ、履歴長を初期化
        int n = Mathf.Max(256, settings.fftSize);
        spectrum = new float[n];
        prevSpec = new float[n];
        fluxBuf = new float[Mathf.Clamp(settings.fluxHistory, 16, 256)];
    }

    void Update()
    {
        if (settings == null) return;

        // FFTで周波数成分を取得
        src.GetSpectrumData(spectrum, 0, settings.window);

        // 帯域をLow/Mid/Highに積分（境界はNyquist周波数からbin換算）
        float nyquist = AudioSettings.outputSampleRate * 0.5f;
        int n = spectrum.Length;
        int lowMaxBin = Mathf.Clamp(Mathf.RoundToInt(settings.lowMaxHz / nyquist * n), 1, n - 2);
        int midMaxBin = Mathf.Clamp(Mathf.RoundToInt(settings.midMaxHz / nyquist * n), lowMaxBin + 1, n - 1);

        // 帯域積分
        float low = 0, mid = 0, high = 0;
        for (int i = 0; i < n; i++)
        {
            float v = spectrum[i];
            if (i <= lowMaxBin) low += v;
            else if (i <= midMaxBin) mid += v;
            else high += v;
        }

        // スペクトルフラックスの計算
        // スペクトルフラックス：前フレームより増えた分の合計
        float flux = 0f;
        for (int i = 0; i < n; i++)
        {
            float diff = spectrum[i] - prevSpec[i];

            // 正の差分のみ採用
            if (diff > 0) flux += diff;

            // 次回比較用に保存
            prevSpec[i] = spectrum[i];
        }

        // 各解析結果を反映
        Low = low;
        Mid = mid;
        High = high;
        Flux = flux;

        // フラックスの移動平均*倍率を閾値にして、超えたらビートイベントを発火
        fluxBuf[fluxIdx] = flux; 
        fluxIdx = (fluxIdx + 1) % fluxBuf.Length;

        float avg = 0f;
        for (int i = 0; i < fluxBuf.Length; i++) avg += fluxBuf[i];
        avg /= fluxBuf.Length;

        float threshold = avg * settings.fluxThresholdMul + 0.0005f;    // 微小オフセット
        if (flux > threshold && Time.time - lastBeatTime > settings.beatCooldown)
        {
            lastBeatTime = Time.time;
            OnBeat?.Invoke();   // 外部に通知
        }
    }
}
