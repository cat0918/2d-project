using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public abstract bool CanInteract(PlayerMove player);
    public abstract void Interact(PlayerMove player);
}
