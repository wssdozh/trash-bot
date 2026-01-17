public sealed class PlayerBattleTimer
{
    private float _remainingSeconds;

    public bool IsRunning { get; private set; }

    public void Restart(float seconds)
    {
        _remainingSeconds = seconds;
        IsRunning = true;
    }

    public void Stop()
    {
        _remainingSeconds = 0f;
        IsRunning = false;
    }

    public bool Tick(float deltaTime)
    {
        if (IsRunning == false)
        {
            return false;
        }

        _remainingSeconds -= deltaTime;

        if (_remainingSeconds > 0f)
        {
            return false;
        }

        _remainingSeconds = 0f;
        IsRunning = false;

        return true;
    }
}
