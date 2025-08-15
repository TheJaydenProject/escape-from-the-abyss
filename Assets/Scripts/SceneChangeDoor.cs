using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneChangeDoor : MonoBehaviour, IInteractable
{
    [Header("SFX (optional)")]
    public AudioSource doorSfx; // plays once on interact

    [Header("Scene")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset sceneAsset; // visible only in Editor
#endif
    [SerializeField] private string sceneToLoad; // actual name used in build

    [Header("Timing")]
    [Tooltip("If no AudioSource/clip is available, wait this long before loading.")]
    public float fallbackWaitSeconds = 1.0f;
    [Tooltip("Extra delay added after SFX (or fallback) before loading.")]
    public float extraDelay = 0f;

    [Header("One-shot Control")]
    [Tooltip("Prevent multiple triggers while waiting.")]
    public bool disableCollidersOnUse = true;

    public string PromptText => "[E] Open";

    bool _triggered;

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-update scene name when you pick a scene in Inspector
        if (sceneAsset != null)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            sceneToLoad = System.IO.Path.GetFileNameWithoutExtension(path);
        }
    }
#endif

    public void Interact(PlayerInteractorRaycast interactor)
    {
        if (_triggered) return;
        _triggered = true;

        if (disableCollidersOnUse)
        {
            foreach (var c in GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }

        if (doorSfx && doorSfx.clip)
        {
            doorSfx.playOnAwake = false;
            if (doorSfx.spatialBlend < 0f || doorSfx.spatialBlend > 1f)
                doorSfx.spatialBlend = 0f;
            doorSfx.Play();
        }

        StartCoroutine(LoadAfterDelay());
    }

    IEnumerator LoadAfterDelay()
    {
        float wait = ComputeWaitSeconds();
        if (wait > 0f) yield return new WaitForSeconds(wait);

        if (extraDelay > 0f) yield return new WaitForSeconds(extraDelay);

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogWarning("[SceneChangeDoor] No sceneToLoad set.");
        }
    }

    float ComputeWaitSeconds()
    {
        if (doorSfx && doorSfx.clip)
        {
            float p = Mathf.Max(0.01f, doorSfx.pitch);
            return doorSfx.clip.length / p;
        }
        return Mathf.Max(0f, fallbackWaitSeconds);
    }
}
