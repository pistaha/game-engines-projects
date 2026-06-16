using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickupItem : MonoBehaviour, IInteractable
{
    // Универсальная логика подбора через общий интерфейс взаимодействия.
    // В десктопной версии бутылки дополнительно обрабатываются скриптом предмета и скриптом захвата.
    [SerializeField] private float followSharpness = 26f;
    [SerializeField] private bool disableGravityWhileHeld = true;

    private Rigidbody cachedRigidbody;
    private PlayerInteraction heldBy;
    private bool previousKinematic;
    private bool previousUseGravity;
    private CollisionDetectionMode previousCollisionDetectionMode;

    public bool IsHeldByPlayer => heldBy != null;
    public PlayerInteraction HeldBy => heldBy;

    public virtual string InteractionPrompt => IsHeldByPlayer ? "ПКМ - отпустить" : "ПКМ - взять";

    protected virtual void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
    }

    public virtual bool CanInteract(PlayerInteraction playerInteraction)
    {
        return playerInteraction != null && (heldBy == null || heldBy == playerInteraction);
    }

    public virtual void Interact(PlayerInteraction playerInteraction)
    {
        if (!CanInteract(playerInteraction))
            return;

        if (IsHeldByPlayer)
            Drop();
        else
            PickUp(playerInteraction);
    }

    public virtual void PickUp(PlayerInteraction playerInteraction)
    {
        if (playerInteraction == null || IsHeldByPlayer)
            return;

        if (cachedRigidbody == null)
            cachedRigidbody = GetComponent<Rigidbody>();

        heldBy = playerInteraction;
        heldBy.SetHeldItem(this);

        if (cachedRigidbody == null)
            return;

        previousKinematic = cachedRigidbody.isKinematic;
        previousUseGravity = cachedRigidbody.useGravity;
        previousCollisionDetectionMode = cachedRigidbody.collisionDetectionMode;
        cachedRigidbody.isKinematic = false;
        cachedRigidbody.linearVelocity = Vector3.zero;
        cachedRigidbody.angularVelocity = Vector3.zero;
        cachedRigidbody.useGravity = !disableGravityWhileHeld && previousUseGravity;
        cachedRigidbody.isKinematic = true;
        cachedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    public virtual void Drop()
    {
        PlayerInteraction previousHolder = heldBy;
        heldBy = null;

        if (previousHolder != null)
            previousHolder.ClearHeldItem(this);

        RestoreRigidbodyState();
    }

    public void ForceReleaseWithoutPhysicsRestore()
    {
        PlayerInteraction previousHolder = heldBy;
        heldBy = null;

        if (previousHolder != null)
            previousHolder.ClearHeldItem(this);
    }

    public void TickHeld(float deltaTime)
    {
        if (heldBy == null || cachedRigidbody == null)
            return;

        Transform holdPoint = heldBy.HoldPoint;
        if (holdPoint == null)
            return;

        Transform heldTransform = cachedRigidbody.transform;
        heldTransform.position = Vector3.Lerp(
            heldTransform.position,
            holdPoint.position,
            1f - Mathf.Exp(-followSharpness * deltaTime)
        );
    }

    protected void RestoreRigidbodyState()
    {
        if (cachedRigidbody == null)
            return;

        cachedRigidbody.isKinematic = previousKinematic;
        cachedRigidbody.useGravity = previousUseGravity;
        cachedRigidbody.collisionDetectionMode = previousCollisionDetectionMode;
        cachedRigidbody.WakeUp();
    }
}
