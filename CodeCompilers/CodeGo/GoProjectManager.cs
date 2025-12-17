using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCompilers.Go
{
    /// <summary>
    /// Manages the creation, building, and execution of Go projects.
    /// </summary>
    public class GoProjectManager
    {
        public event EventHandler<string>? OutputReceived;
        public event EventHandler<string>? ErrorReceived;

        public string ProjectPath { get; }
        public string ModuleName { get; } // e.g., github.com/myuser/myproject
        private const string MainGoFileName = "main.go";

        public GoProjectManager(string moduleName, string directory)
        {
            ModuleName = moduleName;
            string projectName = Path.GetFileName(moduleName);
            ProjectPath = Path.Combine(directory, projectName);
        }

        /// <summary>
        /// Creates the project directory and initializes a Go module.
        /// </summary>
        public async Task<bool> CreateProjectAsync(CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(ProjectPath);
            OutputReceived?.Invoke(this, $"--- Initializing Go module: {ModuleName} ---");
            return await RunCommandAsync("go", $"mod init {ModuleName}", ProjectPath, cancellationToken);
        }

        /// <summary>
        /// Adds a Go package dependency to the project.
        /// </summary>
        public async Task<bool> AddDependencyAsync(string packagePath, CancellationToken cancellationToken = default)
        {
            OutputReceived?.Invoke(this, $"--- Getting Go package: {packagePath} ---");
            // 'go get' automatically adds the dependency to go.mod
            return await RunCommandAsync("go", $"get {packagePath}", ProjectPath, cancellationToken);
        }

        /// <summary>
        /// Adds the main Go source file to the project.
        /// </summary>
        public async Task AddCodeFileAsync(string code, CancellationToken cancellationToken = default)
        {
            await File.WriteAllTextAsync(Path.Combine(ProjectPath, MainGoFileName), code, cancellationToken);
        }

        /// <summary>
        /// Builds an executable from the Go source code.
        /// </summary>
        public async Task<bool> BuildAsync(CancellationToken cancellationToken = default)
        {
            OutputReceived?.Invoke(this, "--- Building Go application... ---");
            // The output file name will be the project's directory name by default
            return await RunCommandAsync("go", "build", ProjectPath, cancellationToken);
        }

        /// <summary>
        /// Runs the Go application directly without creating a permanent executable.
        /// </summary>
        public async Task<bool> RunAsync(CancellationToken cancellationToken = default)
        {
            OutputReceived?.Invoke(this, "--- Running Go application... ---");
            return await RunCommandAsync("go", "run .", ProjectPath, cancellationToken);
        }

        private async Task<bool> RunCommandAsync(string command, string arguments, string workingDirectory, CancellationToken cancellationToken)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(command, arguments)
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
    }
}
