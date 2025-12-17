using CodeCompilers.CSharp;
using Xunit;

namespace CodeCompilers.Tests;

public class CSharpProjectManagerTests : IDisposable
{
    private readonly string _testDirectory;

    public CSharpProjectManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CSharpProjectTests_{Guid.NewGuid():N}");
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
    public void CSharpProjectManager_Constructor_CreatesInstance()
    {
        if (!DependencyDetector.IsDotNetAvailable())
            return;

        var manager = new CSharpProjectManager("TestProject", _testDirectory, CSharpProjectType.Console);
        Assert.NotNull(manager);
        Assert.Equal("TestProject", manager.ProjectName);
    }

    [Fact]
    public async Task CreateProjectAsync_ConsoleProject_CreatesProjectFiles()
    {
        if (!DependencyDetector.IsDotNetAvailable())
            return;

        var manager = new CSharpProjectManager("TestConsole", _testDirectory, CSharpProjectType.Console);

        await manager.CreateProjectAsync();

        string projectPath = Path.Combine(_testDirectory, "TestConsole");
        Assert.True(Directory.Exists(projectPath), "Expected project directory to be created");
    }

    [Fact]
    public async Task AddCodeFileAsync_ValidCode_CreatesFile()
    {
        if (!DependencyDetector.IsDotNetAvailable())
            return;

        var manager = new CSharpProjectManager("TestCode", _testDirectory, CSharpProjectType.Console);
        await manager.CreateProjectAsync();

        string code = @"
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine(""Test"");
    }
}";

        await manager.AddCodeFileAsync("Program.cs", code);

        string codePath = Path.Combine(_testDirectory, "TestCode", "Program.cs");
        Assert.True(File.Exists(codePath), "Expected code file to be created");
    }

    [Fact]
    public async Task RestoreAndBuildAsync_SimpleProject_BuildsSuccessfully()
    {
        if (!DependencyDetector.IsDotNetAvailable())
            return;

        var manager = new CSharpProjectManager("TestBuild", _testDirectory, CSharpProjectType.Console);

        bool outputReceived = false;
        manager.OutputReceived += (s, msg) => outputReceived = true;

        await manager.CreateProjectAsync();

        string code = @"
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine(""Build test"");
    }
}";

        await manager.AddCodeFileAsync("Program.cs", code);

        bool buildSuccess = await manager.RestoreAndBuildAsync();

        Assert.True(buildSuccess, "Expected build to succeed");
        Assert.True(outputReceived, "Expected to receive build output");
    }
}
