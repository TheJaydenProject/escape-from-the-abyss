using UnityEngine;

public class CollectibleKey : MonoBehaviour, IInteractable
{
    [Header("SFX")]
    public AudioSource pickupSfx; // can be elsewhere in the scene

    public string PromptText => "Press E to pick up Key";

    public void Interact(PlayerInteractorRaycast interactor)
    {
        if (GameManager.Instance == null || !GameManager.Instance.CollectKey()) return;

        if (pickupSfx && pickupSfx.clip)
        {
            pickupSfx.spatialBlend = 0f;

            bool sourceInsideKey = pickupSfx.transform == transform || pickupSfx.transform.IsChildOf(transform);

            if (sourceInsideKey)
            {
                pickupSfx.Play();
                HideKey();
                float life = pickupSfx.clip.length / Mathf.Max(0.01f, pickupSfx.pitch);
                Destroy(gameObject, life);
                return;
            }
            else
            {
                // Source is elsewhere -> just one-shot and destroy key now
                pickupSfx.PlayOneShot(pickupSfx.clip);
            }
        }

        // Key is gone immediately in both paths when source is external
        Destroy(gameObject);
    }

    void HideKey()
    {
        foreach (var c in GetComponentsInChildren<Collider>(true))  c.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>(true))  r.enabled = false;
        enabled = false;
    }
}
