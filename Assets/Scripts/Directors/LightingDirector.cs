using UnityEngine;

/// <summary>
/// 音声解析結果に基づいてライティングを制御するディレクター
/// AudioAnalyzerの結果を受け取り、Lightコンポーネントとマテリアルのエミッションを制御します
/// </summary>
public class LightingDirector : MonoBehaviour
{
    /// <summary>音声解析コンポーネント（ビート検出と周波数分析用）</summary>
    [SerializeField] private AudioAnalyzer analyzer;
    /// <summary>ライティングの設定プリセット</summary>
    [SerializeField] private LightingPreset preset;
    /// <summary>音声に反応させたいLightコンポーネント</summary>
    [SerializeField] private Light reactiveLight;
    /// <summary>エミッションを制御したいレンダラー配列</summary>
    [SerializeField] private Renderer[] emissionTargetArray;

    // 内部状態
    /// <summary>現在の色相（0-1の範囲、HSV色空間）</summary>
    float hue;
    /// <summary>ビート検出時に加算されるフラッシュ強度</summary>
    float flashAdd;
    /// <summary>マテリアルプロパティブロック（エミッション制御用）</summary>
    MaterialPropertyBlock mpb;
    
    // 動的閾値システム用
    /// <summary>低周波数帯域の移動平均（動的閾値計算用）</summary>
    float[] lowHistory;
    /// <summary>低周波数帯域履歴の現在位置</summary>
    int lowHistoryIdx;
    /// <summary>低周波数帯域の移動平均</summary>
    float lowAverage;
    /// <summary>前フレームの低周波数帯域値（変化量計算用）</summary>
    float prevLow;
    /// <summary>低周波数帯域の変化量</summary>
    float lowDelta;
    
    // ビート強調システム用
    /// <summary>ビート強調効果の強度</summary>
    float beatEmphasis;

    void Start()
    {
        // ビート検出時にフラッシュ効果を追加
        if (analyzer != null) analyzer.OnBeat.AddListener(OnBeatDetected);
        
        // 初期色相をプリセットから設定
        hue = preset != null ? preset.baseHue : 0.5f;
        
        // マテリアルプロパティブロックを初期化
        mpb = new MaterialPropertyBlock();
        
        // 動的閾値システムの初期化
        lowHistory = new float[30]; // 0.5秒間の履歴（60FPS想定）
        lowHistoryIdx = 0;
        lowAverage = 0f;
        prevLow = 0f;
        lowDelta = 0f;
        
        // ビート強調システムの初期化
        beatEmphasis = 0f;
    }

    void Update()
    {
        if (analyzer == null || preset == null) return;

        // 動的閾値システム：低周波数帯域の移動平均を計算
        UpdateLowFrequencyAnalysis();

        // 高周波数帯域で色相を変化（AnimationCurveで調整可能）
        // float hueDelta = preset.highToHueSpeed.Evaluate(Norm(analyzer.High)) * preset.hueSpeed * Time.deltaTime;
        // hue = (hue + hueDelta) % 1f;  // 0-1の範囲に正規化
        Color c = Color.HSVToRGB(hue, preset.sat, preset.val);

        // 改善された強度計算：動的閾値と変化量を考慮
        float intensity = CalculateDynamicIntensity();
        
        // Lightコンポーネントに色と強度を適用
        if (reactiveLight)
        {
            reactiveLight.color = c;
            reactiveLight.intensity = intensity;
        }

        // マテリアルのエミッションを制御（MaterialPropertyBlock使用）
        float emiss = CalculateEmissionIntensity();

        // 各レンダラーにエミッション色を適用
        foreach (var r in emissionTargetArray)
        {
            if (!r) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", c * emiss);
            r.SetPropertyBlock(mpb);
        }
    }

