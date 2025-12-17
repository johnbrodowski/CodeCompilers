
using CodeCompilers.Python;
 
using Microsoft.Win32;

using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
 
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCompilers.Python
{
    /// <summary>
    /// Manages Python code execution with virtual environment support and package management.
    /// Supports Python versions 3.8 through 3.13 with automatic virtual environment creation,
    /// pip package installation, and asynchronous code execution.
    /// </summary>
    /// <example>
    /// <code>
    /// var manager = new PythonManager("3.11", "request-001");
    /// manager.PyOutPutMessage += (s, e) => Console.WriteLine(e.Message);
    ///
    /// var settings = new PythonSettingsObject { Version = "3.11", Code = "print('Hello')" };
    /// await manager.RunTheCode(settings);
    /// </code>
    /// </example>
    public class PythonManager : IDisposable
    {
        #region Fields & Constants

        // Standard library modules that don't need to be installed
        private static readonly HashSet<string> _standardLibModules = new HashSet<string>
        {
            "sys", "os", "math", "datetime", "json", "re", "logging", "random", "subprocess",
            "http", "urllib", "io", "collections", "itertools", "functools", "threading", "multiprocessing",
            "turtle", "string", "argparse", "unittest", "time", "heapq", "bisect", "array", "sets", "queue",
            "types", "tempfile", "glob", "fnmatch", "linecache", "shutil", "pickle", "copy", "hashlib", "csv",
            "xml", "xmlrpc", "email", "html", "http", "socket", "ssl", "asyncio", "asyncore", "asynchat", "logging",
            "configparser", "contextlib", "enum", "inspect", "traceback", "codecs", "select", "struct", "signal",
            "mmap", "errno", "ctypes", "thread", "multiprocessing", "copyreg", "sched", "queue", "dummy_threading",
            "weakref", "gc", "trace", "dis", "site", "cmd", "platform", "webbrowser", "pkgutil", "modulefinder",
            "runpy", "importlib", "parser", "ast", "symtable", "token", "keyword", "tokenize", "tabnanny", "pyclbr",
            "py_compile", "compileall", "dis", "pickletools", "bdb", "faulthandler", "macpath", "posixpath", "ntpath",
            "genericpath", "os2emxpath", "antigravity", "this", "scheduler", "urllib.request", "urllib.response",
            "urllib.parse", "urllib.error", "urllib.robotparser", "http.client", "ftplib", "poplib", "imaplib",
            "nntplib", "smtplib", "smtpd", "telnetlib", "uuid", "socketserver", "http.server", "http.cookies",
            "http.cookiejar", "xmlrpc.client", "xmlrpc.server", "ipaddress", "audioop", "aifc", "sunau", "wave",
            "chunk", "colorsys", "imghdr", "sndhdr", "ossaudiodev", "gettext", "locale", "turtle", "cmd", "shlex",
            "tkinter", "tkinter.ttk", "tkinter.tix", "tkinter.scrolledtext", "tkinter.messagebox", "tkinter.dnd",
            "tkinter.colorchooser", "tkinter.commondialog", "tkinter.filedialog", "tkinter.font", "tkinter.simpledialog"
        };


        public string RequestID { get; set; } = "0000";
        public string Code { get; set; } = "";

        // Configuration constants
        private const int PROCESS_TIMEOUT_MS = 120000; // 2 minutes
        private const int DEFAULT_RETRY_COUNT = 3;
        private const int DEFAULT_RETRY_DELAY_MS = 1000;

        // Process tracking and state management
        private static readonly Dictionary<string, Process> _runningProcesses = new Dictionary<string, Process>();
        private readonly object _processLock = new object();

        // Concurrency controls for compilation
        private CancellationTokenSource _compilationCts;
        private readonly object _compilationLock = new object();
        private volatile bool _isCompiling;
        private readonly SemaphoreSlim _compilationSemaphore = new SemaphoreSlim(1, 1);

        // Concurrency controls for execution
        private volatile bool _isExecuting;
        private CancellationTokenSource _executionCts;
        private readonly SemaphoreSlim _executionSemaphore = new SemaphoreSlim(1, 1);

        #endregion

        #region Events

        public event EventHandler<PyCompileResultsEventArgs> PyCompileResults;
        public event EventHandler<PyExecuteResultsEventArgs> PyExecuteResults;

        public event EventHandler<PyErrorCompileEventArgs> PyErrorCompile;
        public event EventHandler<PyErrorExecuteEventArgs> PyErrorExecute;

        public event EventHandler<PySuccessCompileEventArgs> PySuccessCompile;
        public event EventHandler<PySuccessExecuteEventArgs> PySuccessExecute;

        public event EventHandler<PyStatusUpdatedEventArgs> PyStatusUpdated;
        public event EventHandler<PyErrorOccurredEventArgs> PyErrorOccurred;
        public event EventHandler<PyOutPutMessageEventArgs> PyOutPutMessage;
        public event EventHandler<PyInputRequestedEventArgs> PyInputRequested;
        public event EventHandler<PyCompleteEventArgs> PyComplete;

        #endregion

        #region Constructor & Initialization

        public PythonManager(string pyVersion, string requestID = "0000")
        {
            // Initialize events with empty handlers to prevent null reference exceptions
            PyStatusUpdated = (sender, e) => { };
            PyErrorOccurred = (sender, e) => { };
            PyErrorCompile = (sender, e) => { };
            PySuccessCompile = (sender, e) => { };
            PyExecuteResults = (sender, e) => { };
            PyOutPutMessage = (sender, e) => { };
            PyInputRequested = (sender, e) => { };
            PyComplete = (sender, e) => { };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a Python virtual environment if one doesn't exist
        /// </summary>
        public async Task CreateVirtualEnvironment(PythonSettingsObject pythonObject)
        {
            try
            {
                await EnsureVirtualEnvAndInstallPylint(pythonObject);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to create virtual environment: {ex.Message}", pythonObject.RequestID ?? "unknown");
                throw;
            }
        }

        /// <summary>
        /// Runs the Python code specified in the PythonObject
        /// </summary>
        public async Task RunTheCode(PythonSettingsObject pythonObject)
        {
            if (pythonObject == null)
                throw new ArgumentNullException(nameof(pythonObject), "Python object cannot be null");

            string requestId = pythonObject.RequestID ?? "0000";

            // Check if execution is already in progress
            if (_isExecuting)
            {
                OnStatusUpdated("Execution already in progress, please wait", requestId);
                return;
            }

            // Validate Python object
            if (string.IsNullOrEmpty(pythonObject.Version))
            {
                OnErrorOccurred("Python version is not specified", requestId);
                return;
            }

            if (string.IsNullOrEmpty(pythonObject.Code))
            {
                OnErrorOccurred("No code provided to execute", requestId);
                return;
            }

            // Check if virtual environment exists
            bool venvExists = await RunVenvCheckScript(pythonObject);
            if (!venvExists)
            {
                OnStatusUpdated("Virtual environment not found, creating now...", requestId);
                await EnsureVirtualEnvAndInstallPylint(pythonObject);
            }

            try
            {
                await _executionSemaphore.WaitAsync();
                _isExecuting = true;
                _executionCts = new CancellationTokenSource(); 
                var token = _executionCts.Token;

                // Save code to file
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(pythonObject.ScriptFilePath));
                    await File.WriteAllTextAsync(pythonObject.ScriptFilePath, pythonObject.Code, token);
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"Error saving code to file: {ex.Message}", requestId);
                    return;
                }

                token.ThrowIfCancellationRequested();

                // Extract and save requirements
                try
                {
                    RequirementsFileBuilder(pythonObject);
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"Error generating requirements file: {ex.Message}", requestId);
                    // Continue anyway since this is not critical
                }

                token.ThrowIfCancellationRequested();

                // Install requirements
                try
                {
                    await ActivateAndInstallRequirementsAsync(pythonObject);
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"Error installing requirements: {ex.Message}", requestId);
                    // Continue anyway, the required packages might already be installed
                }

                token.ThrowIfCancellationRequested();

                // Execute the script
                await ExecutePythonScript(
                    pythonObject.ScriptFileName,
                    pythonObject.ScriptFilePath,
                    pythonObject.VirtualEnvironmentPythonExePath,
                    pythonObject.VirtualEnvironmentProjectFolder,
                    "0000"
                );
            }
            catch (OperationCanceledException)
            {
                OnStatusUpdated("Execution cancelled", requestId);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error running the code: {ex.Message}", requestId);
            }
            finally
            {
                _isExecuting = false;
                _executionSemaphore.Release();
                _executionCts?.Dispose();
                _executionCts = null;
                OnComplete("Operation complete", requestId);
            }
        }

        /// <summary>
        /// Compiles the Python code without executing it
        /// </summary>
        public async Task<bool> PyCompile(PythonSettingsObject pythonObject)
        {

            bool isSuccess = false;


            if (pythonObject == null)
                throw new ArgumentNullException(nameof(pythonObject), "Python object cannot be null");

            string requestId = pythonObject.RequestID ?? "unknown";

            if (_isCompiling)
            {
                OnStatusUpdated("Compilation already in progress", requestId);
                return false;
            }

            try
            {
                await _compilationSemaphore.WaitAsync();
                _isCompiling = true;

                lock (_compilationLock)
                {
                    _compilationCts?.Cancel();
                    _compilationCts = new CancellationTokenSource();
                }

                var token = _compilationCts.Token;

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(pythonObject.ScriptFilePath));

                await File.WriteAllTextAsync(pythonObject.ScriptFilePath, pythonObject.Code, token);

                token.ThrowIfCancellationRequested();

                await EnsureVirtualEnvAndInstallPylint(pythonObject);

                RequirementsFileBuilder(pythonObject);

                await ActivateAndInstallRequirementsAsync(pythonObject);

                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = pythonObject.VirtualEnvironmentPythonExePath,
                    Arguments = $"-c \"import py_compile; py_compile.compile(r'{pythonObject.ScriptFilePath}', doraise=True)\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = pythonObject.VirtualEnvironmentProjectFolder
                };

                var processCompletionSource = new TaskCompletionSource<bool>();

                process.EnableRaisingEvents = true;
                process.Exited += (s, e) => processCompletionSource.TrySetResult(true);

                process.Start();
                using (token.Register(() =>
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill();
                    }
                    catch { }
                }))
                {
                    // Wait for process to exit or timeout after 2 minutes
                    await Task.WhenAny(processCompletionSource.Task, Task.Delay(PROCESS_TIMEOUT_MS, token));
                    token.ThrowIfCancellationRequested();
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();




                if (process.ExitCode == 0)
                {
                    OnCompileSuccess($"Compilation successful\n\n{output}", requestId, pythonObject.Code);
                    isSuccess = true;
                }
                else
                {
                    OnCompileError($"Compilation failed: {error}\n{output ?? ""}", requestId, pythonObject.Code);
                    isSuccess = false;
                }
            }
            catch (OperationCanceledException)
            {
                OnCompileError("Compilation cancelled", requestId, pythonObject.Code);
                isSuccess = false;
            }
            catch (Exception ex)
            {
                OnCompileError($"Compilation error: {ex.Message}", requestId, pythonObject.Code);
                isSuccess = false;
            }
            finally
            {
                _isCompiling = false;
                _compilationSemaphore.Release();
                OnComplete("Operation complete", requestId);
            }

            return isSuccess;
        }

        /// <summary>
        /// Installs Python packages from pip commands
        /// </summary>
        public async Task RunPips(PythonSettingsObject pythonObject)
        {
            if (pythonObject == null)
                throw new ArgumentNullException(nameof(pythonObject), "Python object cannot be null");

            string requestId = pythonObject.RequestID ?? "unknown";

            if (string.IsNullOrEmpty(pythonObject.PipInstallCommands))
            {
                OnStatusUpdated("No pip commands provided", requestId);
                return;
            }

            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(pythonObject.PipFilePath));

                // Save pip commands to file
                await File.WriteAllTextAsync(pythonObject.PipFilePath, pythonObject.PipInstallCommands);

                // Generate requirements.txt from pip commands
                RequirementsFileFromPipCommands(pythonObject);

                // Install packages
                await ActivateAndInstallRequirementsAsync(pythonObject);

                OnStatusUpdated( $"Pip packages installed successfully", requestId);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error installing pip packages: {ex.Message}", requestId);
                throw;
            }
        }

        /// <summary>
        /// Gets a list of all installed Python versions with details
        /// </summary>
        public List<PythonInfo> GetInstalledPythonVersionsWithDetails(string RequestID)
        {
            HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<PythonInfo> installations = new List<PythonInfo>();

            try
            {
                // Method 1: Check PATH environment variable
                string pathEnv = Environment.GetEnvironmentVariable("PATH");
                if (!string.IsNullOrEmpty(pathEnv))
                {
                    string[] paths = pathEnv.Split(';');
                    foreach (string path in paths)
                    {
                        CheckPythonInstallation(Path.Combine(path, "python.exe"), seenPaths, installations, RequestID);
                    }
                }

                // Method 2: Check common installation directories
                foreach (string dir in PythonDirectories.GetCommonDirectories())
                {
                    if (Directory.Exists(dir))
                    {
                        foreach (string subDir in Directory.GetDirectories(dir))
                        {
                            CheckPythonInstallation(Path.Combine(subDir, "python.exe"), seenPaths, installations, RequestID);
                        }
                    }
                }

                // Method 3: Check Windows Registry
                string[] registryKeys = { @"SOFTWARE\Python\PythonCore", @"SOFTWARE\Wow6432Node\Python\PythonCore" };
                foreach (string regKey in registryKeys)
                {
                    try
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(regKey))
                        {
                            if (key != null)
                            {
                                foreach (var subKeyName in key.GetSubKeyNames())
                                {
                                    using (var subKey = key.OpenSubKey(subKeyName))
                                    {
                                        if (subKey != null)
                                        {
                                            var installPath = subKey.GetValue("InstallPath") as string;
                                            if (!string.IsNullOrEmpty(installPath))
                                            {
                                                CheckPythonInstallation(Path.Combine(installPath, "python.exe"), seenPaths, installations, RequestID);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error accessing registry: {ex.Message}");
                        // Continue anyway, we might find Python through other methods
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error detecting Python installations: {ex.Message}");
                OnErrorOccurred($"Error detecting Python installations: {ex.Message}", RequestID);
            }

            return installations;
        }

        /// <summary>
        /// Saves Editor content to a file
        /// </summary>
        public async Task SaveIdeContentToFileAsync(string ideId, string filePath, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(ideId))
                throw new ArgumentException("Editor ID cannot be null or empty", nameof(ideId));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            string fullPath = Path.GetFullPath(filePath);

            if (File.Exists(fullPath) && !overwrite)
            {
                throw new IOException($"File already exists: {fullPath}");
            }

            try
            {
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(fullPath, Code);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to save file: {ex.Message}", RequestID);
                throw;
            }
        }

        /// <summary>
        /// Loads Editor content from a file
        /// </summary>
        public async Task LoadIdeContentFromFileAsync(string ideId, string filePath)
        {
            if (string.IsNullOrEmpty(ideId))
                throw new ArgumentException("Editor ID cannot be null or empty", nameof(ideId));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            string fullPath = Path.GetFullPath(filePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"File not found: {fullPath}");

            try
            {
                Code = await File.ReadAllTextAsync(fullPath);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to load file: {ex.Message}", RequestID);
                throw;
            }
        }

        /// <summary>
        /// Sends input to a running Python process
        /// </summary>
        public void SendInputToProcess(string input, string RequestID = "0000")
        {
            if (string.IsNullOrEmpty(input))
                return;

            lock (_processLock)
            {
                if (_runningProcesses.TryGetValue(RequestID, out Process process))
                {
                    try
                    {
                        // Ensure the process is still running
                        if (!process.HasExited)
                        {
                            process.StandardInput.WriteLine(input);
                            process.StandardInput.Flush();
                        }
                        else
                        {
                            Debug.WriteLine($"Process for RequestID {RequestID} has already exited.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error sending input to process: {ex.Message}");
                        OnErrorOccurred($"Error sending input to process: {ex.Message}", RequestID);
                    }
                }
                else
                {
                    Debug.WriteLine($"No running process found for RequestID {RequestID}.");
                    OnStatusUpdated($"No running process found for input", RequestID);
                }
            }
        }

        /// <summary>
        /// Installs Python if not present
        /// </summary>
        public void InstallPython(string projectCompilePath, string version)
        {
            if (string.IsNullOrEmpty(projectCompilePath))
                throw new ArgumentException("Project path cannot be null or empty", nameof(projectCompilePath));

            if (string.IsNullOrEmpty(version))
                throw new ArgumentException("Version cannot be null or empty", nameof(version));

            try
            {
                string filePath = Path.Combine(projectCompilePath, "py_pip.bat");
                string batchContent = GetPythonInstallBatchContent(version);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, batchContent);

                Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                    Verb = "runas"
                });

                if (process != null)
                {
                    process.WaitForExit();
                }
                else
                {
                    throw new InvalidOperationException("Failed to start Python installation process");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error installing Python: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Installs curl and Python if needed
        /// </summary>
        public void InstallCurlAndPython(string projectCompilePath, string version)
        {
            // Could add curl installation here if needed
            InstallPython(projectCompilePath, version);
        }

        #endregion

        #region Private Helper Methods

        private string GetPythonInstallBatchContent(string version)
        {
            string theUrl = version switch
            {
                "3.8" => @"https://www.python.org/ftp/python/3.8.10/python-3.8.10-amd64.exe",
                "3.9" => @"https://www.python.org/ftp/python/3.9.13/python-3.9.13-amd64.exe",
                "3.10" => @"https://www.python.org/ftp/python/3.10.11/python-3.10.11-amd64.exe",
                "3.11" => @"https://www.python.org/ftp/python/3.11.9/python-3.11.9-amd64.exe",
                "3.12" => @"https://www.python.org/ftp/python/3.12.5/python-3.12.5-amd64.exe",
                "3.13" => @"https://www.python.org/ftp/python/3.13.0/python-3.13.0rc1-amd64.exe",
                _ => @"https://www.python.org/ftp/python/3.9.13/python-3.9.13-amd64.exe",
            };
            return BatchFileContent(version, theUrl);
        }

        private string BatchFileContent(string version, string url)
        {
            return $@"@echo off
echo Downloading Python {version} installer...
curl -o python-installer.exe {url}
if %errorlevel% neq 0 (
    echo Failed to download Python installer.
    exit /b 1
)
echo Installing Python {version}...
start /wait python-installer.exe /quiet InstallAllUsers=1 PrependPath=1
echo Verifying Python {version} installation...
py -{version} --version
if %errorlevel% neq 0 (
    echo Python {version} installation failed.
    exit /b 1
)
echo Python {version} installed successfully.
py -{version} -m pip --version
echo Installation complete.
pause";
        }




        // Modify ExecutePythonScript in PythonManager.cs to capture more output details
        public async Task ExecutePythonScript(string scriptName, string scriptPath, string pythonExePath, string workingDirectory, string RequestID = "0000")
        {
            if (!File.Exists(scriptPath))
            {
                OnErrorOccurred($"Script file not found: {scriptPath}", RequestID);
                return;
            }

            if (!File.Exists(pythonExePath))
            {
                OnErrorOccurred($"Python executable not found: {pythonExePath}", RequestID);
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = pythonExePath,
                Arguments = $"-u \"{scriptPath}\"",  // -u flag ensures unbuffered output
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };

            Process process = new Process { StartInfo = startInfo };

            // Set up event handlers for process output
            var outputBuffer = new StringBuilder();
            var errorBuffer = new StringBuilder();

            // Log important execution context
            OnOutPutMessage($"==== Python Execution Start ====", RequestID);
            OnOutPutMessage($"Script: {scriptName}", RequestID);
            OnOutPutMessage($"Python: {pythonExePath}", RequestID);
            OnOutPutMessage($"Working Directory: {workingDirectory}", RequestID);
            OnOutPutMessage($"Command: {pythonExePath} -u \"{scriptPath}\"", RequestID);
            OnOutPutMessage($"==== Script Output Below ====", RequestID);

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    lock (outputBuffer)
                    {
                        outputBuffer.AppendLine(e.Data);
                    }
                    OnOutPutMessage(e.Data, RequestID);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    lock (errorBuffer)
                    {
                        errorBuffer.AppendLine(e.Data);
                    }
                    OnExecuteError(e.Data, RequestID, Code);
                }
            };

            try
            {
                process.Start();

                // Start reading output and error asynchronously
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Store the process reference so it can be accessed later for input
                lock (_processLock)
                {
                    _runningProcesses[RequestID] = process;
                }

                // Capture process info before waiting for exit
                long startTime = DateTime.Now.Ticks;

                // Wait for the process to exit
                try
                {
                    bool exited = await Task.Run(() => process.WaitForExit(PROCESS_TIMEOUT_MS));

                    if (!exited)
                    {
                        OnStatusUpdated("Process is taking too long, attempting to terminate...", RequestID);
                        try
                        {
                            process.Kill();
                            OnExecuteError("Process execution timed out and was terminated", RequestID, Code);
                        }
                        catch (Exception ex)
                        {
                            OnErrorOccurred($"Failed to terminate process: {ex.Message}", RequestID);
                        }
                    }
                    else
                    {
                        // Process has exited normally - capture exit code immediately
                        int exitCode = process.ExitCode;
                        OnOutPutMessage($"Process exited with code: {exitCode}", RequestID);

                        // Calculate execution time
                        TimeSpan executionTime = TimeSpan.FromTicks(DateTime.Now.Ticks - startTime);

                        // Check if there was an error
                        bool hasError = exitCode != 0;
                        if (hasError)
                        {
                            string errorMessage = "An error occurred during execution";
                            lock (errorBuffer)
                            {
                                if (errorBuffer.Length > 0)
                                {
                                    errorMessage = errorBuffer.ToString();
                                }
                            }
                            OnExecuteError(errorMessage, RequestID, Code);
                        }
                        else
                        {
                            // Success case
                            string output = "";
                            lock (outputBuffer)
                            {
                                output = outputBuffer.ToString();
                            }

                            // Include execution summary in the output
                            StringBuilder summaryOutput = new StringBuilder();
                            summaryOutput.AppendLine($"==== Execution Summary ====");
                            summaryOutput.AppendLine($"Exit Code: {exitCode}");
                            summaryOutput.AppendLine($"Execution Time: {executionTime.TotalSeconds:F2} seconds");
                            summaryOutput.AppendLine($"==== End of Output ====");

                            // First send detailed output to immediate user feedback
                            OnOutPutMessage(summaryOutput.ToString(), RequestID);

                            // Then include it in the success message for the tool result
                            OnExecuteSuccess($"Execution completed successfully.\n{output}\n{summaryOutput}", RequestID);
                            OnComplete("Execution completed successfully.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"Error waiting for process: {ex.Message}", RequestID);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error executing Python script: {ex.Message}", RequestID);
            }
            finally
            {
                // Remove the process reference
                lock (_processLock)
                {
                    _runningProcesses.Remove(RequestID);
                }

                // Dispose resources
                try
                {
                    if (process != null)
                    {
                        if (!process.HasExited)
                        {
                            try
                            {
                                process.Kill();
                            }
                            catch { /* Ignore errors during kill */ }
                        }

                        // Safe disposal - don't access any properties after this
                        process.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    // Safely log disposal errors without accessing process properties
                    OnStatusUpdated($"Note: Cleanup issue occurred: {ex.Message}", RequestID);
                }
            }
        }





        private async Task<bool> RunVenvCheckScript(PythonSettingsObject pythonObject)
        {
            string requestId = pythonObject.RequestID ?? "unknown";
            OnStatusUpdated("Checking virtual environment setup...", requestId);

            // First check if Python executable path is valid
            if (string.IsNullOrEmpty(pythonObject.VirtualEnvironmentPythonExePath))
            {
                OnStatusUpdated("Python executable path is not specified", requestId);
                return false;
            }

            // Check if Python executable exists
            if (!File.Exists(pythonObject.VirtualEnvironmentPythonExePath))
            {
                OnStatusUpdated($"Python executable not found at: {pythonObject.VirtualEnvironmentPythonExePath}", requestId);
                return false;
            }

            // Check if the environment directory exists
            if (!Directory.Exists(pythonObject.VirtualEnvironmentPath))
            {
                OnStatusUpdated($"Virtual environment directory not found at: {pythonObject.VirtualEnvironmentPath}", requestId);
                return false;
            }

            // Create a script that verifies if we're in a virtual environment
            string checkScript = @"
import sys
import os
import site

# Check if we're in a virtual environment
is_venv = False
if hasattr(sys, 'real_prefix') or (hasattr(sys, 'base_prefix') and sys.base_prefix != sys.prefix):
    is_venv = True

# Check additional signs of a virtual environment
site_packages = site.getsitepackages()
venv_dirs = ['Lib', 'Scripts', 'pyvenv.cfg', 'Include']

# Print basic environment information
print(f'Python version: {sys.version}')
print(f'Executable: {sys.executable}')
print(f'Prefix: {sys.prefix}')
print(f'Is virtual environment: {is_venv}')

# Check for venv directories
venv_structure_valid = True
for dir_name in venv_dirs:
    path = os.path.join(sys.prefix, dir_name)
    exists = os.path.exists(path)
    if not exists:
        venv_structure_valid = False
    print(f'Structure check - {dir_name}: {exists}')

print(f'Virtual environment structure valid: {venv_structure_valid}')

# Final verdict: are we in a proper virtual environment?
if is_venv and venv_structure_valid:
    print('VENV_CHECK_PASSED')
else:
    print('VENV_CHECK_FAILED')
";

            // Create a temporary file for the check script
            string tempScriptPath = Path.Combine(
                pythonObject.VirtualEnvironmentProjectFolder,
                $"venv_check_{Guid.NewGuid():N}.py"
            );

            try
            {
                // Ensure the directory exists
                Directory.CreateDirectory(pythonObject.VirtualEnvironmentProjectFolder);

                // Write the check script to a file
                await File.WriteAllTextAsync(tempScriptPath, checkScript);

                // Run the check script
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = pythonObject.VirtualEnvironmentPythonExePath,
                    Arguments = tempScriptPath,
                    WorkingDirectory = pythonObject.VirtualEnvironmentProjectFolder,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };

                // Create output and error builders
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                // Setup event handlers
                process.OutputDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        lock (outputBuilder)
                        {
                            outputBuilder.AppendLine(e.Data);
                        }
                    }
                };

                process.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        lock (errorBuilder)
                        {
                            errorBuilder.AppendLine(e.Data);
                        }
                    }
                };

                // Start the process
                bool processStarted = process.Start();
                if (!processStarted)
                {
                    OnStatusUpdated($"Failed to start Python process for version {pythonObject.Version}", requestId);
                    return false;
                }

                // Begin reading output and error
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Create a timeout task
                var timeoutTask = Task.Delay(30000); // 30 second timeout
                var processTask = Task.Run(() => process.WaitForExit());

                // Wait for process completion or timeout
                if (await Task.WhenAny(processTask, timeoutTask) == timeoutTask)
                {
                    // Process timed out
                    OnStatusUpdated("Virtual environment check timed out", requestId);
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                    catch
                    {
                        // Ignore errors when killing the process
                    }
                    return false;
                }

                // Get output
                string output = outputBuilder.ToString();
                string error = errorBuilder.ToString();

                // Log the check results
                OnStatusUpdated($"Virtual environment check output: {output}", requestId);

                if (!string.IsNullOrEmpty(error))
                {
                    OnStatusUpdated($"Virtual environment check error: {error}", requestId);
                }

                // Determine if the virtual environment is valid
                bool isVenvValid = output.Contains("VENV_CHECK_PASSED");

                if (isVenvValid)
                {
                    OnStatusUpdated("Virtual environment check passed", requestId);
                }
                else
                {
                    OnStatusUpdated("Virtual environment check failed", requestId);
                }

                return isVenvValid;
            }
            catch (Exception ex)
            {
                OnStatusUpdated($"Error checking virtual environment: {ex.Message}", requestId);
                return false;
            }
            finally
            {
                // Clean up the temporary script
                try
                {
                    if (File.Exists(tempScriptPath))
                    {
                        File.Delete(tempScriptPath);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        private async Task<(bool, string)> RunCommandAsync(string command, string workingDirectory)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };
            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    Debug.WriteLine("RunCommand failed to start the process.");
                    return (false, "Failed to start the process.");
                }
                while (!process.StandardError.EndOfStream)
                {
                    string line = process.StandardError.ReadLine();
                    Debug.WriteLine("Error: " + line);
                    return (false, "Error: " + line);
                }
                await process.WaitForExitAsync();
                return (true, "Completed without errors");
            }
        }

        private async Task EnsureVirtualEnvAndInstallPylint(PythonSettingsObject pythonObject)
        {

            StringBuilder outputMessage = new StringBuilder();

            if (!VirtualEnvExists(pythonObject))
            {
                string versionToUse = (!string.IsNullOrEmpty(pythonObject.Version) &&
                                        (pythonObject.Version == "3.9" || pythonObject.Version == "3.10" ||
                                         pythonObject.Version == "3.11" || pythonObject.Version == "3.12"))
                                        ? pythonObject.Version : "3.9";
               
                // PythonVersion = versionToUse;

               pythonObject.Version = versionToUse;


                var (success, message) = await RunCommandAsync($"py -{versionToUse} -m venv {pythonObject.VirtualEnvironmentName}", pythonObject.VirtualEnvironmentProjectFolder);
               
                if (success)
                    outputMessage.AppendLine($"{message}: A virtual environment named {pythonObject.VirtualEnvironmentName} was created in {pythonObject.VirtualEnvironmentProjectFolder}");
                else
                {
                    OnErrorOccurred(message, RequestID);
                    return;
                }

                outputMessage.AppendLine($"Python version: {pythonObject.Version}");
                outputMessage.AppendLine($"Environment name: {pythonObject.VirtualEnvironmentName}");
                outputMessage.AppendLine($"Environment location: {pythonObject.VirtualEnvironmentProjectFolder}");
                outputMessage.AppendLine($"Python exe path: {pythonObject.VirtualEnvironmentPythonExePath}");

                try
                {
                    outputMessage.AppendLine("Attempting to upgrade pip...");
                    var (pipSuccess, pipMessage) = await RunCommandAsync($"\"{pythonObject.VirtualEnvironmentPythonExePath}\" -m pip install --upgrade pip", pythonObject.VirtualEnvironmentPath);
                    outputMessage.AppendLine(pipMessage);
                    outputMessage.AppendLine("Attempting to install pylint...");
                    var (pylintSuccess, pylintMessage) = await RunCommandAsync($"\"{pythonObject.VirtualEnvironmentPythonExePath}\" -m pip install pylint", pythonObject.VirtualEnvironmentPath);
                    outputMessage.AppendLine(pylintMessage);
                }
                catch (Exception ex)
                {
                    outputMessage.AppendLine($"Error installing pylint: {ex.Message}");
                }

                var packages = GetInstalledPackages(pythonObject.VirtualEnvironmentPythonExePath);
                outputMessage.AppendLine("Installed packages:");
                foreach (var pac in packages)
                    outputMessage.AppendLine($"2 {pac}");

                OnComplete(outputMessage.ToString());
            }
            else
            {
                string venvPath = Path.Combine(pythonObject.VirtualEnvironmentProjectFolder, pythonObject.VirtualEnvironmentName);
                OnComplete($"A virtual environment named {pythonObject.VirtualEnvironmentName} already exists at {pythonObject.VirtualEnvironmentProjectFolder}\nPath: {venvPath}");
            }
        }

        private bool VirtualEnvExists(PythonSettingsObject pythonObject)
        {
            return File.Exists(pythonObject.VirtualEnvironmentPythonExePath);
        }

        private void RequirementsFileFromPipCommands(PythonSettingsObject pythonObject)
        {
            try
            {
                if (File.Exists(pythonObject.PipFilePath))
                {
                    var lines = File.ReadAllLines(pythonObject.PipFilePath);
                    HashSet<string> packages = new HashSet<string>();
                    foreach (var line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("# pip install"))
                        {
                            var parts = trimmed.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 3)
                            {
                                for (int i = 3; i < parts.Length; i++)
                                {
                                    var pkg = parts[i].Trim();
                                    if (!_standardLibModules.Contains(pkg))
                                        packages.Add(pkg);
                                }
                            }
                        }
                    }
                    if (packages.Count > 0)
                    {
                        File.WriteAllLines(pythonObject.VirtualEnvironmentRequirementsTxtPath, packages);
                        Debug.WriteLine("requirements.txt created successfully.");
                    }
                    else
                    {
                        Debug.WriteLine("No packages found. requirements.txt not created.");
                    }
                }
                else
                {
                    Debug.WriteLine("Pip file not found. Cannot generate requirements.txt");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating requirements.txt: {ex.Message}");
            }
        }

        private void RequirementsFileBuilder(PythonSettingsObject pythonObject)
        {
            try
            {
                if (File.Exists(pythonObject.ScriptFilePath))
                {
                    var lines = File.ReadAllLines(pythonObject.ScriptFilePath);
                    HashSet<string> packages = new HashSet<string>();
                    foreach (var line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("# pip install"))
                        {
                            var parts = trimmed.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 3)
                            {
                                for (int i = 3; i < parts.Length; i++)
                                {
                                    var pkg = parts[i].Trim();
                                    if (!_standardLibModules.Contains(pkg))
                                        packages.Add(pkg);
                                }
                            }
                        }
                    }
                    if (packages.Count > 0)
                    {
                        File.WriteAllLines(pythonObject.VirtualEnvironmentRequirementsTxtPath, packages);
                        Debug.WriteLine("requirements.txt created successfully.");
                    }
                    else
                    {
                        Debug.WriteLine("No packages found. requirements.txt not created.");
                    }
                }
                else
                {
                    Debug.WriteLine("Script file not found. Cannot generate requirements.txt");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating requirements.txt: {ex.Message}");
            }
        }

        private async Task ActivateAndInstallRequirementsAsync(PythonSettingsObject pythonObject)
        {
            using (var process = new Process())
            {
                OnStatusUpdated("Activated virtual environment.", RequestID);
                process.StartInfo.FileName = pythonObject.VirtualEnvironmentPythonExePath;
                process.StartInfo.Arguments = $"-m pip install -r {pythonObject.VirtualEnvironmentRequirementsTxtPath}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                await Task.Run(() => process.Start());
                await process.WaitForExitAsync();
                OnStatusUpdated("Requirements installed.", pythonObject.RequestID);
                OnOutPutMessage("Requirements installed.", RequestID);
                if (process.ExitCode != 0)
                {
                    Debug.WriteLine($"Error installing requirements: {await process.StandardError.ReadToEndAsync()}");
                }
            }
        }

        private void CheckPythonInstallation(string pythonExePath, HashSet<string> seenPaths, List<PythonInfo> installations, string RequestID = "304")
        {
            if (File.Exists(pythonExePath) && !seenPaths.Contains(pythonExePath))
            {
                try
                {
                    PythonInfo info = new PythonInfo
                    {
                        Path = pythonExePath,
                        Version = GetPythonVersion(pythonExePath, RequestID ),
                        Architecture = GetPythonArchitecture(pythonExePath, RequestID),
                        IsPipInstalled = IsPipInstalled(pythonExePath, RequestID),
                        InstalledPackages = GetInstalledPackages(pythonExePath, RequestID)
                    };

                    if (!string.IsNullOrEmpty(info.Version) /*&& info.Version.Contains("3.9")*/)
                    {
                        installations.Add(info);
                        seenPaths.Add(pythonExePath);
                    }
                }
                catch { /* ignore errors */ }
            }
        }

        private string GetPythonVersion(string pythonExePath, string RequestID)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = pythonExePath;
                process.StartInfo.Arguments = "--version";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Trim();
            }
        }

        private string GetPythonArchitecture(string pythonExePath, string RequestID)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = pythonExePath;
                process.StartInfo.Arguments = "-c \"import struct; print(struct.calcsize('P') * 8)\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return output + "-bit";
            }
        }

        private bool IsPipInstalled(string pythonExePath, string RequestID)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = pythonExePath;
                process.StartInfo.Arguments = "-m pip --version";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
        }

        private List<string> GetInstalledPackages(string pythonExePath, string RequestID = "0000")
        {
            List<string> packages = new List<string>();
            using (var process = new Process())
            {
                process.StartInfo.FileName = pythonExePath;
                process.StartInfo.Arguments = "-m pip list";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines.Skip(2))
                {
                    var match = Regex.Match(line, @"^(\S+)\s+(\S+)");
                    if (match.Success)
                        packages.Add($"{match.Groups[1].Value} {match.Groups[2].Value}");
                }
            }
            return packages;
        }

        private void HandlePythonScriptOutput(string output, string RequestID)
        {
            if (!string.IsNullOrEmpty(output))
            {
                OnStatusUpdated(output, RequestID);
            }
        }

        private void HandleProcessExit(bool errorFlag, string error, string RequestID)
        {
            if (errorFlag)
                OnErrorOccurred($"Error: The process exited with an error.\n{error}", RequestID);
        }

        #endregion

        #region Batch & Environment Setup Helpers

        public async Task SetupEnvironmentAndRunScriptAsync(PythonSettingsObject pythonObject)
        {
            try
            {
                if (!Directory.Exists(pythonObject.VirtualEnvironmentProjectFolder))
                {
                    bool isPythonInstalled = await IsPythonInstalledAsync( );
                    if (isPythonInstalled)
                        OnStatusUpdated("Python is installed, env will be setup if not found.", pythonObject.RequestID);
                    else
                    {
                        // Python not installed - user needs to install manually
                    }
                }
                else
                {
                    OnStatusUpdated($"Python {pythonObject.Version} is installed.", pythonObject.RequestID);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error creating virtual environment: {ex.Message}", pythonObject.RequestID);
            }
        }

        private async Task<bool> IsPythonInstalledAsync()
        {
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "python";
                    process.StartInfo.Arguments = "--version";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    await process.WaitForExitAsync();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    return process.ExitCode == 0 && (output.Contains("Python") || error.Contains("Python"));
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Event Raisers


        // Compile Events
        protected virtual void OnCompileSuccess(string message, string RequestID, string theCode)
        {
            PySuccessCompile?.Invoke(this, new PySuccessCompileEventArgs(message, RequestID, theCode));
        }
        protected virtual void OnCompileError(string errorMessage, string RequestID, string theCode)
        {
            PyErrorCompile?.Invoke(this, new PyErrorCompileEventArgs(errorMessage, RequestID, theCode));
        }
        protected virtual void OnCompileResults(string message, string RequestID)
        {
            PyCompileResults?.Invoke(this, new PyCompileResultsEventArgs(message, RequestID));
        }


        // Execute Events
        protected virtual void OnExecuteSuccess(string message, string RequestID )
        {
            PySuccessExecute?.Invoke(this, new PySuccessExecuteEventArgs(message, RequestID));
        }
        protected virtual void OnExecuteError(string errorMessage, string RequestID, string theCode)
        {
            PyErrorExecute?.Invoke(this, new PyErrorExecuteEventArgs(errorMessage, RequestID, theCode));
        }
        protected virtual void OnExecuteResults(string message, string RequestID)
        {
            PyExecuteResults?.Invoke(this, new PyExecuteResultsEventArgs(message, RequestID));
        }



        // General Events
        protected virtual void OnErrorOccurred(string errorMessage, string RequestID)
        {
            PyErrorOccurred?.Invoke(this, new PyErrorOccurredEventArgs(errorMessage, RequestID));
        }
        protected virtual void OnStatusUpdated(string message, string RequestID)
        {
            PyStatusUpdated?.Invoke(this, new PyStatusUpdatedEventArgs(message, RequestID));
        }
        protected virtual void OnOutPutMessage(string message, string RequestID)
        {
            PyOutPutMessage?.Invoke(this, new PyOutPutMessageEventArgs(message, RequestID));
        }
        protected virtual void OnComplete(string? message = null, string? RequestID = null)
        {
            PyComplete?.Invoke(this, new PyCompleteEventArgs(message, RequestID));
        }



        #endregion

        #region IDisposable Support

        public void Dispose()
        {
            _compilationCts?.Dispose();
            _compilationSemaphore.Dispose();
            _executionCts?.Dispose();
            _executionSemaphore.Dispose();
        }

        #endregion

        #region Nested Classes

        public class PyInputRequestedEventArgs : EventArgs
        {
            public string Prompt { get; }
            public Action<string> ProvideInput { get; }
            public PyInputRequestedEventArgs(string prompt, Action<string> provideInput)
            {
                Prompt = prompt;
                ProvideInput = provideInput;
            }
        }

        public class PyStatusUpdatedEventArgs : EventArgs
        {
            public string Message { get; }
            public string RequestID { get; }
            public PyStatusUpdatedEventArgs(string message, string requestId)
            {
                Message = message;
                RequestID = requestId;
            }
        }

        public class PyCompileResultsEventArgs : EventArgs
        {
            public string Message { get; }
            public string RequestID { get; }
            public PyCompileResultsEventArgs(string message, string requestId)
            {
                Message = message;
                RequestID = requestId;
            }
        }

        public class PyExecuteResultsEventArgs : EventArgs
        {
            public string Message { get; }
            public string RequestID { get; }
            public PyExecuteResultsEventArgs(string message, string requestId)
            {
                Message = message;
                RequestID = requestId;
            }
        }
        public class PyCompleteEventArgs : EventArgs
        {
            public string? Message { get; }
            public string? RequestID { get; }
            public PyCompleteEventArgs(string? message = null, string? requestId = null)
            {
                Message = message;
                RequestID = requestId;
            }
        }

        public class PyErrorOccurredEventArgs : EventArgs
        {
            public string ErrorMessage { get; }
            public string RequestID { get; }
            public PyErrorOccurredEventArgs(string errorMessage, string requestId)
            {
                ErrorMessage = errorMessage;
                RequestID = requestId;
            }
        }

        public class PyOutPutMessageEventArgs : EventArgs
        {
            public string Message { get; }
            public string RequestID { get; }
            public PyOutPutMessageEventArgs(string message, string requestId)
            {
                Message = message;
                RequestID = requestId;
            }
        }

        public class PyErrorCompileEventArgs : EventArgs
        {
            public string CompileErrorMessage { get; }
            public string RequestID { get; }
            public string theCode { get; }
            public PyErrorCompileEventArgs(string compileErrorMessage, string requestId, string theCode)
            {
                CompileErrorMessage = compileErrorMessage;
                RequestID = requestId;
                this.theCode = theCode;
            }
        }
        public class PyErrorExecuteEventArgs : EventArgs
        {
            public string ExecuteErrorMessage { get; }
            public string RequestID { get; }
            public string theCode { get; }
            public PyErrorExecuteEventArgs(string executeErrorMessage, string requestId, string theCode)
            {
                ExecuteErrorMessage = executeErrorMessage;
                RequestID = requestId;
                this.theCode = theCode;
            }
        }

        public class PySuccessCompileEventArgs : EventArgs
        {
            public string CompileMessage { get; }
            public string RequestID { get; }
            public string theCode { get; }
            public PySuccessCompileEventArgs(string message, string requestId, string theCode)
            {
                CompileMessage = message;
                RequestID = requestId;
                this.theCode = theCode;
            }
        }

        public class PySuccessExecuteEventArgs : EventArgs
        {
            public string ExecuteMessage { get; }
            public string RequestID { get; }
            public PySuccessExecuteEventArgs(string message, string requestId)
            {
                ExecuteMessage = message;
                RequestID = requestId;
            }
        }

        #endregion
    }

    #region Supporting Classes


    public class PythonInfo
    {
        public string Version { get; set; }
        public string Path { get; set; }
        public string Architecture { get; set; }
        public bool IsPipInstalled { get; set; }
        public List<string> InstalledPackages { get; set; }
    }

    public static class PythonDirectories
    {
        private static readonly string[] ProgramFilesPaths = {
            @"C:\Python",
            @"C:\Program Files\Python\",
            @"C:\Program Files (x86)\Python\"
        };

        private static readonly string[] PythonVersions = { "", "38", "39", "310", "311", "312", "313" };

        private static string UserSpecificPath => $@"C:\Users\{Environment.UserName}\AppData\Local\Programs\Python\Python";
        private static string LocalAppDataPath => $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Programs\Python\Python";

        public static IEnumerable<string> GetCommonDirectories()
        {
            var dirs = new List<string>();
            foreach (var basePath in ProgramFilesPaths)
            {
                dirs.AddRange(PythonVersions.Select(version => $"{basePath}{version}"));
            }
            dirs.AddRange(PythonVersions.Select(version => $"{UserSpecificPath}{version}"));
            dirs.AddRange(PythonVersions.Select(version => $"{LocalAppDataPath}{version}"));
            return dirs.Distinct();
        }
    }

    #endregion
}

