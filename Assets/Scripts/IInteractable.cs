public interface IInteractable
{
    string PromptText { get; }
    void Interact(PlayerInteractorRaycast interactor);
}
