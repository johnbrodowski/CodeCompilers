using System.Diagnostics;
using System.Text;

namespace AnthropicApp.CodeRust
{
 
        /// <summary>
        /// Manages the creation, building, and execution of Rust projects via the Cargo CLI.
        /// Cargo is Rust's official and standard build tool.
        /// </summary>
        public class RustProjectManager
        {
            public event EventHandler<string>? OutputReceived;
            public event EventHandler<string>? ErrorReceived;

            public string ProjectPath { get; }
            public string ProjectName { get; }
            public string ParentDirectory { get; }

            public RustProjectManager(string projectName, string directory)
            {
                ProjectName = projectName;
                ParentDirectory = directory;
                ProjectPath = Path.Combine(directory, projectName);
            }

            /// <summary>
            /// Creates a new Rust project using 'cargo new'.
            /// </summary>
            public async Task<bool> CreateProjectAsync(CancellationToken cancellationToken = default)
            {
                Directory.CreateDirectory(ParentDirectory);
                OutputReceived?.Invoke(this, $"--- Creating new Rust project '{ProjectName}' with Cargo... ---");
                return await RunCommandAsync("cargo", $"new {ProjectName}", ParentDirectory, cancellationToken);
            }

            /// <summary>
            /// Adds a dependency (a "crate") to the Cargo.toml file.
            /// </summary>
            public async Task AddDependencyAsync(string crateName, string version, CancellationToken cancellationToken = default)
            {
                string cargoTomlPath = Path.Combine(ProjectPath, "Cargo.toml");
                if (!File.Exists(cargoTomlPath))
                {
                    ErrorReceived?.Invoke(this, $"[ERROR] Cargo.toml not found at {cargoTomlPath}.");
                    return;
                }

                string content = await File.ReadAllTextAsync(cargoTomlPath, cancellationToken);
                string dependencyLine = $"{crateName} = \"{version}\"";

                // Add the dependency under the [dependencies] section
                if (content.Contains("[dependencies]"))
                {
                    content = content.Replace("[dependencies]", $"[dependencies]\n{dependencyLine}");
                }
                else
                {
                    content += $"\n[dependencies]\n{dependencyLine}\n";
                }

                await File.WriteAllTextAsync(cargoTomlPath, content, cancellationToken);
            }

            /// <summary>
            /// Overwrites the main source file (src/main.rs) with new code.
            /// </summary>
            public async Task UpdateMainCodeAsync(string code, CancellationToken cancellationToken = default)
            {
                string mainRsPath = Path.Combine(ProjectPath, "src", "main.rs");
                await File.WriteAllTextAsync(mainRsPath, code, cancellationToken);
            }

            /// <summary>
            /// Builds the project using 'cargo build'.
            /// </summary>
            public async Task<bool> BuildAsync(CancellationToken cancellationToken = default)
            {
                OutputReceived?.Invoke(this, "--- Building Rust project with Cargo... ---");
                return await RunCommandAsync("cargo", "build", ProjectPath, cancellationToken);
            }

            /// <summary>
            /// Runs the project using 'cargo run'.
            /// </summary>
            public async Task<bool> RunAsync(CancellationToken cancellationToken = default)
            {
                OutputReceived?.Invoke(this, "--- Running Rust application with Cargo... ---");
                return await RunCommandAsync("cargo", "run", ProjectPath, cancellationToken);
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
// --- END OF FILE RustProjectManager.cs ---