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
        var loadable = new MockAsyncLoadable(10);
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
        var loadable = new MockAsyncLoadable(10) { ExceptionToThrow = new InvalidOperationException("Test error") };
        var step = new LoadingStep<string>(1.0f, loadable);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => step.Execute());
    }

    [Fact]
    public async Task ExecuteSteps_AllActions_WithMultipleWeightedSteps_AllProgressReported()
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

    [Fact]
    public async Task ExecuteSteps_AllLoadables_WithMultipleWeightedSteps_AllProgressReported()
    {
        // Arrange
        var loadable0 = new MockAsyncLoadable(callCount: 2);
        var loadable1 = new MockAsyncLoadable(callCount: 2);
        var loadable2 = new MockAsyncLoadable(callCount: 2);

        LoadingStep<string>[] steps =
        [
            new(1.0f, loadable0), // ← No status parameter!
            new(2.0f, loadable1),
            new(1.0f, loadable2)
        ];
        var reports = new List<(float, string)>();

        // Act
        await OrchestratorTestHelper.SimulateExecuteSteps(steps, (p, s) => reports.Add((p, s)));

        // Assert
        reports.Count.Should().Be(9);

        // Loadable 0 (weight 1.0, spans 0.0 - 0.25)
        reports[0].Item1.Should().BeApproximately(0.0f, 0.01f);
        reports[0].Item2.Should().Be("Loading step 0");
        reports[1].Item1.Should().BeApproximately(0.125f, 0.01f);
        reports[1].Item2.Should().Be("Loading step 1");
        reports[2].Item1.Should().BeApproximately(0.25f, 0.01f);
        reports[2].Item2.Should().Be("Loading step 2");

        // Loadable 1 (weight 2.0, spans 0.25 - 0.75)
        reports[3].Item1.Should().BeApproximately(0.25f, 0.01f);
        reports[3].Item2.Should().Be("Loading step 0");
        reports[4].Item1.Should().BeApproximately(0.5f, 0.01f);
        reports[4].Item2.Should().Be("Loading step 1");
        reports[5].Item1.Should().BeApproximately(0.75f, 0.01f);
        reports[5].Item2.Should().Be("Loading step 2");

        // Loadable 2 (weight 1.0, spans 0.75 - 1.0)
        reports[6].Item1.Should().BeApproximately(0.75f, 0.01f);
        reports[6].Item2.Should().Be("Loading step 0");
        reports[7].Item1.Should().BeApproximately(0.875f, 0.01f);
        reports[7].Item2.Should().Be("Loading step 1");
        reports[8].Item1.Should().BeApproximately(1.0f, 0.01f);
        reports[8].Item2.Should().Be("Loading step 2");

        // Verify all loadables completed
        loadable0.IsLoaded.Should().BeTrue();
        loadable1.IsLoaded.Should().BeTrue();
        loadable2.IsLoaded.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteSteps_ThrowsOnStepException_AndHalts()
    {
        // Arrange
        LoadingStep<string>[] steps =
        [
            new(1.0f, "Good", () => Task.CompletedTask),
            new(1.0f, "Bad", () => throw new Exception("fail")),
            new(1.0f, "After", () => Task.CompletedTask)
        ];
        var reports = new List<string>();

        // Act
        var act = async () => await OrchestratorTestHelper.SimulateExecuteSteps(steps, (_, s) => reports.Add(s));

        // Assert
        await act.Should().ThrowAsync<Exception>();
        reports.Should().Contain("Good");
        reports.Should().Contain("Bad");
        reports.Should().NotContain("After");
    }
}
