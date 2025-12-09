using Godot;

namespace Gosferano.Godot.LoadingOrchestrator;

/// <summary>
/// Orchestrates multistep loading operations with weighted progress tracking
/// </summary>
public class LoadingOrchestrator<TStatus>
    where TStatus : notnull
{
    private readonly SceneTree _sceneTree;

    /// <summary>
    /// Creates a new loading orchestrator
    /// </summary>
    /// <param name="sceneTree">Scene tree reference</param>
    public LoadingOrchestrator(SceneTree sceneTree)
    {
        _sceneTree = sceneTree;
    }

    /// <summary>
    /// Executes multiple loading steps with aggregate progress tracking
    /// </summary>
    /// <param name="steps">Loading steps to execute</param>
    /// <param name="onProgress">Progress callback (0.0 to 1.0, status object)</param>
    public async Task ExecuteSteps(LoadingStep<TStatus>[] steps, Action<float, TStatus>? onProgress = null)
    {
        float totalWeight = steps.Sum(step => step.Weight);
        var currentProgress = 0f;

        foreach (var step in steps)
        {
            float stepStart = currentProgress;
            float stepEnd = currentProgress + step.Weight / totalWeight;

            await step.Execute(
                (stepProgress, status) =>
                {
                    float actualProgress = stepStart + ((stepEnd - stepStart) * stepProgress);
                    onProgress?.Invoke(actualProgress, status);
                }
            );

            currentProgress = stepEnd;
        }
    }

    /// <summary>
    /// Executes an operation with a loading screen
    /// </summary>
    /// <param name="loadingScreen">Pre-instantiated loading screen node</param>
    /// <param name="operation">Operation to execute</param>
    /// <param name="onComplete">Optional callback when operation completes successfully</param>
    /// <param name="onError">Optional error handler</param>
    public async Task ExecuteWithLoadingScreen(
        Node loadingScreen,
        Func<Action<float, TStatus>, Task> operation,
        Func<Task>? onComplete = null,
        Func<Exception, Task>? onError = null
    )
    {
        _sceneTree.Root.AddChild(loadingScreen);

        try
        {
            void ProgressCallback(float progress, TStatus status)
            {
                if (loadingScreen is ILoadingScreen<TStatus> loadingScreenInterface)
                {
                    loadingScreenInterface.UpdateLoadingState(progress, status);
                }
            }

            await operation(ProgressCallback);

            if (onComplete != null)
            {
                await onComplete();
            }
        }
        catch (Exception ex)
        {
            if (onError != null)
            {
                await onError(ex);
            }
            else
            {
                throw;
            }
        }
        finally
        {
            loadingScreen.QueueFree();
        }
    }

    /// <summary>
    /// Executes loading steps with a loading screen (convenience method)
    /// </summary>
    /// <param name="loadingScreen">Pre-instantiated loading screen node</param>
    /// <param name="steps">Loading steps to execute</param>
    /// <param name="onComplete">Optional callback when loading completes</param>
    /// <param name="onError">Optional error handler</param>
    public async Task ExecuteStepsWithLoadingScreen(
        Node loadingScreen,
        LoadingStep<TStatus>[] steps,
        Func<Task>? onComplete = null,
        Func<Exception, Task>? onError = null
    )
    {
        await ExecuteWithLoadingScreen(
            loadingScreen,
            async progressCallback => await ExecuteSteps(steps, progressCallback),
            onComplete,
            onError
        );
    }
}
