using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ObjectiveItemHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public event Action<ObjectiveItemHoverHandler> PointerEntered;
    public event Action<ObjectiveItemHoverHandler> PointerExited;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (PointerEntered != null)
        {
            PointerEntered.Invoke(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (PointerExited != null)
        {
            PointerExited.Invoke(this);
        }
    }
}
