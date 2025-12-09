namespace Gosferano.Godot.LoadingOrchestrator.Tests.Mocks;

public class MockAsyncLoadable : IAsyncLoadable<string>
{
    private readonly int _callCount;
    public bool IsLoaded { get; private set; }
    public int LoadCallCount { get; private set; }
    public TimeSpan LoadDelay { get; set; } = TimeSpan.Zero;
    public Exception? ExceptionToThrow { get; init; }

    public MockAsyncLoadable(int callCount)
    {
        _callCount = callCount;
    }

    public async Task LoadResources(Action<float, string>? onProgress = null)
    {
        LoadCallCount++;

        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        // Simulate progressive loading
        for (var i = 0; i <= _callCount; i++)
        {
            float progress = (float)i / _callCount;
            var status = $"Loading step {i}";

            onProgress?.Invoke(progress, status);

            if (LoadDelay > TimeSpan.Zero)
            {
                await Task.Delay(LoadDelay / 10);
            }
        }

        IsLoaded = true;
    }
}
