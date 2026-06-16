using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StirringSpoon : PickupItem
{
    // Ложка отличается от ингредиентов тем, что запускает действие перемешивания в котле.
    private bool desktopHeld;

    public bool IsHeld => IsHeldByPlayer || desktopHeld;

    public override string InteractionPrompt => IsHeldByPlayer ? "ПКМ - отпустить" : "ПКМ - взять";

    public void SetDesktopHeld(bool value)
    {
        desktopHeld = value;
    }

}
