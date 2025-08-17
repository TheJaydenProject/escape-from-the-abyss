/*
 * Author: Jayden Wong
 * Date: 11 August 2025
 * Description: Controls looping background audio for the end-game HUD.
 *              Ensures the audio always loops, and provides simple Play and Stop
 *              methods to control the audio playback.
 */

using UnityEngine;

/// <summary>
/// Manages a looping audio source for the end HUD.
/// - Ensures the AudioSource is set to loop.
/// - Provides simple methods to start and stop the loop.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class EndHudAudioLoop : MonoBehaviour
{
    public AudioSource source; // Can be assigned in the Inspector or auto-fetched on Awake

    /// <summary>
    /// Initializes the audio source:
    /// - Gets the AudioSource if none is assigned.
    /// - Forces it to loop so it plays continuously once started.
    /// </summary>
    void Awake()
    {
        // If the reference is missing, try to auto-get it from the same GameObject
        if (!source)
            source = GetComponent<AudioSource>();

        // Always loop the assigned clip to make sure HUD music is continuous
        source.loop = true;
    }

    /// <summary>
    /// Starts playing the loop if the AudioSource and clip are valid.
    /// </summary>
    public void PlayLoop()
    {
        // Only play if a clip is assigned, otherwise nothing will happen
        if (source && source.clip)
            source.Play();
    }

    /// <summary>
    /// Stops the loop immediately if the AudioSource is valid.
    /// </summary>
    public void StopLoop()
    {
        if (source)
            source.Stop();
    }
}
