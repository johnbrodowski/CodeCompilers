using CodeCompilers.TypeScript;
using Xunit;

namespace CodeCompilers.Tests;

public class TypeScriptProjectManagerTests : IDisposable
{
    private readonly string _testDirectory;

    public TypeScriptProjectManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"TypeScriptTests_{Guid.NewGuid():N}");
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
    public void TypeScriptProjectManager_Constructor_CreatesInstance()
    {
        var manager = new TypeScriptProjectManager("TestTsApp", _testDirectory);
        Assert.NotNull(manager);
        Assert.Equal("TestTsApp", manager.ProjectName);
    }

    [Fact]
    public async Task CreateProjectAsync_CreatesProjectFiles()
    {
        if (!DependencyDetector.IsTypeScriptAvailable() || !DependencyDetector.IsNodeAvailable())
            return; // Skip if TypeScript or Node not installed

        var manager = new TypeScriptProjectManager("tstest", _testDirectory);

        await manager.CreateProjectAsync();

        string packageJsonPath = Path.Combine(manager.ProjectPath, "package.json");
        string tsConfigPath = Path.Combine(manager.ProjectPath, "tsconfig.json");

        Assert.True(File.Exists(packageJsonPath), "Expected package.json to be created");
        Assert.True(File.Exists(tsConfigPath), "Expected tsconfig.json to be created");
    }

    [Fact]
    public async Task AddCodeFileAsync_CreatesIndexTs()
    {
        if (!DependencyDetector.IsTypeScriptAvailable() || !DependencyDetector.IsNodeAvailable())
            return;

        var manager = new TypeScriptProjectManager("tscode", _testDirectory);
        await manager.CreateProjectAsync();

        string code = @"
console.log('Hello from TypeScript test!');

function greet(name: string): string {
    return `Hello, ${name}!`;
}

console.log(greet('World'));
";

        await manager.AddCodeFileAsync(code);

        string indexTsPath = Path.Combine(manager.ProjectPath, "index.ts");
        Assert.True(File.Exists(indexTsPath), "Expected index.ts to be created");
    }

    [Fact]
    public async Task BuildAsync_ValidTypeScript_TranspilesSuccessfully()
    {
        if (!DependencyDetector.IsTypeScriptAvailable() || !DependencyDetector.IsNodeAvailable())
            return;

        var manager = new TypeScriptProjectManager("tsbuild", _testDirectory);

        await manager.CreateProjectAsync();

        string code = @"
const message: string = 'TypeScript build test';
console.log(message);
";

        await manager.AddCodeFileAsync(code);

        bool success = await manager.BuildAsync();

        Assert.True(success, "Expected TypeScript transpilation to succeed");

        string indexJsPath = Path.Combine(manager.ProjectPath, "index.js");
        Assert.True(File.Exists(indexJsPath), "Expected index.js to be created after transpilation");
    }
}
