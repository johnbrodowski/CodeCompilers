 
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AnthropicApp.Python
{
    /*

        static async Task Main(string[] args)
    {
        Debug.WriteLine("Python Environment Management Demo");
        Debug.WriteLine("=================================");
        
        // Create the EnhancedPythonSetup instance
        var pythonSetup = new EnhancedPythonSetup();
        
        // Subscribe to events
        pythonSetup.OutputReceived += (sender, output) => Debug.WriteLine($"[INFO] {output}");
        pythonSetup.ErrorReceived += (sender, error) => 
        {
            Debug.ForegroundColor = ConsoleColor.Red;
            Debug.WriteLine($"[ERROR] {error}");
            Debug.ResetColor();
        };
        pythonSetup.ProgressChanged += (sender, progress) => 
            Debug.WriteLine($"[PROGRESS] {progress.Description}");
        
        try
        {
            // Example flow with virtual environment activation and deactivation
            
            // 1. Check Python installation
            await pythonSetup.CheckPythonAsync(11); // Use Python 3.11
            
            // 2. Create virtual environment
            await pythonSetup.CreateVirtualEnvAsync("my_project_env");
            
            // 3. Activate virtual environment
            await pythonSetup.ActivateVirtualEnvAsync("my_project_env");
            
            // 4. Install packages in the virtual environment
            await pythonSetup.InstallPackagesAsync(new[] { "numpy", "pandas" });
            
            // 5. Verify package installation
            await pythonSetup.VerifyPackagesAsync(new[] { "numpy", "pandas" });
            
            // 6. Do some work in the virtual environment
            Debug.WriteLine("\nPerforming operations in the virtual environment...");
            await pythonSetup.RunCommandAsync(pythonSetup._pythonExe, 
                "-c \"import sys; print(f'Using Python from: {sys.executable}')\"");
            
            // 7. Deactivate virtual environment when done
            Debug.WriteLine("\nDeactivating virtual environment...");
            await pythonSetup.DeactivateVirtualEnvAsync();
            
            // 8. Verify we're back to system Python
            Debug.WriteLine("\nVerifying system Python after deactivation...");
            await pythonSetup.RunCommandAsync(pythonSetup._pythonExe, 
                "-c \"import sys; print(f'Now using Python from: {sys.executable}')\"");
            
            Debug.WriteLine("\nEnvironment management operations completed successfully!");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"\nOperation failed: {ex.MessageAnthropic}");
        }
        
        Debug.WriteLine("\nPress any key to exit...");
        Debug.ReadKey();
    }

    */

    /*


            var setup = new EnhancedPythonSetup();

        // Hook up event handlers
        setup.OutputReceived += (s, data) => Debug.WriteLine($"Output: {data}");
        setup.ErrorReceived += (s, data) => Debug.WriteLine($"Error: {data}");
        setup.ProgressChanged += (s, p) => Debug.WriteLine($"Progress: {p.Description}");
        setup.SetupCompleted += (s, r) => Debug.WriteLine($"Done: {r.success} - {r.message}");
        setup.InputRequired += (s, req) =>
        {
            Debug.WriteLine(req.prompt);
            string response = Console.ReadLine();
            req.callback(response);
        };

        // Run the setup (defaults: Python 3.9, venv, pygame, main.py)
        await setup.RunSetupAsync();

        // Or customize it:
        // await setup.RunSetupAsync(
        //     pythonVersion: 10,
        //     venvName: "myenv",
        //     packages: new[] { "numpy", "pandas" },
        //     scriptName: "analysis.py"
        // );
    }



    */

    public class EnhancedPythonSetup
    {
        // Events for real-time feedback and control
        public event EventHandler<string> OutputReceived;
        public event EventHandler<string> ErrorReceived;
        public event EventHandler<string> ProgressChanged;
        public event EventHandler<(bool success, string message)> SetupCompleted;
        public event EventHandler<(string prompt, Action<string> callback)> InputRequired;

        // Private fields
        private CancellationTokenSource _cancellationTokenSource;
        private string _pythonPath;
        private string _pythonExe;
 
        private string _venvPythonExe;


        private string? _workingDirectory = null;
     
        public EnhancedPythonSetup(string? customPythonPath = null, string? workingDirectory = null)
        {

            this._workingDirectory = workingDirectory;

            _cancellationTokenSource = new CancellationTokenSource();
            _pythonPath = customPythonPath ?? @"C:\Program Files (x86)\Microsoft Visual Studio\Shared\Python39_64";
            _pythonExe = Path.Combine(_pythonPath, "python.exe");
        }

       
        public async Task RunSetupAsync(int pythonVersion = 9, string venvName = "venv", string[] packages = null, string scriptName = "main.py")
        {
            try
            {
        
                await CheckPythonAsync(pythonVersion);
                await VerifyPythonVersionAsync();
                await AddToPathAsync();
                await CheckPipAsync();
                await CheckPylintAsync();
                await InstallChocolateyAsync();
                await CheckCurlAsync();
                await CreateVirtualEnvAsync(venvName);
                await ActivateVirtualEnvAsync(venvName);
                await InstallPackagesAsync(packages ?? new[] { "pygame" });
                await VerifyPackagesAsync(packages ?? new[] { "pygame" });
                await CompileScriptAsync(scriptName);
                await ExecuteScriptAsync(scriptName);

                OnSetupCompleted(true, "Python environment setup completed successfully.");
            }
            catch (OperationCanceledException)
            {
                OnOutputReceived("Setup was canceled by user.");
                OnSetupCompleted(false, "Setup was canceled by user.");
            }
            catch (Exception ex)
            {
                OnErrorReceived($"Error during setup: {ex.Message}");
                OnSetupCompleted(false, $"Setup failed with error: {ex.Message}");
            }
        }

 

        // Update progress and notify subscribers
        private void UpdateProgress(string stepDescription)
        {
         
            ProgressChanged?.Invoke(this, ( stepDescription));
        }

        // Cancel the setup process
        public void Cancel()
        {
            OnOutputReceived("Cancelling setup operation...");
            _cancellationTokenSource.Cancel();
        }

      
        public async Task CheckPythonAsync(int version = 9)
        {
            UpdateProgress("Checking Python Installation");
            OnOutputReceived($"Checking Python in {_pythonPath}...");

            if (!File.Exists(_pythonExe))
            {
                OnOutputReceived("Python not found. Installing...");
                await InstallPythonAsync(version);
            }
            else
            {
                OnOutputReceived("Python executable found.");
            }
        }

        private async Task InstallPythonAsync(int version)
        {
            string pythonUrl = GetPythonUrlForVersion(version);
            string installerPath = Path.Combine(Path.GetTempPath(), "python_installer.exe");

            using (WebClient client = new WebClient())
            {
                OnOutputReceived($"Downloading Python installer from {pythonUrl}...");
                await client.DownloadFileTaskAsync(pythonUrl, installerPath);
            }

            OnOutputReceived("Running Python installer...");
            string arguments = $"/quiet InstallAllUsers=1 PrependPath=0 TargetDir=\"{_pythonPath}\"";
            await RunCommandAsync(installerPath, arguments);

            File.Delete(installerPath);
            await SetEnvironmentVariableAsync("PYTHONPATH", _pythonPath);
            OnOutputReceived("Python installation completed.");
        }

        private string GetPythonUrlForVersion(int version)
        {
            return version switch
            {
                9 => "https://www.python.org/ftp/python/3.9.13/python-3.9.13-amd64.exe",
                10 => "https://www.python.org/ftp/python/3.10.11/python-3.10.11-amd64.exe",
                11 => "https://www.python.org/ftp/python/3.11.7/python-3.11.7-amd64.exe",
                12 => "https://www.python.org/ftp/python/3.12.1/python-3.12.1-amd64.exe",
                13 => "https://www.python.org/ftp/python/3.13.0/python-3.13.0a4-amd64.exe",
                _ => throw new ArgumentException($"Unsupported Python version: {version}")
            };
        }

        // Step 2: Verify Python version
        public async Task VerifyPythonVersionAsync()
        {
            UpdateProgress("Verifying Python Version");
            OnOutputReceived("Verifying Python version...");

            string output = await RunCommandAsync(_pythonExe, "--version");
            if (!output.Contains("Python 3."))
            {
                throw new Exception($"Unexpected Python version: {output}");
            }
            OnOutputReceived($"Python version verified: {output.Trim()}");
        }

        
        public async Task AddToPathAsync()
        {
            UpdateProgress("Adding Python to PATH");
            OnOutputReceived("Checking if Python is in system PATH...");

            string path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
            if (!path.Contains(_pythonPath))
            {
                OnOutputReceived("Adding Python to system PATH...");
                string newPath = $"{path};{_pythonPath}";
                await SetEnvironmentVariableAsync("PATH", newPath);
            }
            else
            {
                OnOutputReceived("Python is already in system PATH.");
            }
        }


        /// <summary>
        /// Asynchronously retrieves a list of installed pip package names from a virtual environment.
        /// </summary>
        /// <param name="venvPath">The path to the virtual environment directory.</param>
        /// <returns>A list of installed package names.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the venv directory does not exist.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the Python executable is not found in the venv.</exception>
        /// <exception cref="Exception">Thrown if the pip command fails or output parsing fails.</exception>
        public async Task<List<string>> GetInstalledPackagesAsync(string venvPath)
        {
            // Convert to absolute path for consistency
            venvPath = Path.GetFullPath(venvPath);

            // Validate the venv directory exists
            if (!Directory.Exists(venvPath))
            {
                throw new DirectoryNotFoundException($"Virtual environment directory not found: {venvPath}");
            }

            // Determine Python executable path based on OS
            string pythonExe;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                pythonExe = Path.Combine(venvPath, "Scripts", "python.exe");
            }
            else
            {
                pythonExe = Path.Combine(venvPath, "bin", "python");
            }

            // Verify Python executable exists
            if (!File.Exists(pythonExe))
            {
                throw new FileNotFoundException($"Python executable not found in venv: {pythonExe}");
            }

            // Execute 'python -m pip list --format=json' to get installed packages
            string arguments = "-m pip list --format=json";
            string output = await RunCommandAsync(pythonExe, arguments);

            // Parse the JSON output
            try
            {
                var packages = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(output);
                List<string> packageNames = new List<string>();
                if (packages != null)
                {
                    foreach (var pkg in packages)
                    {
                        if (pkg.TryGetValue("name", out string? name) && name != null)
                        {
                            packageNames.Add(name);
                        }
                    }
                }
                return packageNames;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse pip list output as JSON", ex);
            }
        }





        // Step 4: Check and install pip
        public async Task CheckPipAsync()
        {
            UpdateProgress("Checking pip Installation");
            OnOutputReceived("Checking pip installation...");

            try
            {
                await RunCommandAsync(_pythonExe, "-m pip --version");
            }
            catch
            {
                OnOutputReceived("Pip not found. Installing...");
                await RunCommandAsync(_pythonExe, "-m ensurepip --upgrade");
            }
        }

        // Step 5: Check and install pylint
        public async Task CheckPylintAsync()
        {
            UpdateProgress("Checking pylint Installation");
            OnOutputReceived("Checking pylint installation...");

            try
            {
                await RunCommandAsync(_pythonExe, "-m pylint --version");
            }
            catch
            {
                OnOutputReceived("Pylint not found. Installing...");
                await RunCommandAsync(_pythonExe, "-m pip install pylint");
            }
        }

        // Step 6: Install Chocolatey
        public async Task InstallChocolateyAsync()
        {
            UpdateProgress("Installing Chocolatey");
            OnOutputReceived("Checking for Chocolatey...");

            try
            {
                await RunCommandAsync("choco", "-v");
            }
            catch
            {
                OnOutputReceived("Installing Chocolatey...");
                string installScript = await new WebClient().DownloadStringTaskAsync("https://chocolatey.org/install.ps1");
                await RunCommandAsync("powershell", $"-NoProfile -ExecutionPolicy Bypass -Command \"{installScript}\"");
                string chocoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "chocolatey", "bin");
                await SetEnvironmentVariableAsync("PATH", $"{Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine)};{chocoPath}");
            }
        }

        // Step 7: Check and install curl
        public async Task CheckCurlAsync()
        {
            UpdateProgress("Checking curl Installation");
            OnOutputReceived("Checking curl installation...");

            try
            {
                await RunCommandAsync("curl", "--version");
            }
            catch
            {
                OnOutputReceived("Curl not found. Installing...");
                await RunCommandAsync("choco", "install curl -y");
            }
        }

        // Step 8: Create virtual environment
        public async Task CreateVirtualEnvAsync(string envName = "venv")
        {
            UpdateProgress("Creating Virtual Environment");
            if (!Directory.Exists(envName))
            {
                OnOutputReceived($"Creating virtual environment '{envName}'...");
                await RunCommandAsync(_pythonExe, $"-m venv {envName}");
            }
            else
            {
                OnOutputReceived("Virtual environment already exists.");
            }
        }
 
        public async Task ActivateVirtualEnvAsync(string envName = "venv")
        {
            UpdateProgress("Activating Virtual Environment");
            string baseDir = _workingDirectory ?? Directory.GetCurrentDirectory();
            _venvPythonExe = Path.Combine(baseDir, envName, "Scripts", "python.exe");

            if (File.Exists(_venvPythonExe))
            {
                _pythonExe = _venvPythonExe;
                OnOutputReceived($"Activated virtual environment '{envName}'. Using Python at: {_pythonExe}");
            }
            else
            {
                throw new Exception($"Virtual environment '{envName}' not found at {_venvPythonExe}.");
            }
            await Task.CompletedTask;
        }

 
        public async Task DeactivateVirtualEnvAsync()
        {
            UpdateProgress("Deactivating Virtual Environment");

            // Only deactivate if we're actually in a virtual environment
            if (_pythonExe == _venvPythonExe && _venvPythonExe != null)
            {
                // Store the original python path before reverting
                string originalPythonExe = Path.Combine(_pythonPath, "python.exe");

                // Make sure the system Python still exists
                if (File.Exists(originalPythonExe))
                {
                    _pythonExe = originalPythonExe;
                    OnOutputReceived($"Virtual environment deactivated. Switched back to system Python at: {_pythonExe}");
                }
                else
                {
                    OnErrorReceived($"System Python not found at {originalPythonExe}. Cannot deactivate virtual environment.");
                    throw new Exception("System Python executable not found. Cannot deactivate virtual environment.");
                }
            }
            else
            {
                OnOutputReceived("No active virtual environment to deactivate.");
            }

            await Task.CompletedTask; // Ensure method is awaitable
        }

 
        public async Task InstallPackagesAsync(string[] packages)
        {
            UpdateProgress("Installing Packages");
            foreach (var package in packages)
            {
                OnOutputReceived($"Checking {package} installation...");
                try
                {
                    await RunCommandAsync(_pythonExe, $"-m pip show {package}");
                }
                catch
                {
                    OnOutputReceived($"Installing {package}...");
                    await RunCommandAsync(_pythonExe, $"-m pip install {package}");
                }
            }
        }

 
        public async Task VerifyPackagesAsync(string[] packages)
        {
            UpdateProgress("Verifying Packages");
            foreach (var package in packages)
            {
                OnOutputReceived($"Verifying {package} installation...");
                string script = $"import {package}; print(f'{package} {{{package}.__version__}} installed')";
                string output = await RunCommandAsync(_pythonExe, $"-c \"{script}\"");
                if (!output.Contains(package))
                {
                    throw new Exception($"{package} verification failed.");
                }
                OnOutputReceived(output);
            }
        }

  
        public async Task CompileScriptAsync(string scriptName = "main.py")
        {
            UpdateProgress("Compiling and Running Script");
            OnOutputReceived($"Compiling {scriptName}...");
            await RunCommandAsync(_pythonExe, $"-m py_compile {scriptName}");
            OnOutputReceived("Compilation successful...");
        }

        public async Task ExecuteScriptAsync(string scriptName = "main.py")
        {
            await RunCommandAsync(_pythonExe, scriptName);
        }



      
        private async Task<string> RunCommandAsync(string command, string arguments)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = _workingDirectory ?? Directory.GetCurrentDirectory();
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                StringBuilder outputBuilder = new StringBuilder();
                StringBuilder errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        outputBuilder.AppendLine(e.Data);
                        OutputReceived?.Invoke(this, e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        errorBuilder.AppendLine(e.Data);
                        ErrorReceived?.Invoke(this, e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var processTask = Task.Run(() => process.WaitForExit(), _cancellationTokenSource.Token);

                try
                {
                    await processTask;
                }
                catch (OperationCanceledException)
                {
                    process.Kill();
                    throw;
                }

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Command failed with exit code {process.ExitCode}: {errorBuilder}");
                }

                return outputBuilder.ToString();
            }
        }

  
        private async Task SetEnvironmentVariableAsync(string name, string value)
        {
            OnOutputReceived($"Setting environment variable {name}={value}");
            await RunCommandAsync("setx", $"{name} \"{value}\" /M");
        }

        // Helper: Request user input when needed
        public async Task<string> RequestInputAsync(string prompt)
        {
            var tcs = new TaskCompletionSource<string>();
            InputRequired?.Invoke(this, (prompt, response => tcs.SetResult(response)));
            return await tcs.Task;
        }

        // Event-raising helpers
        private void OnOutputReceived(string output) => OutputReceived?.Invoke(this, output);
        private void OnErrorReceived(string error) => ErrorReceived?.Invoke(this, error);
        private void OnSetupCompleted(bool success, string message) => SetupCompleted?.Invoke(this, (success, message));
    }
}
