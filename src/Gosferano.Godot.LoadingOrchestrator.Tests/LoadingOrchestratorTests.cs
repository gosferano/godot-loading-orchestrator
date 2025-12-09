using FluentAssertions;
using Gosferano.Godot.LoadingOrchestrator.Tests.Helpers;
using Gosferano.Godot.LoadingOrchestrator.Tests.Mocks;
using Xunit;

namespace Gosferano.Godot.LoadingOrchestrator.Tests;

public class LoadingOrchestratorTests
{
    [Fact]
    public async Task ExecuteSteps_WithSingleStep_ReportsProgressFromZeroToOne()
    {
        // Arrange
        var loadable = new MockAsyncLoadable();
        var steps = new[] { new LoadingStep<string>(1.0f, loadable) };
        var progressReports = new List<(float, string)>();

        // Note: We can't easily test LoadingOrchestrator without SceneTree
        // But we can test the logic by calling ExecuteSteps directly if we refactor it

        // Act
        await steps[0].Execute((p, s) => progressReports.Add((p, s)));

        // Assert
        progressReports.Should().NotBeEmpty();
        progressReports.First().Item1.Should().BeGreaterThanOrEqualTo(0f);
        progressReports.Last().Item1.Should().Be(1.0f);
    }

    [Fact]
    public async Task ExecuteSteps_WhenStepThrows_PropagatesException()
    {
        // Arrange
        var loadable = new MockAsyncLoadable { ExceptionToThrow = new InvalidOperationException("Test error") };
        var step = new LoadingStep<string>(1.0f, loadable);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => step.Execute());
    }

    [Fact]
    public async Task ExecuteSteps_WithMultipleWeightedSteps_AllProgressReported()
    {
        // Arrange
        LoadingStep<string>[] steps =
        [
            new(1.0f, "Action 0", () => Task.CompletedTask),
            new(2.0f, "Action 1", () => Task.CompletedTask),
            new(1.0f, "Action 2", () => Task.CompletedTask)
        ];
        var reports = new List<(float, string)>();

        // Act
        await OrchestratorTestHelper.SimulateExecuteSteps(steps, (p, s) => reports.Add((p, s)));

        // Assert
        reports.Count.Should().Be(6);
        reports[0].Item1.Should().BeApproximately(0, 0.01f);
        reports[0].Item2.Should().Be("Action 0");
        reports[1].Item1.Should().BeApproximately(1 / 4f, 0.01f);
        reports[1].Item2.Should().Be("Action 0");
        reports[2].Item1.Should().BeApproximately(1 / 4f, 0.01f);
        reports[2].Item2.Should().Be("Action 1");
        reports[3].Item1.Should().BeApproximately(3 / 4f, 0.01f);
        reports[3].Item2.Should().Be("Action 1");
        reports[4].Item1.Should().BeApproximately(3 / 4f, 0.01f);
        reports[4].Item2.Should().Be("Action 2");
        reports[5].Item1.Should().BeApproximately(1.0f, 0.01f);
        reports[5].Item2.Should().Be("Action 2");
    }
}
