using CodeCompilers.Cpp;
using Xunit;

namespace CodeCompilers.Tests;

public class CPlusPlusCompilerTests : IDisposable
{
    private readonly string _testDirectory;

    public CPlusPlusCompilerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CppTests_{Guid.NewGuid():N}");
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
    public void CPlusPlusCompiler_Constructor_CreatesInstance()
    {
        string? compilerPath = DependencyDetector.GetCppCompilerPath();
        if (compilerPath == null)
            return; // Skip if no C++ compiler available

        var options = new CPlusPlusCompilerOptions
        {
            CompilerPath = compilerPath
        };

        var compiler = new CPlusPlusCompiler(options);
        Assert.NotNull(compiler);
    }

    [Fact]
    public async Task CompileAsync_SimpleProgram_CompilesSuccessfully()
    {
        string? compilerPath = DependencyDetector.GetCppCompilerPath();
        if (compilerPath == null)
            return;

        var options = new CPlusPlusCompilerOptions
        {
            CompilerPath = compilerPath,
            DefaultConsoleFlags = "-std=c++20"
        };

        var compiler = new CPlusPlusCompiler(options);

        string code = @"
#include <iostream>

int main() {
    std::cout << ""Hello from C++ test!"" << std::endl;
    return 0;
}";

        string exePath = Path.Combine(_testDirectory, "test.exe");

        bool success = await compiler.CompileAsync(code, exePath);

        Assert.True(success, "Expected C++ compilation to succeed");
        Assert.True(File.Exists(exePath) || File.Exists(Path.Combine(_testDirectory, "test")),
            "Expected executable to be created");
    }

    [Fact]
    public async Task CompileAsync_SyntaxError_ReturnsFalse()
    {
        string? compilerPath = DependencyDetector.GetCppCompilerPath();
        if (compilerPath == null)
            return;

        var options = new CPlusPlusCompilerOptions
        {
            CompilerPath = compilerPath
        };

        var compiler = new CPlusPlusCompiler(options);

        bool compilationFailed = false;
        compiler.CompilationCompleted += (s, e) =>
        {
            if (e.Message.Contains("failed", StringComparison.OrdinalIgnoreCase))
                compilationFailed = true;
        };

        string code = @"
#include <iostream>

int main() {
    // Missing semicolon - syntax error
    std::cout << ""Test""
    return 0;
}";

        string exePath = Path.Combine(_testDirectory, "error_test.exe");

        bool success = await compiler.CompileAsync(code, exePath);

        Assert.False(success, "Expected compilation to fail due to syntax error");
        Assert.True(compilationFailed, "Expected compilation failed event to be raised");
    }

    [Fact]
    public async Task CompileAndExecuteAsync_SimpleProgram_ExecutesSuccessfully()
    {
        string? compilerPath = DependencyDetector.GetCppCompilerPath();
        if (compilerPath == null)
            return;

        var options = new CPlusPlusCompilerOptions
        {
            CompilerPath = compilerPath
        };

        var compiler = new CPlusPlusCompiler(options);

        bool outputReceived = false;
        compiler.ExecutionOutputReceived += (s, e) =>
        {
            if (e.Output.Contains("Hello"))
                outputReceived = true;
        };

        string code = @"
#include <iostream>

int main() {
    std::cout << ""Hello from C++!"" << std::endl;
    return 0;
}";

        await compiler.CompileAndExecuteAsync(
            code: code,
            outputExecutableName: "cpp_test.exe",
            outputMode: OutputMode.Structured
        );

        await Task.Delay(500); // Give it time to execute

        Assert.True(outputReceived, "Expected to receive output from C++ program");
    }

    [Fact]
    public void KillProcess_NoRunningProcess_DoesNotThrow()
    {
        string? compilerPath = DependencyDetector.GetCppCompilerPath();
        if (compilerPath == null)
            return;

        var options = new CPlusPlusCompilerOptions
        {
            CompilerPath = compilerPath
        };

        var compiler = new CPlusPlusCompiler(options);

        // Should not throw even if no process is running
        compiler.KillProcess();

        Assert.True(true); // If we get here, test passed
    }
}
