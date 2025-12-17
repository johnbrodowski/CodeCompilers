using CodeCompilers.Rust;
using Xunit;

namespace CodeCompilers.Tests;

public class RustProjectManagerTests : IDisposable
{
    private readonly string _testDirectory;

    public RustProjectManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"RustTests_{Guid.NewGuid():N}");
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
    public void RustProjectManager_Constructor_CreatesInstance()
    {
        var manager = new RustProjectManager("test_rust_app", _testDirectory);
        Assert.NotNull(manager);
        Assert.Equal("test_rust_app", manager.ProjectName);
    }

    [Fact]
    public async Task CreateProjectAsync_InitializesCargoProject()
    {
        if (!DependencyDetector.IsRustAvailable())
            return; // Skip if Rust/Cargo not installed

        var manager = new RustProjectManager("rusttest", _testDirectory);

        bool success = await manager.CreateProjectAsync();
        Assert.True(success, "Expected Cargo project creation to succeed");

        string cargoTomlPath = Path.Combine(manager.ProjectPath, "Cargo.toml");
        Assert.True(File.Exists(cargoTomlPath), "Expected Cargo.toml to be created");
    }

    [Fact]
    public async Task UpdateMainCodeAsync_UpdatesMainRs()
    {
        if (!DependencyDetector.IsRustAvailable())
            return;

        var manager = new RustProjectManager("rustcode", _testDirectory);
        await manager.CreateProjectAsync();

        string code = @"
fn main() {
    println!(""Hello from Rust test!"");
}";

        await manager.UpdateMainCodeAsync(code);

        string mainRsPath = Path.Combine(manager.ProjectPath, "src", "main.rs");
        Assert.True(File.Exists(mainRsPath), "Expected src/main.rs to exist");

        string content = await File.ReadAllTextAsync(mainRsPath);
        Assert.Contains("Hello from Rust test!", content);
    }

    [Fact]
    public async Task RunAsync_SimpleProgram_ExecutesSuccessfully()
    {
        if (!DependencyDetector.IsRustAvailable())
            return;

        var manager = new RustProjectManager("rustrun", _testDirectory);

        bool outputReceived = false;
        manager.OutputReceived += (s, msg) =>
        {
            if (msg.Contains("Hello") || msg.Contains("Compiling"))
                outputReceived = true;
        };

        await manager.CreateProjectAsync();

        string code = @"
fn main() {
    println!(""Hello from Rust!"");
}";

        await manager.UpdateMainCodeAsync(code);

        bool success = await manager.RunAsync();

        Assert.True(success, "Expected Rust program to run successfully");
        Assert.True(outputReceived, "Expected to receive output from Rust program");
    }
}
