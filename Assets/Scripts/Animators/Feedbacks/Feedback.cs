using UnityEngine;

public abstract class Feedback : MonoBehaviour
{
    public virtual bool IsPlaying => false;

    public abstract void Play();

    public virtual void Stop()
    {
    }
}
