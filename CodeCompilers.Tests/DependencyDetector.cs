using System.Diagnostics;

namespace CodeCompilers.Tests;

/// <summary>
/// Helper class to detect which language dependencies are installed on the system.
/// Used to skip tests when required tools are not available.
/// </summary>
public static class DependencyDetector
{
    private static readonly Dictionary<string, bool?> _cache = new();

    /// <summary>
    /// Checks if Python is available on the system.
    /// </summary>
    public static bool IsPythonAvailable(string version = "")
    {
        string key = $"python_{version}";
        if (_cache.TryGetValue(key, out bool? cached) && cached.HasValue)
            return cached.Value;

        bool available = string.IsNullOrEmpty(version)
            ? CheckCommand("python", "--version")
            : CheckCommand("python", "--version", version);

        _cache[key] = available;
        return available;
    }

    /// <summary>
    /// Checks if .NET SDK is available (for C# compilation).
    /// </summary>
    public static bool IsDotNetAvailable()
    {
        if (_cache.TryGetValue("dotnet", out bool? cached) && cached.HasValue)
            return cached.Value;

        bool available = CheckCommand("dotnet", "--version");
        _cache["dotnet"] = available;
        return available;
    }

    /// <summary>
    /// Checks if Go is available on the system.
    /// </summary>
    public static bool IsGoAvailable()
    {
        if (_cache.TryGetValue("go", out bool? cached) && cached.HasValue)
            return cached.Value;

        bool available = CheckCommand("go", "version");
        _cache["go"] = available;
        return available;
    }

    /// <summary>
    /// Checks if Rust/Cargo is available on the system.
    /// </summary>
    public static bool IsRustAvailable()
    {
        if (_cache.TryGetValue("cargo", out bool? cached) && cached.HasValue)
            return cached.Value;

        bool available = CheckCommand("cargo", "--version");
        _cache["cargo"] = available;
        return available;
    }

    /// <summary>
    /// Checks if TypeScript compiler (tsc) is available on the system.
    /// </summary>
    public static bool IsTypeScriptAvailable()
    {
        if (_cache.TryGetValue("tsc", out bool? cached) && cached.HasValue)
            return cached.Value;

        bool available = CheckCommand("tsc", "--version");
        _cache["tsc"] = available;
        return available;
    }

    /// <summary>
    /// Checks if Node.js is available on the system.
    /// </summary>
    public static bool IsNodeAvailable()
    {
        if (_cache.TryGetValue("node", out bool? cached) && cached.HasValue)
            return cached.Value;

        bool available = CheckCommand("node", "--version");
        _cache["node"] = available;
        return available;
    }

    /// <summary>
    /// Checks if a C++ compiler (clang or g++) is available on the system.
    /// </summary>
    public static bool IsCppCompilerAvailable()
    {
        if (_cache.TryGetValue("cpp", out bool? cached) && cached.HasValue)
            return cached.Value;

        // Try clang first, then g++
        bool available = CheckCommand("clang", "--version") || CheckCommand("g++", "--version");
        _cache["cpp"] = available;
        return available;
    }

    /// <summary>
    /// Gets the path to a C++ compiler if available.
    /// </summary>
    public static string? GetCppCompilerPath()
    {
        // Try common locations
        string[] possiblePaths = new[]
        {
            "clang",
            "g++",
            "/usr/bin/clang",
            "/usr/bin/clang++",
            "/usr/bin/g++",
            @"C:\Program Files\LLVM\bin\clang.exe",
            @"C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\Llvm\x64\bin\clang.exe"
        };

        foreach (var path in possiblePaths)
        {
            if (CheckCommand(path, "--version"))
                return path;
        }

        return null;
    }

    private static bool CheckCommand(string command, string arguments, string? expectedOutput = null)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000); // 5 second timeout

            if (process.ExitCode != 0)
                return false;

            if (!string.IsNullOrEmpty(expectedOutput))
                return output.Contains(expectedOutput, StringComparison.OrdinalIgnoreCase);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a summary of all available dependencies for diagnostic purposes.
    /// </summary>
    public static Dictionary<string, bool> GetAvailabilityReport()
    {
        return new Dictionary<string, bool>
        {
            ["Python"] = IsPythonAvailable(),
            [".NET SDK"] = IsDotNetAvailable(),
            ["Go"] = IsGoAvailable(),
            ["Rust/Cargo"] = IsRustAvailable(),
            ["TypeScript"] = IsTypeScriptAvailable(),
            ["Node.js"] = IsNodeAvailable(),
            ["C++ Compiler"] = IsCppCompilerAvailable()
        };
    }
}
