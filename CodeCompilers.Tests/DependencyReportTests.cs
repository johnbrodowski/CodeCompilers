using Xunit;
using Xunit.Abstractions;

namespace CodeCompilers.Tests;

/// <summary>
/// This test class generates a diagnostic report showing which language dependencies
/// are available on the current system. Useful for understanding which tests will run.
/// </summary>
public class DependencyReportTests
{
    private readonly ITestOutputHelper _output;

    public DependencyReportTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateDependencyReport()
    {
        _output.WriteLine("=== CodeCompilers Dependency Report ===");
        _output.WriteLine("");

        var report = DependencyDetector.GetAvailabilityReport();

        foreach (var (dependency, available) in report)
        {
            string status = available ? "✓ AVAILABLE" : "✗ NOT FOUND";
            _output.WriteLine($"{dependency,-20} {status}");
        }

        _output.WriteLine("");
        _output.WriteLine("Tests will be skipped for languages whose dependencies are not available.");
        _output.WriteLine("");

        // Special note about C++ compiler
        if (report["C++ Compiler"])
        {
            string? compilerPath = DependencyDetector.GetCppCompilerPath();
            _output.WriteLine($"C++ Compiler Path: {compilerPath}");
        }

        // Always pass - this is just informational
        Assert.True(true);
    }

    [Fact]
    public void VerifyDotNetAvailable()
    {
        // .NET SDK should always be available since we're running .NET tests
        bool dotnetAvailable = DependencyDetector.IsDotNetAvailable();

        Assert.True(dotnetAvailable,
            ".NET SDK should be available since these tests are running on .NET");
    }
}
