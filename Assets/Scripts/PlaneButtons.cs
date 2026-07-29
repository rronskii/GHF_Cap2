using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class PlaneButtons : MonoBehaviour
{
    [Tooltip("What happens when the player clicks this plane?")]
    public UnityEvent onClickAction;

    // This is a built-in Unity function that detects when a mouse clicks a 3D Collider!
    private void OnMouseDown()
    {
        onClickAction.Invoke();
    }
}