using UnityEngine;

public class FeedbackGroup : MonoBehaviour
{
    [SerializeField] private Feedback[] _feedbacks;

    public void Play()
    {
        for (int i = 0; i < _feedbacks.Length; i++)
        {
            _feedbacks[i].Play();
        }
    }

    public void Stop()
    {
        for (int i = 0; i < _feedbacks.Length; i++)
        {
            _feedbacks[i].Stop();
        }
    }
}
