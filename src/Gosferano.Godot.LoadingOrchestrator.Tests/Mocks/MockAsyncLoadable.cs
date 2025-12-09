namespace Gosferano.Godot.LoadingOrchestrator.Tests.Mocks;

public class MockAsyncLoadable : IAsyncLoadable<string>
{
    public bool IsLoaded { get; private set; }
    public int LoadCallCount { get; private set; }
    public TimeSpan LoadDelay { get; set; } = TimeSpan.Zero;
    public Exception? ExceptionToThrow { get; init; }

    public async Task LoadResources(Action<float, string>? onProgress = null)
    {
        LoadCallCount++;

        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        // Simulate progressive loading
        for (var i = 0; i <= 10; i++)
        {
            float progress = i / 10f;
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
