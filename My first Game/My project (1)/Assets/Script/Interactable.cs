using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public virtual string InteractionLabel => "\uC0C1\uD638\uC791\uC6A9";
    public abstract bool CanInteract(PlayerMove player);
    public abstract void Interact(PlayerMove player);
}
