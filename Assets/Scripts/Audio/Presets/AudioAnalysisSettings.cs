using UnityEngine;

/// <summary>
/// ����͂̃`���[�j���O�l���A�Z�b�g�iInspector�j�ŊǗ����邽�߂̐ݒ�SO
/// </summary>
[CreateAssetMenu(menuName = "CityGroove/AudioAnalysisSettings")]
public class AudioAnalysisSettings : ScriptableObject
{
    [Header("FFT")]
    /// <summary>FFT(�����t�[���G�ϊ�)�̃T���v����</summary>
    [Min(256)] public int fftSize = 1024;
    /// <summary>FFT�̑��֐�</summary>
    public FFTWindow window = FFTWindow.BlackmanHarris;

    [Header("Bands (Hz)")]
    /// <summary>���т̍ő勫�E�l</summary>
    [Min(50)] public int lowMaxHz = 200;
    /// <summary>����т̍ő勫�E�l</summary>
    [Min(1000)] public int midMaxHz = 2000;

    [Header("Beat Detection")]
    /// <summary>�r�[�g���o��臒l�{��</summary>
    [Range(0.5f, 3f)] public float fluxThresholdMul = 1.5f;
    /// <summary>�A�Ŗh�~</summary>
    [Range(0.05f, 0.3f)] public float beatCooldown = 0.12f;
    [Range(16, 96)] public int fluxHistory = 43;

    [Header("Post Processing")]
    /// <summary>�m�C�Y�}���╽�����p</summary>
    public AnimationCurve bandResponse = AnimationCurve.Linear(0, 1, 1, 1);
}
