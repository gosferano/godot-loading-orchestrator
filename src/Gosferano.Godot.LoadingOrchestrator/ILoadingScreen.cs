namespace Gosferano.Godot.LoadingOrchestrator;

/// <summary>
/// Minimal interface for loading screen implementations.
/// Implementations can have any internal structure they want.
/// </summary>
public interface ILoadingScreen<in TStatus>
    where TStatus : notnull
{
    /// <summary>
    /// Updates the loading state. Implementation decides how to display this.
    /// </summary>
    /// <param name="progress">Progress value (0.0 to 1.0)</param>
    /// <param name="status">Status object with progress related data</param>
    void UpdateLoadingState(float progress, TStatus status);
}
