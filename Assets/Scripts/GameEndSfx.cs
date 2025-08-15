using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EndHudAudioLoop : MonoBehaviour
{
    public AudioSource source; // Assign in inspector or auto-get

    void Awake()
    {
        if (!source)
            source = GetComponent<AudioSource>();

        source.loop = true; // Always loop
    }

    public void PlayLoop()
    {
        if (source && source.clip)
            source.Play();
    }

    public void StopLoop()
    {
        if (source)
            source.Stop();
    }
}
