using UnityEngine;

public abstract class HighlighterBase : MonoBehaviour
{
    public void Highlight(bool state)
    {
        if (state)
        {
            EnableHighlight();
        }
        else
        {
            DisableHighlight();
        }
    }

    protected abstract void EnableHighlight();
    protected abstract void DisableHighlight();
}
