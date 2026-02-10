public interface IShotStrategy
{
    bool IsBusy { get; }

    bool TryStartShot(FireShotContext context);

    void Tick(FireShotContext context);

    void Stop();
}
