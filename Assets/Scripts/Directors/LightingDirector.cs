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

    void Start()
    {
        // ビート検出時にフラッシュ効果を追加
        if (analyzer != null) analyzer.OnBeat.AddListener(() => flashAdd += preset.beatFlash);
        
        // 初期色相をプリセットから設定
        hue = preset != null ? preset.baseHue : 0.5f;
        
        // マテリアルプロパティブロックを初期化
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (analyzer == null || preset == null) return;

        // 高周波数帯域で色相を変化（AnimationCurveで調整可能）
        float hueDelta = preset.highToHueSpeed.Evaluate(Norm(analyzer.High)) * preset.hueSpeed * Time.deltaTime;
        hue = (hue + hueDelta) % 1f;  // 0-1の範囲に正規化
        Color c = Color.HSVToRGB(hue, preset.sat, preset.val);

        // 低周波数帯域で強度を設定し、ビートフラッシュを加算
        float lowNorm = Norm(analyzer.Low);
        float dyn = preset.baseIntensity
                    + preset.lowToIntensity.Evaluate(lowNorm) * preset.lowGain
                    + flashAdd;
        
        // フラッシュ効果を徐々に減衰
        flashAdd = Mathf.MoveTowards(flashAdd, 0f, preset.flashDecay * Time.deltaTime);

        // Lightコンポーネントに色と強度を適用
        if (reactiveLight)
        {
            reactiveLight.color = c;
            reactiveLight.intensity = dyn;
        }

        // マテリアルのエミッションを制御（MaterialPropertyBlock使用）
        float emiss = preset.emissionBase
                      + preset.lowToIntensity.Evaluate(lowNorm) * preset.emissionGain
                      + flashAdd;

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
    }
}
