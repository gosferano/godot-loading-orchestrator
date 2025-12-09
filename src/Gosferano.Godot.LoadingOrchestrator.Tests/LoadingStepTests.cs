using FluentAssertions;
using Gosferano.Godot.LoadingOrchestrator.Tests.Mocks;
using Xunit;

namespace Gosferano.Godot.LoadingOrchestrator.Tests;

public class LoadingStepTests
{
    [Fact]
    public async Task Execute_WithLoadable_CallsLoadResources()
    {
        // Arrange
        var loadable = new MockAsyncLoadable(10);
        var step = new LoadingStep<string>(1.0f, loadable);

        // Act
        await step.Execute();

        // Assert
        loadable.LoadCallCount.Should().Be(1);
        loadable.IsLoaded.Should().BeTrue();
    }

    [Fact]
    public async Task Execute_WithLoadable_ReportsProgress()
    {
        // Arrange
        var loadable = new MockAsyncLoadable(10);
        var step = new LoadingStep<string>(1.0f, loadable);
        var progressReports = new List<(float, string)>();

        // Act
        await step.Execute((progress, status) => progressReports.Add((progress, status)));

        // Assert
        progressReports.Should().NotBeEmpty();
        progressReports.Last().Item1.Should().Be(1.0f);
    }

    [Fact]
    public async Task Execute_WithAction_ExecutesAction()
    {
        // Arrange
        var actionExecuted = false;
        var step = new LoadingStep<string>(
            1.0f,
            "Test Step",
            async () =>
            {
                await Task.Delay(10);
                actionExecuted = true;
            }
        );

        // Act
        await step.Execute();

        // Assert
        actionExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task Execute_WithAction_ReportsProgressAtStartAndEnd()
    {
        // Arrange
        var step = new LoadingStep<string>(1.0f, "Test Step", () => Task.CompletedTask);
        var progressReports = new List<(float, string)>();

        // Act
        await step.Execute((progress, status) => progressReports.Add((progress, status)));

        // Assert
        progressReports.Should().HaveCount(2);
        progressReports[0].Should().Be((0f, "Test Step"));
        progressReports[1].Should().Be((1f, "Test Step"));
    }

    [Fact]
    public async Task Execute_WithLoadableThrowingException_PropagatesException()
    {
        // Arrange
        var loadable = new MockAsyncLoadable(10) { ExceptionToThrow = new InvalidOperationException("Test error") };
        var step = new LoadingStep<string>(1.0f, loadable);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => step.Execute());
    }

    [Theory]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(2.0f)]
    public void Constructor_WithWeight_StoresWeight(float weight)
    {
        // Arrange & Act
        var step = new LoadingStep<string>(weight, "Test", () => Task.CompletedTask);

        // Assert
        step.Weight.Should().Be(weight);
    }

    [Fact]
    public void Constructor_WithDescription_StoresDescription()
    {
        // Arrange & Act
        var step = new LoadingStep<string>(1.0f, "My Description", () => Task.CompletedTask);

        // Assert
        step.Status.Should().Be("My Description");
    }
}
