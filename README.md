# Loading Orchestrator for Godot

[![NuGet](https://img.shields.io/nuget/v/Gosferano.Godot.LoadingOrchestrator)](https://www.nuget.org/packages/Gosferano.Godot.LoadingOrchestrator)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Gosferano.Godot.LoadingOrchestrator)](https://www.nuget.org/packages/Gosferano.Godot.LoadingOrchestrator)
[![GitHub](https://img.shields.io/github/license/gosferano/godot-loading-orchestrator.svg)](https://github.com/gosferano/godot-loading-orchestrator/blob/main/LICENSE)

A lightweight, flexible async loading orchestration library for Godot 4 C# projects. Manage complex multi-step loading operations with weighted progress tracking and clean separation of concerns.

## Features

- ✅ **Weighted Progress Tracking** - Assign different weights to loading steps for accurate progress reporting
- ✅ **Generic Status Support** - Use any type for progress status (string, structs, custom classes)
- ✅ **Flexible Loading Screens** - Use any Node as a loading screen, no enforced structure
- ✅ **Async/Await Pattern** - Modern C# async patterns throughout
- ✅ **Composable Steps** - Mix `IAsyncLoadable` implementations and custom async actions
- ✅ **Clean API** - Simple, intuitive interface with minimal boilerplate
- ✅ **Error Handling** - Built-in error handling with customizable callbacks
- ✅ **Zero Dependencies** - Only requires GodotSharp

## Installation

### Via NuGet Package Manager
```bash
dotnet add package Gosferano.Godot.LoadingOrchestrator
```

### Via Package Reference
```xml
<PackageReference Include="Gosferano.Godot.LoadingOrchestrator" Version="0.2.0" />
```

## Quick Start

### 1. Create a Loading Screen
```csharp
using Godot;
using Gosferano.Godot.LoadingOrchestrator;

public partial class MyLoadingScreen : Control, ILoadingScreen<string>
{
    [Export] private Label? _statusLabel;
    [Export] private ProgressBar? _progressBar;

    public void UpdateLoadingState(float progress, string status)
    {
        if (_progressBar != null)
        {
            _progressBar.Value = progress * 100;
        }
        
        if (_statusLabel != null)
        {
            _statusLabel.Text = status;
        }
    }
}
```

### 2. Create Loadable Resources
```csharp
public class GameDatabase : IAsyncLoadable<string>
{
    public bool IsLoaded { get; private set; }

    public async Task LoadResources(Action<float, string>? onProgress = null)
    {
        onProgress?.Invoke(0f, "Loading items...");
        await LoadItems();
        
        onProgress?.Invoke(0.5f, "Loading skills...");
        await LoadSkills();
        
        onProgress?.Invoke(1f, "Complete");
        IsLoaded = true;
    }

    private async Task LoadItems() { /* ... */ }
    private async Task LoadSkills() { /* ... */ }
}
```

### 3. Orchestrate Loading
```csharp
public partial class GameLoader : Node
{
    private LoadingOrchestrator<string> _orchestrator;

    public override void _Ready()
    {
        _orchestrator = new LoadingOrchestrator<string>(GetTree());
    }

    public async Task LoadGame()
    {
        var loadingScreen = GD.Load<PackedScene>("res://LoadingScreen.tscn")
            .Instantiate<MyLoadingScreen>();

        var steps = new[]
        {
            new LoadingStep<string>(1f, new GameDatabase()),           // Loadable - no status param
            new LoadingStep<string>(2f, "Generating world", GenerateWorld),  // Action - needs status
            new LoadingStep<string>(1f, "Initializing UI", InitializeUI)
        };

        await _orchestrator.ExecuteStepsWithLoadingScreen(
            loadingScreen,
            steps,
            onComplete: async () =>
            {
                await Task.Delay(500); // Brief pause
            }
        );
    }

    private async Task GenerateWorld()
    {
        // Your world generation logic
        await Task.Delay(1000);
    }

    private async Task InitializeUI()
    {
        // Your UI initialization
        await Task.Delay(500);
    }
}
```

## Usage Examples

### Basic Loading Steps
```csharp
var orchestrator = new LoadingOrchestrator<string>(GetTree());

var steps = new[]
{
    new LoadingStep<string>(1f, "Step 1", async () => 
    {
        await Task.Delay(1000);
        GD.Print("Step 1 complete");
    }),
    new LoadingStep<string>(1f, "Step 2", async () => 
    {
        await Task.Delay(1000);
        GD.Print("Step 2 complete");
    })
};

await orchestrator.ExecuteSteps(steps, (progress, message) =>
{
    GD.Print($"{progress * 100:F1}% - {message}");
});
```

### Weighted Progress

Heavy operations get more weight, affecting overall progress:
```csharp
var steps = new[]
{
    new LoadingStep<string>(1f, "Quick task", QuickTask),      // 10% of total
    new LoadingStep<string>(8f, "Heavy task", HeavyTask),      // 80% of total
    new LoadingStep<string>(1f, "Final task", FinalTask)       // 10% of total
};

await orchestrator.ExecuteSteps(steps);
```

### Using Custom Status Types

Use structs or classes for rich progress information:
```csharp
public readonly struct LoadingProgress
{
    public string Message { get; init; }
    public int ItemsLoaded { get; init; }
    public int TotalItems { get; init; }
}

var orchestrator = new LoadingOrchestrator<LoadingProgress>(GetTree());

public class ItemLoader : IAsyncLoadable<LoadingProgress>
{
    public async Task LoadResources(Action<float, LoadingProgress>? onProgress = null)
    {
        for (int i = 0; i < 100; i++)
        {
            await LoadItem(i);
            onProgress?.Invoke(
                i / 100f, 
                new LoadingProgress 
                { 
                    Message = "Loading items",
                    ItemsLoaded = i,
                    TotalItems = 100
                }
            );
        }
    }
}
```

### Error Handling
```csharp
await orchestrator.ExecuteStepsWithLoadingScreen(
    loadingScreen,
    steps,
    onComplete: async () =>
    {
        GD.Print("Loading complete!");
    },
    onError: async (ex) =>
    {
        GD.PrintErr($"Loading failed: {ex.Message}");
        
        // Show error on loading screen
        if (loadingScreen is ILoadingScreen<string> ls)
        {
            ls.UpdateLoadingState(0f, $"Error: {ex.Message}");
        }
        
        // Wait before closing
        await Task.Delay(3000);
    }
);
```

### Scene Transitions
```csharp
public async Task ChangeScene(string scenePath)
{
    var loadingScreen = _loadingScreenScene.Instantiate<LoadingScreen>();

    await _orchestrator.ExecuteWithLoadingScreen(
        loadingScreen,
        async (progress) =>
        {
            progress(0f, "Unloading current scene...");
            await UnloadCurrentScene();
            
            progress(0.5f, "Loading new scene...");
            var newScene = await ResourceLoaderUtilities.LoadResourceAsync<PackedScene>(
                scenePath,
                p => progress(0.5f + p * 0.5f, "Loading new scene...")
            );
            
            GetTree().Root.AddChild(newScene.Instantiate());
        },
        onComplete: async () =>
        {
            await Task.Delay(200); // Brief pause
        }
    );
}
```

### Localization Support

Translate strings before passing them:
```csharp
var steps = new[]
{
    new LoadingStep<string>(1f, database),                    // Loadable controls its own messages
    new LoadingStep<string>(2f, Tr("loading.world"), GenerateWorld),   // Action uses translated status
    new LoadingStep<string>(1f, Tr("loading.ui"), InitializeUI)
};

await orchestrator.ExecuteStepsWithLoadingScreen(
    loadingScreen,
    steps,
    onComplete: async () =>
    {
        if (loadingScreen is ILoadingScreen<string> ls)
        {
            ls.UpdateLoadingState(1f, Tr("loading.complete"));
        }
        await Task.Delay(500);
    }
);
```

### Progress Tracking Without Loading Screen
```csharp
var progressReports = new List<(float progress, string message)>();

await orchestrator.ExecuteSteps(
    steps,
    (progress, message) => progressReports.Add((progress, message))
);

// Analyze progress reports
foreach (var (progress, message) in progressReports)
{
    GD.Print($"{progress * 100:F1}% - {message}");
}
```

## API Reference

### LoadingOrchestrator<TStatus>

#### Constructor
```csharp
LoadingOrchestrator<TStatus>(SceneTree sceneTree)
  where TStatus : notnull
```

#### Methods

**ExecuteSteps**
```csharp
Task ExecuteSteps(
    LoadingStep<TStatus>[] steps,
    Action<float, TStatus>? onProgress = null
)
```
Executes multiple loading steps with aggregate progress tracking.

**ExecuteWithLoadingScreen**
```csharp
Task ExecuteWithLoadingScreen(
    Node loadingScreen,
    Func<Action<float, TStatus>, Task> operation,
    Func<Task>? onComplete = null,
    Func<Exception, Task>? onError = null
)
```
Executes an operation with a loading screen, handling lifecycle and errors.

**ExecuteStepsWithLoadingScreen**
```csharp
Task ExecuteStepsWithLoadingScreen(
    Node loadingScreen,
    LoadingStep<TStatus>[] steps,
    Func<Task>? onComplete = null,
    Func<Exception, Task>? onError = null
)
```
Convenience method combining ExecuteSteps and ExecuteWithLoadingScreen.

### LoadingStep<TStatus>

#### Constructors
```csharp
// For loadables (status managed by loadable)
LoadingStep<TStatus>(float weight, IAsyncLoadable<TStatus> loadable)

// For actions (status provided by caller)
LoadingStep<TStatus>(float weight, TStatus status, Func<Task> action)
```

#### Properties
- `float Weight` - Weight for progress calculation (must be > 0)
- `TStatus? Status` - Status object (only used for actions)
- `IAsyncLoadable<TStatus>? Loadable` - Optional loadable resource
- `Func<Task>? Action` - Optional async action

### IAsyncLoadable<TStatus>
```csharp
public interface IAsyncLoadable<TStatus> where TStatus : notnull
{
    bool IsLoaded { get; }
    Task LoadResources(Action<float, TStatus>? onProgress = null);
}
```

### ILoadingScreen<TStatus>
```csharp
public interface ILoadingScreen<in TStatus> where TStatus : notnull
{
    void UpdateLoadingState(float progress, TStatus status);
}
```

## Best Practices

### Weight Assignment

Assign weights based on expected duration:
```csharp
// Quick operations: 0.5 - 1.0
new LoadingStep<string>(0.5f, "Initialize", Initialize),

// Medium operations: 1.0 - 3.0
new LoadingStep<string>(2f, "Load assets", LoadAssets),

// Heavy operations: 3.0 - 10.0
new LoadingStep<string>(8f, "Generate world", GenerateWorld)
```

### Progress Granularity

Report progress within long-running operations:
```csharp
public async Task LoadResources(Action<float, string>? onProgress = null)
{
    for (int i = 0; i < items.Length; i++)
    {
        await LoadItem(items[i]);
        float progress = (i + 1) / (float)items.Length;
        onProgress?.Invoke(progress, $"Loaded {i + 1}/{items.Length} items");
    }
}
```

### Error Recovery

Always provide error handlers for critical operations:
```csharp
onError: async (ex) =>
{
    Log.Error(ex, "Critical loading failure");
    
    // Show user-friendly message
    ShowErrorDialog("Failed to load game. Please restart.");
    
    // Attempt recovery or cleanup
    await CleanupPartialLoad();
}
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

Built with ❤️ for the Godot community.

## Support

- 🐛 [Report Issues](https://github.com/gosferano/godot-loading-orchestrator/issues)
- 💬 [Discussions](https://github.com/gosferano/godot-loading-orchestrator/discussions)
- ☕ [Sponsor the Project](https://ko-fi.com/gosferano)
- ⭐ Star the Repository