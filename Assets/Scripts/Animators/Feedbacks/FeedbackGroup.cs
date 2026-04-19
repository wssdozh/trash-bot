using UnityEngine;

public class FeedbackGroup : MonoBehaviour
{
    [SerializeField] private Feedback[] _feedbacks;

    public bool IsPlaying
    {
        get
        {
            if (_feedbacks == null)
            {
                return false;
            }

            int feedbackIndex = 0;

            while (feedbackIndex < _feedbacks.Length)
            {
                Feedback feedback = _feedbacks[feedbackIndex];
                feedbackIndex += 1;

                if (feedback == null)
                {
                    continue;
                }

                if (feedback.IsPlaying)
                {
                    return true;
                }
            }

            return false;
        }
    }

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
