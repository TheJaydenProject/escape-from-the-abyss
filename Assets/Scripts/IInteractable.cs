/// <summary>
/// Author: Jayden Wong
/// Date: 11 August 2025
/// Description: Defines a contract for all objects that the player can interact with.
/// Any class that implements this interface must provide interaction behavior 
/// and a prompt text to guide the player.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// A short instruction shown to the player (e.g., "[E] Open Door").
    /// Helps guide the player on what action is possible with this object.
    /// </summary>
    string PromptText { get; }

    /// <summary>
    /// Called when the player interacts with this object.
    /// The object decides how it responds (e.g., open a door, pick up an item).
    /// </summary>
    void Interact(PlayerInteractorRaycast interactor);
}
