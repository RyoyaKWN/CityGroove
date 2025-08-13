// Assets/Scripts/Audio/AudioAnalyzer.cs
using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(AudioSource))]
public class AudioAnalyzer : MonoBehaviour
{
    /// <summary>����͂̐ݒ�SO</summary>
    [SerializeField] private AudioAnalysisSettings settings;

    [System.Serializable] public class BeatEvent : UnityEvent { }
    /// <summary>�r�[�g���O���ɒʒm����C�x���g</summary>
    public BeatEvent OnBeat;

    // ��͌��ʁi�ǂݎ���p�j
    public float Low { get; private set; }
    public float Mid { get; private set; }
    public float High { get; private set; }
    public float Flux { get; private set; }

    /// <summary>�I�[�f�B�I�\�[�X</summary>
    AudioSource src;
    /// <summary>���݂̃X�y�N�g����</summary>
    float[] spectrum;
    /// <summary>�O��̃X�y�N�g����</summary>
    float[] prevSpec;
    /// <summary>�ړ����ϗp�o�b�t�@</summary>
    float[] fluxBuf;
    /// <summary>�����N�o�b�t�@�̏������݈ʒu</summary>
    int fluxIdx;
    /// <summary>���߃r�[�g�̎��ԁi�A�Ŗh�~�j</summary>
    float lastBeatTime; 

    void Awake()
    {
        // �I�[�f�B�I�\�[�X�ǂݍ���
        src = GetComponent<AudioSource>();

        // FFT�T�C�Y�A���𒷂�������
        int n = Mathf.Max(256, settings.fftSize);
        spectrum = new float[n];
        prevSpec = new float[n];
        fluxBuf = new float[Mathf.Clamp(settings.fluxHistory, 16, 256)];
    }

    void Update()
    {
        if (settings == null) return;

        // FFT�Ŏ��g���������擾
        src.GetSpectrumData(spectrum, 0, settings.window);

        // �ш��Low/Mid/High�ɐϕ��i���E��Nyquist���g������bin���Z�j
        float nyquist = AudioSettings.outputSampleRate * 0.5f;
        int n = spectrum.Length;
        int lowMaxBin = Mathf.Clamp(Mathf.RoundToInt(settings.lowMaxHz / nyquist * n), 1, n - 2);
        int midMaxBin = Mathf.Clamp(Mathf.RoundToInt(settings.midMaxHz / nyquist * n), lowMaxBin + 1, n - 1);

        // �ш�ϕ�
        float low = 0, mid = 0, high = 0;
        for (int i = 0; i < n; i++)
        {
            float v = spectrum[i];
            if (i <= lowMaxBin) low += v;
            else if (i <= midMaxBin) mid += v;
            else high += v;
        }

        // �X�y�N�g���t���b�N�X�̌v�Z
        // �X�y�N�g���t���b�N�X�F�O�t���[����葝�������̍��v
        float flux = 0f;
        for (int i = 0; i < n; i++)
        {
            float diff = spectrum[i] - prevSpec[i];

            // ���̍����̂ݍ̗p
            if (diff > 0) flux += diff;

            // �����r�p�ɕۑ�
            prevSpec[i] = spectrum[i];
        }

        // �e��͌��ʂ𔽉f
        Low = low;
        Mid = mid;
        High = high;
        Flux = flux;

        // �t���b�N�X�̈ړ�����*�{����臒l�ɂ��āA��������r�[�g�C�x���g�𔭉�
        fluxBuf[fluxIdx] = flux; 
        fluxIdx = (fluxIdx + 1) % fluxBuf.Length;

        float avg = 0f;
        for (int i = 0; i < fluxBuf.Length; i++) avg += fluxBuf[i];
        avg /= fluxBuf.Length;

        float threshold = avg * settings.fluxThresholdMul + 0.0005f;    // �����I�t�Z�b�g
        if (flux > threshold && Time.time - lastBeatTime > settings.beatCooldown)
        {
            lastBeatTime = Time.time;
            OnBeat?.Invoke();   // �O���ɒʒm
        }
    }
}
