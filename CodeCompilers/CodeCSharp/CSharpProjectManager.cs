using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnthropicApp.CSharp
{
    /// <summary>
    /// Represents the type of C# project to create.
    /// </summary>
    public enum CSharpProjectType { Console, WinForms }

    /// <summary>
    /// Manages the creation, building, and execution of .NET projects via the dotnet CLI.
    /// This class is ideal for building full-fledged applications.
    /// </summary>
    public class CSharpProjectManager
    {
        /// <summary>
        /// Fires when the running dotnet process produces standard output.
        /// </summary>
        public event EventHandler<string>? OutputReceived;

        /// <summary>
        /// Fires when the running dotnet process produces standard error output.
        /// </summary>
        public event EventHandler<string>? ErrorReceived;

        public string ProjectPath { get; }
        public string ProjectName { get; }
        public CSharpProjectType ProjectType { get; }

        /// <summary>
        /// Initializes a new project manager.
        /// </summary>
        /// <param name="projectName">The name of the project (e.g., "MyNewApp").</param>
        /// <param name="directory">The base directory where the project folder will be created.</param>
        /// <param name="projectType">The type of project (Console or WinForms).</param>
        public CSharpProjectManager(string projectName, string directory, CSharpProjectType projectType)
        {
            ProjectName = projectName;
            ProjectPath = Path.Combine(directory, projectName);
            ProjectType = projectType;
        }

        /// <summary>
        /// Creates the project directory and the .csproj file on disk.
        /// </summary>
        public async Task CreateProjectAsync(CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(ProjectPath);

            string csprojContent = ProjectType switch
            {
                CSharpProjectType.WinForms => GetWinFormsProjectFileContent(),
                _ => GetConsoleProjectFileContent(),
            };

            await File.WriteAllTextAsync(Path.Combine(ProjectPath, $"{ProjectName}.csproj"), csprojContent, cancellationToken);
        }









        public async Task NugetParsePackageFileName(string codeContent)
        {
            string pattern = @"Install-Package\s+(.+)\s+-Version\s+(\d+\.\d+\.\d+)";
            Regex regex = new Regex(pattern);
            try
            {
                string[] lines = codeContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                foreach (string line in lines)
                {
                    Match match = regex.Match(line);
                    if (match.Success)
                    {
                        string packageName = match.Groups[1].Value;
                        string packageVersion = match.Groups[2].Value;
                        await AddNuGetPackageAsync(packageName, packageVersion); // Add a NuGet package
                    }
                }
            }
            catch (Exception ex)
            {
                // Package parsing failed - continue without adding packages
            }
        }

        public async Task AddCodeAsync(string code)
        {
            await Task.Run(() =>
            {
                File.WriteAllText(Path.Combine(ProjectPath, "Program.cs"), code);
            });
        }














        /// <summary>
        /// Adds a NuGet package reference to the .csproj file.
        /// </summary>
        public async Task AddNuGetPackageAsync(string packageName, string version, CancellationToken cancellationToken = default)
        {
            string csprojPath = Path.Combine(ProjectPath, $"{ProjectName}.csproj");
            string content = await File.ReadAllTextAsync(csprojPath, cancellationToken);

            string packageReference = $@"    <PackageReference Include=""{packageName}"" Version=""{version}"" />";
            content = content.Replace("<!-- NuGet packages will be added here -->", $"{packageReference}\n    <!-- NuGet packages will be added here -->");

            await File.WriteAllTextAsync(csprojPath, content, cancellationToken);
        }

        /// <summary>
        /// Adds a C# code file (e.g., Program.cs) to the project directory.
        /// </summary>
        public async Task AddCodeFileAsync(string fileName, string code, CancellationToken cancellationToken = default)
        {
            await File.WriteAllTextAsync(Path.Combine(ProjectPath, fileName), code, cancellationToken);
        }

        /// <summary>
        /// Runs 'dotnet restore' and then 'dotnet build' on the project.
        /// </summary>
        /// <returns>True if both commands succeed, otherwise false.</returns>
        public async Task<bool> RestoreAndBuildAsync(CancellationToken cancellationToken = default)
        {
            OutputReceived?.Invoke(this, "--- Restoring packages... ---");
            bool restoreSuccess = await RunDotnetCommandAsync("restore", ProjectPath, cancellationToken);
            if (!restoreSuccess)
            {
                ErrorReceived?.Invoke(this, "--- Package restore FAILED. Build cancelled. ---");
                return false;
            }

            OutputReceived?.Invoke(this, "--- Building project... ---");
            bool buildSuccess = await RunDotnetCommandAsync("build", ProjectPath, cancellationToken);
            if (!buildSuccess)
            {
                ErrorReceived?.Invoke(this, "--- Build FAILED. ---");
            }

            return buildSuccess;
        }

        /// <summary>
        /// Runs the project using 'dotnet run'.
        /// </summary>
        /// <returns>True if the command executes successfully (process exit code 0).</returns>
        public async Task<bool> RunProjectAsync(CancellationToken cancellationToken = default)
        {
            OutputReceived?.Invoke(this, "--- Running project... ---");
            return await RunDotnetCommandAsync($"run --project \"{ProjectName}.csproj\"", ProjectPath, cancellationToken);
        }

        private async Task<bool> RunDotnetCommandAsync(string arguments, string workingDirectory, CancellationToken cancellationToken)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo("dotnet", arguments)
                {
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (s, e) => { if (e.Data != null) OutputReceived?.Invoke(this, e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) ErrorReceived?.Invoke(this, e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode == 0;
        }

        private string GetConsoleProjectFileContent() => $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <!-- NuGet packages will be added here -->
  </ItemGroup>
</Project>";

        private string GetWinFormsProjectFileContent() => $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <!-- NuGet packages will be added here -->
  </ItemGroup>
</Project>";
    }
}
