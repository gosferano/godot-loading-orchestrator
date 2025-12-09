# Loading Orchestrator for Godot

[![NuGet](https://img.shields.io/nuget/v/Gosferano.Godot.LoadingOrchestrator.svg)](https://www.nuget.org/packages/Gosferano.Godot.LoadingOrchestrator)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Gosferano.Godot.LoadingOrchestrator.svg)](https://www.nuget.org/packages/Gosferano.Godot.LoadingOrchestrator)
[![GitHub](https://img.shields.io/github/license/gosferano/godot-loading-orchestrator.svg)](https://github.com/gosferano/godot-loading-orchestrator/blob/main/LICENSE)

A lightweight, flexible async loading orchestration library for Godot 4 C# projects. Manage complex multi-step loading operations with weighted progress tracking and clean separation of concerns.

## Features

- ✅ **Weighted Progress Tracking** - Assign different weights to loading steps for accurate progress reporting
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
<PackageReference Include="Gosferano.Godot.LoadingOrchestrator" Version="1.0.0" />
```

## Quick Start

### 1. Create a Loading Screen
```csharp
using Godot;
using Gosferano.Godot.LoadingOrchestrator;

public partial class MyLoadingScreen : Control, ILoadingScreen<Status>
{
    private Label _statusLabel;
    private ProgressBar _progressBar;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("StatusLabel");
        _progressBar = GetNode<ProgressBar>("ProgressBar");
    }

    public void UpdateLoadingState(float progress, Status status)
    {
        _progressBar.Value = progress * 100;
        _statusLabel.Text = status.Message;
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
            new LoadingStep<string>(1f, "Loading database", new GameDatabase()),
            new LoadingStep<string>(2f, "Generating world", GenerateWorld),
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
        if (loadingScreen is ILoadingScreen ls)
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
    new LoadingStep<string>(1f, Tr("loading.database"), database),
    new LoadingStep<string>(2f, Tr("loading.world"), worldGenerator),
    new LoadingStep<string>(1f, Tr("loading.ui"), uiLoader)
};

await orchestrator.ExecuteStepsWithLoadingScreen(
    loadingScreen,
    steps,
    onComplete: async () =>
    {
        if (loadingScreen is ILoadingScreen ls)
        {
            ls.UpdateLoadingState(1f, Tr("loading.complete"));
        }
        await Task.Delay(500);
    }
);
```

### Combining with ResourceLoaderUtilities
```csharp
var steps = new[]
{
    new LoadingStep<string>(1f, "Loading textures", async () =>
    {
        var texture = await ResourceLoaderUtilities.LoadResourceAsync<Texture2D>(
            "res://textures/atlas.png"
        );
        ApplyTexture(texture);
    }),
    
    new LoadingStep<string>(2f, "Loading audio", async () =>
    {
        var music = await ResourceLoaderUtilities.LoadResourceAsync<AudioStream>(
            "res://audio/music.ogg"
        );
        PlayMusic(music);
    })
};
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

### LoadingOrchestrator

#### Constructor
```csharp
LoadingOrchestrator(SceneTree sceneTree)
```

#### Methods

**ExecuteSteps**
```csharp
Task ExecuteSteps(
    LoadingStep[] steps,
    Action<float, string>? onProgress = null
)
```
Executes multiple loading steps with aggregate progress tracking.

**ExecuteWithLoadingScreen**
```csharp
Task ExecuteWithLoadingScreen(
    Node loadingScreen,
    Func<Action<float, string>, Task> operation,
    Func<Task>? onComplete = null,
    Func<Exception, Task>? onError = null
)
```
Executes an operation with a loading screen, handling lifecycle and errors.

**ExecuteStepsWithLoadingScreen**
```csharp
Task ExecuteStepsWithLoadingScreen(
    Node loadingScreen,
    LoadingStep[] steps,
    Func<Task>? onComplete = null,
    Func<Exception, Task>? onError = null
)
```
Convenience method combining ExecuteSteps and ExecuteWithLoadingScreen.

### LoadingStep

#### Constructors
```csharp
LoadingStep(float weight, Status status, IAsyncLoadable loadable)
LoadingStep(float weight, Status status, Func<Task> action)
```

#### Properties
- `float Weight` - Weight for progress calculation
- `string Description` - Description or localization key

### IAsyncLoadable
```csharp
public interface IAsyncLoadable
{
    bool IsLoaded { get; }
    Task LoadResources(Action<float, string>? onProgress = null);
}
```

### ILoadingScreen
```csharp
public interface ILoadingScreen<Status>
{
    void UpdateLoadingState(float progress, Status status);
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
public async Task LoadResources(Action<float, Status>? onProgress = null)
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