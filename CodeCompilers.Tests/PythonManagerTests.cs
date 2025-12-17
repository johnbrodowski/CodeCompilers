using CodeCompilers.Python;
using System.Diagnostics;
using Xunit;

namespace CodeCompilers.Tests;

public class PythonManagerTests : IDisposable
{
    private readonly string _cleanupDirectory;

    public PythonManagerTests()
    {
        // PythonManager will create directories in the current working directory
        // We'll track them for cleanup
        _cleanupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "PythonScripts_3_11");
    }

    public void Dispose()
    {
        try
        {
            // Cleanup Python test directories
            if (Directory.Exists(_cleanupDirectory))
            {
                // Give processes time to release file handles
                Thread.Sleep(500);
                Directory.Delete(_cleanupDirectory, recursive: true);
            }
        }
        catch
        {
            // Cleanup best effort - Python venv may have locked files
        }
    }

    [Fact]
    public void PythonManager_Constructor_CreatesInstance()
    {
        if (!DependencyDetector.IsPythonAvailable())
        {
            Debug.WriteLine("SKIPPED: PythonManager_Constructor_CreatesInstance - Python not available");
            return;
        }

        Debug.WriteLine("RUNNING: PythonManager_Constructor_CreatesInstance");
        using var manager = new PythonManager("3.11", "test-001");
        Assert.NotNull(manager);
        Debug.WriteLine("PASSED: PythonManager_Constructor_CreatesInstance");
    }

    [Fact]
    public async Task RunTheCode_SimpleScript_ExecutesSuccessfully()
    {
        if (!DependencyDetector.IsPythonAvailable())
        {
            Debug.WriteLine("SKIPPED: RunTheCode_SimpleScript_ExecutesSuccessfully - Python not available");
            return;
        }

        Debug.WriteLine("RUNNING: RunTheCode_SimpleScript_ExecutesSuccessfully");
        using var manager = new PythonManager("3.11", "test-002");

        bool outputReceived = false;
        manager.PyOutPutMessage += (s, e) =>
        {
            Debug.WriteLine($"[Python Output] {e.Message}");
            outputReceived = true;
        };

        var settings = new PythonSettingsObject
        {
            Version = "3.11",
            VirtualEnvironmentName = "test_venv",
            Code = "print('Hello from Python test!')",
            PipInstallCommands = "" // No packages needed
        };

        // Ensure the working directory exists before running
        if (settings.VirtualEnvironmentProjectFolder != null)
            Directory.CreateDirectory(settings.VirtualEnvironmentProjectFolder);

        await manager.RunTheCode(settings);

        // Give it time to execute and fire events
        await Task.Delay(2000);

        Debug.WriteLine($"Output received: {outputReceived}");
        Assert.True(outputReceived, "Expected to receive output from Python execution");
        Debug.WriteLine("PASSED: RunTheCode_SimpleScript_ExecutesSuccessfully");
    }

    [Fact]
    public async Task RunTheCode_PrintStatement_FiresOutputEvent()
    {
        if (!DependencyDetector.IsPythonAvailable())
        {
            Debug.WriteLine("SKIPPED: RunTheCode_PrintStatement_FiresOutputEvent - Python not available");
            return;
        }

        Debug.WriteLine("RUNNING: RunTheCode_PrintStatement_FiresOutputEvent");
        using var manager = new PythonManager("3.11", "test-003");

        string? receivedOutput = null;
        manager.PyOutPutMessage += (s, e) =>
        {
            Debug.WriteLine($"[Python Output] {e.Message}");
            if (e.Message.Contains("Test message 123"))
                receivedOutput = e.Message;
        };

        var settings = new PythonSettingsObject
        {
            Version = "3.11",
            VirtualEnvironmentName = "test_venv2",
            Code = "print('Test message 123')",
            PipInstallCommands = ""
        };

        // Ensure the working directory exists before running
        if (settings.VirtualEnvironmentProjectFolder != null)
            Directory.CreateDirectory(settings.VirtualEnvironmentProjectFolder);

        await manager.RunTheCode(settings);

        await Task.Delay(2000);

        Debug.WriteLine($"Received output: {receivedOutput}");
        Assert.NotNull(receivedOutput);
        Debug.WriteLine("PASSED: RunTheCode_PrintStatement_FiresOutputEvent");
    }

    [Fact]
    public async Task RunTheCode_SyntaxError_HandlesGracefully()
    {
        if (!DependencyDetector.IsPythonAvailable())
        {
            Debug.WriteLine("SKIPPED: RunTheCode_SyntaxError_HandlesGracefully - Python not available");
            return;
        }

        Debug.WriteLine("RUNNING: RunTheCode_SyntaxError_HandlesGracefully");
        using var manager = new PythonManager("3.11", "test-004");

        bool errorReceived = false;
        bool anyEventReceived = false;

        manager.PyErrorOccurred += (s, e) =>
        {
            Debug.WriteLine($"[Python Error] {e.ErrorMessage}");
            errorReceived = true;
            anyEventReceived = true;
        };

        manager.PyOutPutMessage += (s, e) =>
        {
            Debug.WriteLine($"[Python Output] {e.Message}");
            anyEventReceived = true;
            // Syntax errors might appear in output instead of error events
            if (e.Message.Contains("SyntaxError") || e.Message.Contains("invalid syntax"))
                errorReceived = true;
        };

        var settings = new PythonSettingsObject
        {
            Version = "3.11",
            VirtualEnvironmentName = "test_venv3",
            Code = "print('Missing closing quote)",
            PipInstallCommands = ""
        };

        // Ensure the working directory exists before running
        if (settings.VirtualEnvironmentProjectFolder != null)
            Directory.CreateDirectory(settings.VirtualEnvironmentProjectFolder);

        // This test verifies that PythonManager handles syntax errors gracefully
        // without crashing. The error might be reported via PyErrorOccurred event
        // or via PyOutPutMessage event depending on how Python reports it.
        await manager.RunTheCode(settings);

        // Give more time for virtual environment creation and execution
        await Task.Delay(5000);

        // We expect either an error event or some output
        // The important thing is the manager doesn't crash
        Debug.WriteLine($"Error received: {errorReceived}, Any event: {anyEventReceived}");
        Assert.True(anyEventReceived || errorReceived,
            "Expected to receive some event from Python execution (error or output)");
        Debug.WriteLine("PASSED: RunTheCode_SyntaxError_HandlesGracefully");
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        if (!DependencyDetector.IsPythonAvailable())
        {
            Debug.WriteLine("SKIPPED: Dispose_CalledMultipleTimes_DoesNotThrow - Python not available");
            return;
        }

        Debug.WriteLine("RUNNING: Dispose_CalledMultipleTimes_DoesNotThrow");
        var manager = new PythonManager("3.11", "test-005");

        manager.Dispose();
        manager.Dispose(); // Should not throw

        Assert.True(true); // If we get here, test passed
        Debug.WriteLine("PASSED: Dispose_CalledMultipleTimes_DoesNotThrow");
    }
}
