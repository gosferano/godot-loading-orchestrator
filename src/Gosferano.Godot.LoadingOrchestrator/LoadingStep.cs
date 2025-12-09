namespace Gosferano.Godot.LoadingOrchestrator;

/// <summary>
/// Represents a single step in a multistep loading process
/// </summary>
public class LoadingStep<TStatus>
    where TStatus : notnull
{
    /// <summary>
    /// Weight of this step (for progress calculation)
    /// </summary>
    public float Weight { get; }

    /// <summary>
    /// Status object for this step
    /// </summary>
    public TStatus Status { get; }

    /// <summary>
    /// Optional loadable resource
    /// </summary>
    public IAsyncLoadable<TStatus>? Loadable { get; }

    /// <summary>
    /// Optional async action to execute
    /// </summary>
    public Func<Task>? Action { get; }

    /// <summary>
    /// Creates a loading step with an IAsyncLoadable
    /// </summary>
    public LoadingStep(float weight, TStatus status, IAsyncLoadable<TStatus> loadable)
    {
        if (weight <= 0f)
        {
            throw new ArgumentException("Weight must be greater than zero", nameof(weight));
        }

        Weight = weight;
        Status = status;
        Loadable = loadable;
        Action = null;
    }

    /// <summary>
    /// Creates a loading step with a custom action
    /// </summary>
    public LoadingStep(float weight, TStatus status, Func<Task> action)
    {
        if (weight <= 0f)
        {
            throw new ArgumentException("Weight must be greater than zero", nameof(weight));
        }

        Weight = weight;
        Status = status;
        Loadable = null;
        Action = action;
    }

    /// <summary>
    /// Executes this loading step
    /// </summary>
    /// <param name="onProgress">Progress callback (0.0 to 1.0, status message)</param>
    public async Task Execute(Action<float, TStatus>? onProgress = null)
    {
        if (Loadable != null)
        {
            await Loadable.LoadResources(onProgress);
        }
        else if (Action != null)
        {
            onProgress?.Invoke(0f, Status);
            await Action();
            onProgress?.Invoke(1f, Status);
        }
    }
}
