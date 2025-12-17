using CodeCompilers.Go;
using System.Diagnostics;
using Xunit;

namespace CodeCompilers.Tests;

public class GoProjectManagerTests : IDisposable
{
    private readonly string _testDirectory;

    public GoProjectManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"GoTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, recursive: true);
        }
        catch
        {
            // Cleanup best effort
        }
    }

    [Fact]
    public void GoProjectManager_Constructor_CreatesInstance()
    {
        Debug.WriteLine("RUNNING: GoProjectManager_Constructor_CreatesInstance");
        var manager = new GoProjectManager("github.com/test/myapp", _testDirectory);
        Assert.NotNull(manager);
        Assert.Equal("github.com/test/myapp", manager.ModuleName);
        Debug.WriteLine("PASSED: GoProjectManager_Constructor_CreatesInstance");
    }

    [Fact]
    public async Task CreateProjectAsync_InitializesGoModule()
    {
        if (!DependencyDetector.IsGoAvailable())
        {
            Debug.WriteLine("SKIPPED: CreateProjectAsync_InitializesGoModule - Go not installed");
            return;
        }

        Debug.WriteLine("RUNNING: CreateProjectAsync_InitializesGoModule");

        var manager = new GoProjectManager("github.com/test/gotest", _testDirectory);

        bool success = await manager.CreateProjectAsync();
        Assert.True(success, "Expected Go module initialization to succeed");

        string goModPath = Path.Combine(manager.ProjectPath, "go.mod");
        Assert.True(File.Exists(goModPath), "Expected go.mod file to be created");
    }

    [Fact]
    public async Task AddCodeFileAsync_CreatesMainGo()
    {
        if (!DependencyDetector.IsGoAvailable())
        {
            Debug.WriteLine("SKIPPED: AddCodeFileAsync_CreatesMainGo - Go not installed");
            return;
        }

        Debug.WriteLine("RUNNING: AddCodeFileAsync_CreatesMainGo");

        var manager = new GoProjectManager("github.com/test/gocode", _testDirectory);
        await manager.CreateProjectAsync();

        string code = @"
package main

import ""fmt""

func main() {
    fmt.Println(""Hello from Go test!"")
}";

        await manager.AddCodeFileAsync(code);

        string mainGoPath = Path.Combine(manager.ProjectPath, "main.go");
        Assert.True(File.Exists(mainGoPath), "Expected main.go to be created");
    }

    [Fact]
    public async Task RunAsync_SimpleProgram_ExecutesSuccessfully()
    {
        if (!DependencyDetector.IsGoAvailable())
        {
            Debug.WriteLine("SKIPPED: RunAsync_SimpleProgram_ExecutesSuccessfully - Go not installed");
            return;
        }

        Debug.WriteLine("RUNNING: RunAsync_SimpleProgram_ExecutesSuccessfully");

        var manager = new GoProjectManager("github.com/test/gorun", _testDirectory);

        bool outputReceived = false;
        manager.OutputReceived += (s, msg) =>
        {
            if (msg.Contains("Hello"))
                outputReceived = true;
        };

        await manager.CreateProjectAsync();

        string code = @"
package main

import ""fmt""

func main() {
    fmt.Println(""Hello from Go!"")
}";

        await manager.AddCodeFileAsync(code);

        bool success = await manager.RunAsync();

        Assert.True(success, "Expected Go program to run successfully");
        Assert.True(outputReceived, "Expected to receive output from Go program");
    }
}
