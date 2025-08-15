using UnityEngine;

public class ComputerTerminal : MonoBehaviour, IInteractable
{
    [Header("Key Spawn")]
    public GameObject keyPrefab;
    public Transform keySpawnPoint;
    private bool keySpawned;

    [Header("SFX")]
    public AudioSource interactSfx;

    [Header("Material Swap")]
    public Renderer screenRenderer;              
    [Min(0)] public int screenMaterialIndex = 0; 
    public Material materialAfterMilestone;    
    private bool visualsUpdated;

    public string PromptText => string.Empty;

    void Awake()
    {
        if (interactSfx && interactSfx.clip)
        {
            interactSfx.playOnAwake = false;
            interactSfx.spatialBlend = 0f;
            interactSfx.clip.LoadAudioData();
        }
    }

    public void Interact(PlayerInteractorRaycast interactor)
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.vhsMilestoneReached) return;

        // 1) SFX
        if (interactSfx && interactSfx.clip)
        {
            bool onSelf = interactSfx.transform == transform || interactSfx.transform.IsChildOf(transform);
            if (onSelf) interactSfx.Play();
            else        interactSfx.PlayOneShot(interactSfx.clip);
        }

        // 2) Swap ONLY the chosen material slot (once)
        if (!visualsUpdated && screenRenderer && materialAfterMilestone)
        {
            var mats = screenRenderer.materials; // instanced array (safe to modify at runtime)
            if (screenMaterialIndex >= 0 && screenMaterialIndex < mats.Length)
            {
                mats[screenMaterialIndex] = materialAfterMilestone;
                screenRenderer.materials = mats;
                visualsUpdated = true;
            }
            else
            {
                Debug.LogWarning($"[ComputerTerminal] screenMaterialIndex {screenMaterialIndex} out of range (0..{mats.Length - 1}).");
            }
        }

        // 3) Spawn key (once)
        if (!keySpawned && keyPrefab && keySpawnPoint)
        {
            Instantiate(keyPrefab, keySpawnPoint.position, keySpawnPoint.rotation);
            keySpawned = true;
            gm.keyIsSpawned = true;
            Debug.Log("[ComputerTerminal] Key spawned.");
        }
    }
}
