using UnityEngine;
using UnityEngine.UI;

public class FramesPerSecondDisplay : MonoBehaviour
{
    public Text textElement;
    public float refreshIntervalSeconds = 0.5f;

    private float timeAccumulatorSeconds = 0f;
    private int frameAccumulatorCount = 0;
    private float framesPerSecond = 0f;

    private void Update()
    {
        if (textElement == null)
            return;

        timeAccumulatorSeconds += Time.unscaledDeltaTime;
        frameAccumulatorCount += 1;

        if (timeAccumulatorSeconds >= refreshIntervalSeconds)
        {
            framesPerSecond = frameAccumulatorCount / timeAccumulatorSeconds;
            frameAccumulatorCount = 0;
            timeAccumulatorSeconds = 0f;
            textElement.text = Mathf.RoundToInt(framesPerSecond).ToString() + " FPS";
        }
    }
}
