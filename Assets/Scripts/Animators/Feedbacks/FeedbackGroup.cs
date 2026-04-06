using UnityEngine;

public class FeedbackGroup : MonoBehaviour
{
    [SerializeField] private Feedback[] _feedbacks;

    public void Initialize(Feedback[] feedbacks)
    {
        _feedbacks = feedbacks;
    }

    public void Play()
    {
        if (_feedbacks == null)
        {
            return;
        }

        for (int i = 0; i < _feedbacks.Length; i++)
        {
            if (_feedbacks[i] == null)
            {
                continue;
            }

            _feedbacks[i].Play();
        }
    }

    public void Stop()
    {
        if (_feedbacks == null)
        {
            return;
        }

        for (int i = 0; i < _feedbacks.Length; i++)
        {
            if (_feedbacks[i] == null)
            {
                continue;
            }

            _feedbacks[i].Stop();
        }
    }
}
