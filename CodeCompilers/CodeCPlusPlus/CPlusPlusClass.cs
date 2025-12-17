// ---- NEW: Configuration class for flexibility ----
public enum OutputMode
{
    /// <summary>
    /// Fires separate, distinct events for compilation and execution.
    /// </summary>
    Structured,
    /// <summary>
    /// Fires all output (compilation and execution) through the CompilationCompleted event 
    /// for a single, sequential log.
    /// </summary>
    Unified
}

/// <summary>
/// Configuration options for the C++ compiler.
/// </summary>
public record CPlusPlusCompilerOptions
{
    /// <summary>
    /// Path to the C++ compiler executable.
    /// IMPORTANT: You must set this to your system's compiler path.
    /// Examples:
    /// - Windows LLVM: @"C:\Program Files\LLVM\bin\clang.exe"
    /// - Windows Visual Studio: @"C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\Llvm\x64\bin\clang.exe"
    /// - macOS: "/usr/bin/clang++"
    /// - Linux: "/usr/bin/g++"
    /// </summary>
    public string CompilerPath { get; init; } = @"C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\Llvm\x64\bin\clang.exe";

    /// <summary>
    /// Default compiler flags for console applications.
    /// </summary>
    public string DefaultConsoleFlags { get; init; } = "-std=c++20 -pedantic -Wextra -Wall";

    /// <summary>
    /// Default compiler flags for Windows GUI applications (Unicode).
    /// </summary>
    public string DefaultWindowsGuiFlags { get; init; } =
        "-std=c++20 -mwindows -lcomdlg32 -luser32 -lgdi32 -lwinspool -lshell32 -ladvapi32 -lkernel32 -DUNICODE -D_UNICODE";

    /// <summary>
    /// Default compiler flags for Windows GUI applications (ANSI/legacy).
    /// </summary>
    public string DefaultWindowsGuiFlags_ANSI { get; init; } =
        "-std=c++20 -mwindows -lcomdlg32 -luser32 -lgdi32 -lwinspool -lshell32 -ladvapi32 -lkernel32";

}


// ---- REFACTORED: The main compiler class ----
namespace CodeCompilers.Cpp
{
    using System.Diagnostics;
    using System.Text;

    public class CPlusPlusCompiler : IDisposable
    {
        public event EventHandler<CompilationCompletedEventArgs>? CompilationCompleted;
        public event EventHandler<ExecutionOutputReceivedEventArgs>? ExecutionOutputReceived;
        public event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;

        private Process? _runningProcess;

        private readonly CPlusPlusCompilerOptions _options;
        public CPlusPlusCompilerOptions Options => _options;

        // Refactored: Use constructor injection for configuration
        public CPlusPlusCompiler(CPlusPlusCompilerOptions? options = null)
        {
            _options = options ?? new CPlusPlusCompilerOptions();
        }
 
 
        private Process StartProcess(string fileName, string arguments)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    // Refactored: Use UTF8 encoding to handle a wider range of characters
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            process.EnableRaisingEvents = true;
            process.Start();
            return process;
        }

