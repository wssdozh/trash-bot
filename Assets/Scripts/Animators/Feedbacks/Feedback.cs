using UnityEngine;

public abstract class Feedback : MonoBehaviour
{
    public abstract void Play();

    public virtual void Stop()
    {
    }
}
