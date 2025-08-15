using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MasterVolume : MonoBehaviour
{
    public AudioMixer mixer;   
    public Slider slider;        
    const string Param = "MasterVolume";
    const string Key   = "vol_master";

    void Awake()
    {
        float v = PlayerPrefs.GetFloat(Key, 1f);
        if (slider) { slider.value = v; slider.onValueChanged.AddListener(SetVol); }
        SetVol(v);
    }

    // Convert linear (0..1) -> dB (-80..0)
    public void SetVol(float v)
    {
        float dB = (v <= 0.0001f) ? -80f : Mathf.Log10(v) * 20f;
        mixer.SetFloat(Param, dB);
        PlayerPrefs.SetFloat(Key, v);
    }
}
