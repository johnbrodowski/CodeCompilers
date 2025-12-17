using CodeCompilers.Python;
using Xunit;

namespace CodeCompilers.Tests;

public class PythonManagerTests : IDisposable
{
    private readonly string _testDirectory;

    public PythonManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"PythonTests_{Guid.NewGuid():N}");
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
    public void PythonManager_Constructor_CreatesInstance()
    {
        // Skip if Python not available
        if (!DependencyDetector.IsPythonAvailable())
        {
            // In xUnit, we can use Skip but it requires a different attribute
            // For now, just return early
            return;
        }

        using var manager = new PythonManager("3.11", "test-001");
        Assert.NotNull(manager);
    }

    [Fact]
    public async Task RunTheCode_SimpleScript_ExecutesSuccessfully()
    {
        if (!DependencyDetector.IsPythonAvailable())
            return;

        using var manager = new PythonManager("3.11", "test-002");

        bool outputReceived = false;
        manager.PyOutPutMessage += (s, e) => outputReceived = true;

        var settings = new PythonSettingsObject
        {
            Version = "3.11",
            VirtualEnvironmentProjectFolder = _testDirectory,
            VirtualEnvironmentName = "test_venv",
            Code = "print('Hello from Python test!')",
            PipCommands = "" // No packages needed
        };

        await manager.RunTheCode(settings);

        // Give it a moment to process events
        await Task.Delay(500);

        Assert.True(outputReceived, "Expected to receive output from Python execution");
    }

    [Fact]
    public async Task RunTheCode_PrintStatement_FiresOutputEvent()
    {
        if (!DependencyDetector.IsPythonAvailable())
            return;

        using var manager = new PythonManager("3.11", "test-003");

        string? receivedOutput = null;
        manager.PyOutPutMessage += (s, e) =>
        {
            if (e.Message.Contains("Test message 123"))
                receivedOutput = e.Message;
        };

        var settings = new PythonSettingsObject
        {
            Version = "3.11",
            VirtualEnvironmentProjectFolder = _testDirectory,
            VirtualEnvironmentName = "test_venv2",
            Code = "print('Test message 123')",
            PipCommands = ""
        };

        await manager.RunTheCode(settings);

        await Task.Delay(1000);

        Assert.NotNull(receivedOutput);
    }

    [Fact]
    public async Task RunTheCode_SyntaxError_FiresErrorEvent()
    {
        if (!DependencyDetector.IsPythonAvailable())
            return;

        using var manager = new PythonManager("3.11", "test-004");

        bool errorReceived = false;
        manager.PyErrorOccurred += (s, e) => errorReceived = true;

        var settings = new PythonSettingsObject
        {
            Version = "3.11",
            VirtualEnvironmentProjectFolder = _testDirectory,
            VirtualEnvironmentName = "test_venv3",
            Code = "print('Missing closing quote)",
            PipCommands = ""
        };

        await manager.RunTheCode(settings);

        await Task.Delay(1000);

        Assert.True(errorReceived, "Expected to receive error event for syntax error");
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        if (!DependencyDetector.IsPythonAvailable())
            return;

        var manager = new PythonManager("3.11", "test-005");

        manager.Dispose();
        manager.Dispose(); // Should not throw

        Assert.True(true); // If we get here, test passed
    }
}
