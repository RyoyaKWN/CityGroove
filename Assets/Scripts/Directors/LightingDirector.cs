using UnityEngine;

public class LightingDirector : MonoBehaviour
{
    /// <summary>�����</summary>
    [SerializeField] private AudioAnalyzer analyzer;
    /// <summary>�Ɩ��v���Z�b�g</summary>
    [SerializeField] private LightingPreset preset;
    /// <summary>������������Light</summary>
    [SerializeField] private Light reactiveLight;
    /// <summary>�������������I�u�W�F�N�g</summary>
    [SerializeField] private Renderer[] emissionTargetArray;

    /// <summary>���݂̐F��</summary>
    float hue;
    /// <summary>�r�[�g�ň�u���������t���b�V��</summary>
    float flashAdd;
    MaterialPropertyBlock mpb;

    void Start()
    {
        // �I���r�[�g���̃C�x���g�Ƀt���b�V��������t��
        if (analyzer != null) analyzer.OnBeat.AddListener(() => flashAdd += preset.beatFlash);
        hue = preset != null ? preset.baseHue : 0.5f;
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (analyzer == null || preset == null) return;

        // High�тŐF���ݒ�i�J�[�u�Œ����j
        float hueDelta = preset.highToHueSpeed.Evaluate(Norm(analyzer.High)) * preset.hueSpeed * Time.deltaTime;
        hue = (hue + hueDelta) % 1f;
        Color c = Color.HSVToRGB(hue, preset.sat, preset.val);

        // Low�тŋ��x�ݒ肵�A�r�[�g�̃t���b�V�������Z
        float lowNorm = Norm(analyzer.Low);
        float dyn = preset.baseIntensity
                    + preset.lowToIntensity.Evaluate(lowNorm) * preset.lowGain
                    + flashAdd;
        flashAdd = Mathf.MoveTowards(flashAdd, 0f, preset.flashDecay * Time.deltaTime);

        // Light�ɐF���E���x�𔽉f
        if (reactiveLight)
        {
            reactiveLight.color = c;
            reactiveLight.intensity = dyn;
        }

        // Emission�ɔ��f�iPropertyBlock�j
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

    // �������萳�K���i�l��͋Ȉˑ��j
    static float Norm(float x) => Mathf.Clamp01(x * 10f);

    /// <summary>
    /// �r�[�g�C�x���g
    /// </summary>
    public void OnBeatFlash()
    {
        flashAdd += preset.beatFlash;
    }
}
