namespace Gosferano.Godot.LoadingOrchestrator.Tests.Helpers;

/// <summary>
/// Helper for testing orchestrator logic without requiring full Godot SceneTree
/// </summary>
public static class OrchestratorTestHelper
{
    /// <summary>
    /// Simulates ExecuteSteps behavior for testing
    /// </summary>
    public static async Task SimulateExecuteSteps(LoadingStep<string>[] steps, Action<float, string>? onProgress = null)
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
}
