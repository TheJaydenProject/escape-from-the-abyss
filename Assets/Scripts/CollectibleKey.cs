using UnityEngine;

public class CollectibleKey : MonoBehaviour, IInteractable
{
    [Header("VFX/SFX (optional)")]
    public ParticleSystem pickupVfx;
    public AudioClip pickupSfx; 

    public string PromptText => "Press E to pick up Key";

    public void Interact(PlayerInteractorRaycast interactor)
    {
        if (GameManager.Instance != null && GameManager.Instance.CollectKey())
        {
            if (pickupVfx) Instantiate(pickupVfx, transform.position, Quaternion.identity);
            if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, transform.position);

            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("[CollectibleKey] CollectKey() failed or GameManager missing.");
        }
    }
}
