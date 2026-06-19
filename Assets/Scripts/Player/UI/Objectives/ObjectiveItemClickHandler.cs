using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ObjectiveItemClickHandler : MonoBehaviour, IPointerClickHandler
{
    public event Action<ObjectiveItemClickHandler> Clicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Clicked != null)
        {
            Clicked.Invoke(this);
        }
    }
}