    /// <summary>
    /// 低周波数帯域の動的解析を更新
    /// </summary>
    void UpdateLowFrequencyAnalysis()
    {
        // 履歴に現在値を追加
        lowHistory[lowHistoryIdx] = analyzer.Low;
        lowHistoryIdx = (lowHistoryIdx + 1) % lowHistory.Length;
        
        // 移動平均を計算
        float sum = 0f;
        for (int i = 0; i < lowHistory.Length; i++)
        {
            sum += lowHistory[i];
        }
        lowAverage = sum / lowHistory.Length;
        
        // 変化量を計算
        lowDelta = analyzer.Low - prevLow;
        prevLow = analyzer.Low;
    }

    /// <summary>
    /// 動的閾値を考慮した強度計算
    /// </summary>
    float CalculateDynamicIntensity()
    {
        // 基本強度
        float baseInt = preset.baseIntensity;
        
        // 動的閾値を考慮した低周波数帯域の影響
        float normalizedLow = 0f;
        if (lowAverage > 0.001f) // ゼロ除算防止
        {
            // 現在値が平均値に対してどれだけ突出しているかを計算
            float relativeIntensity = analyzer.Low / lowAverage;
            normalizedLow = Mathf.Clamp01((relativeIntensity - 1f) * preset.dynamicThresholdSensitivity);
        }
        
        // 変化量の影響（急激な変化を強調）
        float deltaInfluence = Mathf.Clamp01(Mathf.Abs(lowDelta) * preset.deltaSensitivity);
        
        // 低周波数帯域の総合影響
        float lowInfluence = preset.lowToIntensity.Evaluate(normalizedLow) * preset.lowGain;
        float deltaInfluence_scaled = deltaInfluence * preset.lowGain * 0.5f;
        
        // ビート強調効果
        float beatEffect = flashAdd + beatEmphasis;
        
        // 総合強度
        float totalIntensity = baseInt + lowInfluence + deltaInfluence_scaled + beatEffect;
        
        // フラッシュ効果を徐々に減衰
        flashAdd = Mathf.MoveTowards(flashAdd, 0f, preset.flashDecay * Time.deltaTime);
        
        // ビート強調効果を減衰
        beatEmphasis = Mathf.MoveTowards(beatEmphasis, 0f, preset.beatEmphasisDecay * Time.deltaTime);
        
        return totalIntensity;
    }

    /// <summary>
    /// エミッション強度の計算
    /// </summary>
    float CalculateEmissionIntensity()
    {
        // 基本エミッション
        float baseEmission = preset.emissionBase;
        
        // 動的閾値を考慮した低周波数帯域の影響
        float normalizedLow = 0f;
        if (lowAverage > 0.001f)
        {
            float relativeIntensity = analyzer.Low / lowAverage;
            normalizedLow = Mathf.Clamp01((relativeIntensity - 1f) * preset.dynamicThresholdSensitivity);
        }
        
        // 変化量の影響
        float deltaInfluence = Mathf.Clamp01(Mathf.Abs(lowDelta) * preset.deltaSensitivity);
        
        // エミッションの総合影響
        float lowInfluence = preset.lowToIntensity.Evaluate(normalizedLow) * preset.emissionGain;
        float deltaInfluence_scaled = deltaInfluence * preset.emissionGain * 0.5f;
        
        // ビート強調効果
        float beatEffect = flashAdd + beatEmphasis;
        
        return baseEmission + lowInfluence + deltaInfluence_scaled + beatEffect;
    }

    /// <summary>
    /// ビート検出時の処理
    /// </summary>
    void OnBeatDetected()
    {
        // フラッシュ効果を追加
        flashAdd += preset.beatFlash;
        
        // ビート強調効果を追加（より長続きする効果）
        beatEmphasis += preset.beatFlash * preset.beatEmphasisMultiplier;
    }

    /// <summary>
    /// 音声強度を0-1の範囲に正規化（曲によって値域が異なるため概算）
    /// </summary>
    /// <param name="x">正規化する音声強度</param>
    /// <returns>0-1の範囲に正規化された値</returns>
    static float Norm(float x) => Mathf.Clamp01(x * 10f);

    /// <summary>
    /// ビートイベント（外部から呼び出し可能）
    /// </summary>
    public void OnBeatFlash()
    {
        flashAdd += preset.beatFlash;
        beatEmphasis += preset.beatFlash * preset.beatEmphasisMultiplier;
    }
}
