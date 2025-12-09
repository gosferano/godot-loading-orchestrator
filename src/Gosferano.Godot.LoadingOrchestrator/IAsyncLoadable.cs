namespace Gosferano.Godot.LoadingOrchestrator;

/// <summary>
/// Interface for resources that can be loaded asynchronously with progress tracking
/// </summary>
public interface IAsyncLoadable<out TStatus>
    where TStatus : notnull
{
    /// <summary>
    /// Indicates whether the resource has been loaded
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Loads the resource asynchronously
    /// </summary>
    /// <param name="onProgress">Progress callback (progress: 0.0-1.0, status: description)</param>
    Task LoadResources(Action<float, TStatus>? onProgress = null);
}