        public void KillProcess()
        {
            // No changes needed here, this is already quite clean.
            if (_runningProcess != null && !_runningProcess.HasExited)
            {
                try
                {
                    _runningProcess.Kill(true); // Kill entire process tree
                    _runningProcess.Dispose();
                    _runningProcess = null;
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"Error killing process: {ex.ToString()}");
                }
            }
        }

        public async Task<bool> CompileAsync(
               string code,
               string outputExecutablePath,
               string? customCompilerFlags = null, // NEW: Override flags per-call
               bool keepSourceFile = false,       // NEW: Control source cleanup
               CancellationToken cancellationToken = default)
        {
            string sourceFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".cpp");
            try
            {
                await File.WriteAllTextAsync(sourceFilePath, code, cancellationToken);

                // Use custom flags if provided, otherwise pick a sensible default.
                // Here, we assume console is the most common override target.
                string flagsToUse = customCompilerFlags ?? _options.DefaultConsoleFlags;
                string compileArguments = $"\"{sourceFilePath}\" -o \"{outputExecutablePath}\" {flagsToUse}";

                using var compileProcess = StartProcess(_options.CompilerPath, compileArguments);

                var stdOutTask = compileProcess.StandardOutput.ReadToEndAsync(cancellationToken);
                var stdErrTask = compileProcess.StandardError.ReadToEndAsync(cancellationToken);

                await compileProcess.WaitForExitAsync(cancellationToken);

                string output = await stdOutTask;
                string errors = await stdErrTask;

                if (compileProcess.ExitCode != 0)
                {
                    string fullMessage = $"Compilation failed.\n--- Errors ---\n{errors}\n--- Output ---\n{output}";
                    OnCompilationCompleted(fullMessage);
                    return false;
                }
                else
                {
                    // Include compiler warnings if any
                    string successMessage = "Compilation successful.";
                    if (!string.IsNullOrWhiteSpace(output)) successMessage += $"\n--- Compiler Output/Warnings ---\n{output}";
                    if (!string.IsNullOrWhiteSpace(errors)) successMessage += $"\n--- Compiler Errors/Warnings (stderr) ---\n{errors}";
                    OnCompilationCompleted(successMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"An error occurred during compilation: {ex.ToString()}");
                return false;
            }
            finally
            {
                if (!keepSourceFile && File.Exists(sourceFilePath))
                {
                    File.Delete(sourceFilePath);
                }
            }
        }

        // --- The powerful, flexible CompileAndExecuteAsync method ---
        public async Task CompileAndExecuteAsync(
            string code,
            string outputExecutableName,
            string? customCompilerFlags = null,         // NEW
            OutputMode outputMode = OutputMode.Structured, // NEW: Choose your output style
            bool keepSourceFile = false,               // NEW
            bool deleteExecutableOnCompletion = true,  // NEW: Control exe cleanup
            CancellationToken cancellationToken = default)
        {
            string executablePath = Path.Combine(Environment.CurrentDirectory, outputExecutableName);

            bool compilationSucceeded = await CompileAsync(code, executablePath, customCompilerFlags, keepSourceFile, cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            if (compilationSucceeded)
            {
                await ExecuteAsync(executablePath, outputMode, cancellationToken);
            }

            if (deleteExecutableOnCompletion && File.Exists(executablePath))
            {
                try { File.Delete(executablePath); }
                catch (Exception ex) { OnErrorOccurred($"Could not delete executable '{executablePath}': {ex.Message}"); }
            }
        }


        /// <summary>
        /// Executes a compiled program asynchronously.
        /// </summary>
        /// <param name="executablePath">The full path to the executable file to run.</param>
        /// <param name="outputMode">Determines how output from the process is reported through events.</param>
        /// <param name="cancellationToken">A token to cancel the execution.</param>
        public async Task ExecuteAsync(
            string executablePath,
            OutputMode outputMode = OutputMode.Structured,
            CancellationToken cancellationToken = default)
        {
            // 1. Pre-execution validation
            if (!File.Exists(executablePath))
            {
                OnErrorOccurred($"Executable not found at '{executablePath}'. Please compile first.");
                return;
            }

            // 2. Ensure a clean slate by killing any process this instance might still be managing.
            KillProcess();

            try
            {
                OnExecutionOutputReceived($"--- Starting execution of {Path.GetFileName(executablePath)} ---");

                // 3. Start the process and link it to the current instance
                _runningProcess = StartProcess(executablePath, "");

                // 4. Set up robust cancellation. If the token is cancelled, kill the process.
                // This prevents orphaned processes if the waiting task is cancelled.
                var cancellationRegistration = cancellationToken.Register(() => KillProcess());

                // 5. Wire up the event handlers for output, respecting the selected OutputMode
                _runningProcess.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        if (outputMode == OutputMode.Unified)
                            OnCompilationCompleted(e.Data); // Route to the "unified" log
                        else
                            OnExecutionOutputReceived(e.Data); // Route to the specific execution log
                    }
                };

                _runningProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        string errorMessage = $"[STDERR] {e.Data}";
                        if (outputMode == OutputMode.Unified)
                            OnCompilationCompleted(errorMessage);
                        else
                            OnExecutionOutputReceived(errorMessage); // Still goes to execution output, but tagged.
                    }
                };

                // 6. Begin listening for output asynchronously
                _runningProcess.BeginOutputReadLine();
                _runningProcess.BeginErrorReadLine();

                // 7. Wait for the process to exit, honoring the cancellation token
                await _runningProcess.WaitForExitAsync(cancellationToken);

                OnExecutionOutputReceived($"--- Execution finished with exit code: {_runningProcess.ExitCode} ---");
            }
            catch (OperationCanceledException)
            {
                // This is expected if cancellation was requested.
                OnExecutionOutputReceived("\n--- Execution cancelled by user. ---");
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors during process start or management
                OnErrorOccurred($"An error occurred during execution: {ex.ToString()}");
            }
            finally
            {
                // 8. Guaranteed cleanup
                if (_runningProcess != null)
                {
                    if (!_runningProcess.HasExited)
                    {
                        // This is a fallback, but KillProcess should have handled it via cancellation.
                        try { _runningProcess.Kill(true); } catch { /* Ignore */ }
                    }
                    _runningProcess.Dispose();
                    _runningProcess = null;
                }
            }
        }










        protected virtual void OnCompilationCompleted(string message)
        {
            CompilationCompleted?.Invoke(this, new CompilationCompletedEventArgs(message));
        }

        protected virtual void OnExecutionOutputReceived(string output)
        {
            ExecutionOutputReceived?.Invoke(this, new ExecutionOutputReceivedEventArgs(output));
        }

        protected virtual void OnErrorOccurred(string errorMessage)
        {
            ErrorOccurred?.Invoke(this, new ErrorOccurredEventArgs(errorMessage));
        }

        public void Dispose()
        {
            KillProcess();
            GC.SuppressFinalize(this);
        }
    }

    public class CompilationCompletedEventArgs : EventArgs
    {
        public string Message { get; }

        public CompilationCompletedEventArgs(string message)
        {
            Message = message;
        }
    }

    public class ExecutionOutputReceivedEventArgs : EventArgs
    {
        public string Output { get; }

        public ExecutionOutputReceivedEventArgs(string output)
        {
            Output = output;
        }
    }

    public class ErrorOccurredEventArgs : EventArgs
    {
        public string ErrorMessage { get; }

        public ErrorOccurredEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}