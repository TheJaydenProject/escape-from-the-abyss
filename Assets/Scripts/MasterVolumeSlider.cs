/*
 * Author: Jayden Wong
 * Date: 15 August 2025
 * Description: Handles the game's master volume by linking an AudioMixer parameter 
 *              with a UI slider. Saves and restores player preferences so that 
 *              volume settings persist between sessions.
 */

using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MasterVolume : MonoBehaviour
{
    // Reference to the AudioMixer where the MasterVolume parameter exists.
    public AudioMixer mixer;   
    
    // Slider UI element that the player interacts with to control volume.
    public Slider slider;        

    // Constant string used to reference the parameter inside the AudioMixer.
    const string Param = "MasterVolume";

    // Key name used to save and load the volume value in PlayerPrefs.
    const string Key   = "vol_master";

    void Awake()
    {
        // Load the last saved volume setting (default to 1 if not found).
        float v = PlayerPrefs.GetFloat(Key, 1f);

        // If a slider is assigned, set its value and register a listener so that
        // moving the slider updates the volume in real-time.
        if (slider) { slider.value = v; slider.onValueChanged.AddListener(SetVol); }

        // Apply the volume setting to the AudioMixer immediately.
        SetVol(v);
    }

    /// <summary>
    /// Converts the slider value (0–1) into decibels and applies it to the AudioMixer.  
    /// Also saves the value to PlayerPrefs so the setting persists between sessions.
    /// </summary>
    public void SetVol(float v)
    {
        // Unity's AudioMixer works in decibels, not linear 0–1 values.
        // Use logarithmic scaling: very low values become silent (-80dB).
        float dB = (v <= 0.0001f) ? -80f : Mathf.Log10(v) * 20f;

        // Apply the calculated decibel value to the AudioMixer's MasterVolume parameter.
        mixer.SetFloat(Param, dB);

        // Save the linear slider value (0–1) to PlayerPrefs for persistence.
        PlayerPrefs.SetFloat(Key, v);
    }
}
