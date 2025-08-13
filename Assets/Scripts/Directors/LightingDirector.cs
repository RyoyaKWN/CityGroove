using UnityEngine;

public class LightingDirector : MonoBehaviour
{
    /// <summary>音解析</summary>
    [SerializeField] private AudioAnalyzer analyzer;
    /// <summary>照明プリセット</summary>
    [SerializeField] private LightingPreset preset;
    /// <summary>反応させたいLight</summary>
    [SerializeField] private Light reactiveLight;
    /// <summary>発光させたいオブジェクト</summary>
    [SerializeField] private Renderer[] emissionTargetArray;

    /// <summary>現在の色相</summary>
    float hue;
    /// <summary>ビートで一瞬だけ足すフラッシュ</summary>
    float flashAdd;
    MaterialPropertyBlock mpb;

    void Start()
    {
        // オンビート時のイベントにフラッシュ処理を付加
        if (analyzer != null) analyzer.OnBeat.AddListener(() => flashAdd += preset.beatFlash);
        hue = preset != null ? preset.baseHue : 0.5f;
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (analyzer == null || preset == null) return;

        // High帯で色相設定（カーブで調整可）
        float hueDelta = preset.highToHueSpeed.Evaluate(Norm(analyzer.High)) * preset.hueSpeed * Time.deltaTime;
        hue = (hue + hueDelta) % 1f;
        Color c = Color.HSVToRGB(hue, preset.sat, preset.val);

        // Low帯で強度設定し、ビートのフラッシュを加算
        float lowNorm = Norm(analyzer.Low);
        float dyn = preset.baseIntensity
                    + preset.lowToIntensity.Evaluate(lowNorm) * preset.lowGain
                    + flashAdd;
        flashAdd = Mathf.MoveTowards(flashAdd, 0f, preset.flashDecay * Time.deltaTime);

        // Lightに色相・強度を反映
        if (reactiveLight)
        {
            reactiveLight.color = c;
            reactiveLight.intensity = dyn;
        }

        // Emissionに反映（PropertyBlock）
        float emiss = preset.emissionBase
                      + preset.lowToIntensity.Evaluate(lowNorm) * preset.emissionGain
                      + flashAdd;

        foreach (var r in emissionTargetArray)
        {
            if (!r) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", c * emiss);
            r.SetPropertyBlock(mpb);
        }
    }

    // ざっくり正規化（値域は曲依存）
    static float Norm(float x) => Mathf.Clamp01(x * 10f);

    /// <summary>
    /// ビートイベント
    /// </summary>
    public void OnBeatFlash()
    {
        flashAdd += preset.beatFlash;
    }
}
