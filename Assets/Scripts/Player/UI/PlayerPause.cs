using UnityEngine;

public class PlayerPause : MonoBehaviour
{
    [SerializeField] private PauseController _pauseController;

    public void Toggle()
    {
        if (_pauseController.IsPaused == false)
        {
            _pauseController.Pause();
            return;
        }

        _pauseController.Resume();
    }
}
