using UnityEngine;

public class WalkingLoopControllerFPS : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource walkingLoopSource; // assign the AudioSource with the loop clip already set

    [Header("Movement")]
    public float speedThreshold = 0.30f;  // ignore micro-taps
    public float startDelay = 0.1f;      // must hold movement this long before sound starts

    [Header("Polish (optional)")]
    public bool fadeOutOnStop = false;    // quick fade to avoid clicks
    public float fadeTime = 0.08f;

    Vector3 _lastPos;
    bool _fading;
    float _movementHeldTime = 0f; // how long we've been moving

    void Awake()
    {
        if (!walkingLoopSource)
        {
            var cam = Camera.main;
            if (cam)
                walkingLoopSource = cam.GetComponent<AudioSource>() ?? cam.gameObject.AddComponent<AudioSource>();
        }

        if (walkingLoopSource)
        {
            walkingLoopSource.playOnAwake = false; // don’t start immediately
            walkingLoopSource.loop = true;        // keep playing while moving
            walkingLoopSource.spatialBlend = 0f;  // 2D for FPS
            walkingLoopSource.dopplerLevel = 0f;
        }

        _lastPos = transform.position;
    }

    void Update()
    {
        // GameManager state check
        var gm = GameManager.Instance;
        if (gm && (gm.State != GameState.Playing || gm.IsHandlingDeath))
        {
            StopAndResetImmediate();
            _movementHeldTime = 0f;
            return;
        }

        // Horizontal speed (ignore vertical)
        Vector3 pos = transform.position;
        Vector3 delta = pos - _lastPos;
        delta.y = 0f;
        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        _lastPos = pos;

        bool moving = speed > speedThreshold;

        if (moving)
        {
            _movementHeldTime += Time.deltaTime;

            // Start only after holding movement for startDelay
            if (_movementHeldTime >= startDelay && walkingLoopSource && !walkingLoopSource.isPlaying && !_fading)
            {
                walkingLoopSource.time = 0f; // restart from beginning
                walkingLoopSource.Play();
            }
        }
        else
        {
            _movementHeldTime = 0f; // reset timer

            if (walkingLoopSource && walkingLoopSource.isPlaying && !_fading)
            {
                if (fadeOutOnStop) StartCoroutine(FadeOutThenStop());
                else               StopAndResetImmediate();
            }
        }
    }

    System.Collections.IEnumerator FadeOutThenStop()
    {
        _fading = true;
        float startVol = walkingLoopSource.volume;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            walkingLoopSource.volume = Mathf.Lerp(startVol, 0f, Mathf.Clamp01(t / fadeTime));
            yield return null;
        }
        StopAndResetImmediate();
        walkingLoopSource.volume = startVol;
        _fading = false;
    }

    void StopAndResetImmediate()
    {
        if (!walkingLoopSource) return;
        walkingLoopSource.Stop();
    }
}
