using CodeCompilers.CSharp;
using Xunit;

namespace CodeCompilers.Tests;

public class CSharpDynamicCompilerTests : IDisposable
{
    private readonly string _testCacheDirectory;

    public CSharpDynamicCompilerTests()
    {
        _testCacheDirectory = Path.Combine(Path.GetTempPath(), $"CSharpTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testCacheDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testCacheDirectory))
                Directory.Delete(_testCacheDirectory, recursive: true);
        }
        catch
        {
            // Cleanup best effort
        }
    }

    [Fact]
    public void CSharpDynamicCompiler_Constructor_CreatesInstance()
    {
        if (!DependencyDetector.IsDotNetAvailable())
            return;

        var compiler = new CSharpDynamicCompiler(_testCacheDirectory);
        Assert.NotNull(compiler);
    }

    [Fact]
    public async Task RunFromCodeAsync_SimpleHelloWorld_ExecutesSuccessfully()
    {
        if (!DependencyDetector.IsDotNetAvailable())
            return;

        var compiler = new CSharpDynamicCompiler(_testCacheDirectory);

        string code = @"
using System;

public class Program
{
    public static void Main()
    {
        Console.WriteLine(""Hello from C# test!"");
    }
}";

        bool success = await compiler.RunFromCodeAsync(code);
        Assert.True(success, "Expected C# code to compile and run successfully");
    }

    [Fact]
    public async Task RunFromCodeAsync_CompilationError_ReturnsFalse()
    {
        if (!DependencyDetector.IsDotNetAvailable())
            return;

        var compiler = new CSharpDynamicCompiler(_testCacheDirectory);

        string code = @"
using System;

public class Program
{
    public static void Main()
    {
        // Missing semicolon - compilation error
        Console.WriteLine(""Test"")
    }
}";

        bool success = await compiler.RunFromCodeAsync(code);
        Assert.False(success, "Expected compilation to fail due to syntax error");
    }

    [Fact]
    public async Task RunFromCodeAsync_WithSystemTextJson_UsesBuiltInPackage()
    {
        if (!DependencyDetector.IsDotNetAvailable())
            return;

        var compiler = new CSharpDynamicCompiler(_testCacheDirectory);

        string code = @"
using System;
using System.Text.Json;

public class Program
{
    public static void Main()
    {
        var obj = new { Name = ""Test"", Value = 42 };
        string json = JsonSerializer.Serialize(obj);
        Console.WriteLine(json);
    }
}";

        bool success = await compiler.RunFromCodeAsync(code);
        Assert.True(success, "Expected C# code with System.Text.Json to run successfully");
    }
}
