public interface IInteractable
{
    // Общий интерфейс для объектов, с которыми игрок взаимодействует лучом из камеры.
    string InteractionPrompt { get; }
    bool CanInteract(PlayerInteraction playerInteraction);
    void Interact(PlayerInteraction playerInteraction);
}
