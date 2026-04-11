using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FramesPerSecondDisplay : MonoBehaviour
{
    public Text textElement;
    public float refreshIntervalSeconds = 0.5f;

    private float timeAccumulatorSeconds = 0f;
    private int frameAccumulatorCount = 0;
    private float framesPerSecond = 0f;
    private Coroutine _refreshCoroutine;

    private void OnEnable()
    {
        StartRefreshLoop();
    }

    private void OnDisable()
    {
        StopRefreshLoop();
    }

    private IEnumerator RefreshLoop()
    {
        while (enabled)
        {
            if (textElement == null)
            {
                yield return null;

                continue;
            }

            timeAccumulatorSeconds = 0f;
            frameAccumulatorCount = 0;

            while (timeAccumulatorSeconds < refreshIntervalSeconds)
            {
                timeAccumulatorSeconds += Time.unscaledDeltaTime;
                frameAccumulatorCount += 1;

                yield return null;
            }

            framesPerSecond = frameAccumulatorCount / timeAccumulatorSeconds;
            textElement.text = Mathf.RoundToInt(framesPerSecond).ToString() + " FPS";
        }
    }

    private void StartRefreshLoop()
    {
        StopRefreshLoop();
        _refreshCoroutine = StartCoroutine(RefreshLoop());
    }

    private void StopRefreshLoop()
    {
        if (_refreshCoroutine == null)
        {
            return;
        }

        StopCoroutine(_refreshCoroutine);
        _refreshCoroutine = null;
    }
}
