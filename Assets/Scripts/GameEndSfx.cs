using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EndHudAudioLoop : MonoBehaviour
{
    [Tooltip("If left empty, the AudioSource's clip will be used.")]
    public AudioClip loopClip;

    private AudioSource _src;

    void Awake()
    {
        _src = GetComponent<AudioSource>();
        if (loopClip) _src.clip = loopClip;
        _src.loop = true;
    }

    void OnEnable()
    {
        if (_src && _src.clip) _src.Play();
    }

    void OnDisable()
    {
        if (_src) _src.Stop();
    }
}
