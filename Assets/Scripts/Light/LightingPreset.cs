using UnityEngine;

[CreateAssetMenu(menuName = "CityGroove/LightingPreset")]
public class LightingPreset : ScriptableObject
{
    [Header("Intensity")]
    /// <summary>���邳�̃x�[�X�l</summary>
    public float baseIntensity = 1.2f;
    /// <summary>Low�������قǏ�悹</summary>
    public float lowGain = 30f;
    /// <summary>�I���r�[�g���ɏu�ԓI�ɏ�悹</summary>
    public float beatFlash = 2.0f;
    /// <summary>�t���b�V���̌������x</summary>
    public float flashDecay = 5f;

    [Header("Emission")]
    /// <summary>�l�I���̔����x�[�X</summary>
    public float emissionBase = 1.0f;
    /// <summary>�l�I���̔����Q�C��</summary>
    public float emissionGain = 8f;

    [Header("Color (Hue)")]
    /// <summary>�F��</summary>
    [Range(0f, 1f)] public float baseHue = 0.6f;
    public float hueSpeed = 0.1f; // High�ɏ�Z���ĉ�]���x��ݒ�
    /// <summary>�ʓx</summary>
    [Range(0f, 1f)] public float sat = 0.9f;
    /// <summary>���x</summary>
    [Range(0f, 2f)] public float val = 1.0f;

    [Header("Response Curves")]
    /// <summary>AnmationCurve�Ŕ����̃J�[�u</summary>
    public AnimationCurve lowToIntensity = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve highToHueSpeed = AnimationCurve.Linear(0, 0, 1, 1);
}
