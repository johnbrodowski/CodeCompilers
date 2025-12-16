using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace AnthropicApp.Python
{
    public class PythonSettingsObject : INotifyPropertyChanged
    {
        // Event for property change notification
        public event PropertyChangedEventHandler? PropertyChanged;

        // Read-only properties (can only be set during initialization or internally)
        public bool IsValidObject { get; private set; } = false;

        public string CurrentDirectory { get; } = Directory.GetCurrentDirectory();
        public string PipFileName { get; } = "pip.txt";
        public string RequirementsName { get; } = "requirements.txt";

        public string? PipInstallCommands { get; set; }
        public string? RequestID { get; set; }
        public string? Code { get; set; }

        // Properties that can't be set externally
        private string _version { get; set; } = "";

        private string? _scriptFileName { get; set; } = "main.py";
        private string? _virtualEnvironmentName { get; set; } = "venv";
        private string? _toolUseID { get; set; }

        // New properties for direct path control
        private string? _customScriptFilePath { get; set; }

        private bool _useCustomScriptPath { get; set; } = false;

        // Properties that can only be set internally
        public string? ProjectFolderName { get; private set; }

        public string? ScriptFilePath
        {
            get
            {
                // Return custom path if it's set and enabled
                if (_useCustomScriptPath && !string.IsNullOrEmpty(_customScriptFilePath))
                    return _customScriptFilePath;

                // Otherwise return computed path
                return Path.Combine(VirtualEnvironmentProjectFolder, ScriptFileName);
            }
            private set
            {
                _customScriptFilePath = value;
                _useCustomScriptPath = !string.IsNullOrEmpty(value);
                OnPropertyChanged(nameof(ScriptFilePath));
            }
        }

        public string? PipFilePath { get; private set; }
        public string? VirtualEnvironmentProjectFolder { get; private set; }
        public string? VirtualEnvironmentPath { get; private set; }
        public string? VirtualEnvironmentScriptsPath { get; private set; }
        public string? VirtualEnvironmentPythonExePath { get; private set; }
        public string? VirtualEnvironmentRequirementsTxtPath { get; private set; }

        public readonly Dictionary<string, bool> _installedVersions = new()
        {
            { "3.8", false },
            { "3.9", false },
            { "3.10", false },
            { "3.11", false },
            { "3.12", false },
            { "3.13", false }
        };

        public PythonSettingsObject(string? version = null, string scriptFileName = "main.py")
        {
            PropertyChanged += (sender, e) =>
            {
                Debug.WriteLine($"Property {e.PropertyName} was changed.");
            };

            // Initialize ScriptFileName
            _scriptFileName = scriptFileName;
        }

        public bool IsEnvironmentReady => Directory.Exists(VirtualEnvironmentPath) && File.Exists(VirtualEnvironmentPythonExePath);

        public bool IsPythonInstalled(string version) => _installedVersions.ContainsKey(version) && _installedVersions[version];

        public void UpdateInstalledStatus(string version, bool isInstalled)
        {
            if (_installedVersions.ContainsKey(version))
                _installedVersions[version] = isInstalled;
        }

        public string GetVersionStatus()
        {
            bool anyInstalled = _installedVersions.Any(x => x.Value);

            var status = new StringBuilder();
            if (anyInstalled)
            {
              
                foreach (var (version, installed) in _installedVersions)
                {
                    if (installed)
                    {
                        status.AppendLine($"Python {version}: Installed");
                        //continue;
                    }

                    // status.AppendLine($"Python {version}: {(installed ? "Installed" : "Not Installed!")}");
                }
            }
            else
            {
                return string.Empty;
            }

            return status.ToString();
        }


        // Properties with change notifications
        public string Version
        {
            get => _version;
            set
            {
                if (_version != value)
                {
                    _version = value;
                    UpdateProjectCompilePath();
                    OnPropertyChanged(nameof(Version));
                    OnVersionChanged();
                }
            }
        }

        //// Method to update PythonVersion and paths
        //public void UpdatePythonVersion(string newVersion)
        //{
        //    //if (string.IsNullOrEmpty(newVersion))
        //    //    throw new ArgumentException("Python version cannot be null or empty.");

        //    //if (Version != newVersion)
        //    //{
        //    //    TrySetVersion(newVersion);
        //    //    UpdateProjectCompilePath();
        //    //}
        //    //else
        //    //{
        //    //    PythonObjectValues();
        //    //}
        //}


        public bool TrySetVersion(string version)
        {
            if (!_installedVersions.ContainsKey(version))
                return false;
            Version = version;
            return true;
        }


        public string? VirtualEnvironmentName
        {
            get => _virtualEnvironmentName;
            set
            {
                if (_virtualEnvironmentName != value)
                {
                    _virtualEnvironmentName = value;
                    OnPropertyChanged(nameof(VirtualEnvironmentName));
                    OnVenvNameChanged();
                }
            }
        }

        public string? ScriptFileName
        {
            get => _scriptFileName;
            set
            {
                if (_scriptFileName != value)
                {
                    _scriptFileName = value;
                    OnPropertyChanged(nameof(ScriptFileName));
                    OnScriptFileNameChanged();
                }
            }
        }

        public string? ToolUseID
        {
            get => _toolUseID;
            set
            {
                if (_toolUseID != value)
                {
                    _toolUseID = value;
                    OnPropertyChanged(nameof(ToolUseID));
                }
            }
        }

        // Method to validate the object
        public bool IsValidPythonObject()
        {
            if (!string.IsNullOrEmpty(Version) &&
                !string.IsNullOrEmpty(VirtualEnvironmentName) &&
                !string.IsNullOrEmpty(ScriptFileName))
            {
                IsValidObject = true;
            }
            else
            {
                IsValidObject = false;
            }

            return IsValidObject;
        }



        // New method to set a custom script file path directly
        public void SetScriptFilePath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                throw new ArgumentException("Script file path cannot be null or empty");

            // Set the custom script path
            ScriptFilePath = fullPath;

            // Also update script filename for consistency
            _scriptFileName = Path.GetFileName(fullPath);
            OnPropertyChanged(nameof(ScriptFileName));

            // Update validity
            IsValidPythonObject();
        }

        // Method to reset to using computed paths instead of custom path
        public void ResetToComputedPaths()
        {
            _useCustomScriptPath = false;
            _customScriptFilePath = null;
            OnPropertyChanged(nameof(ScriptFilePath));
            UpdateProjectCompilePath();
        }



        // Private method to update project paths based on the current PythonVersion
        public void UpdateProjectCompilePath()
        {
            try
            {
                if (IsValidPythonObject())
                {
                    ProjectFolderName = $"PythonScripts_{Version.Replace('.', '_')}";
                    VirtualEnvironmentProjectFolder = Path.Combine(CurrentDirectory, ProjectFolderName);
                    VirtualEnvironmentPath = Path.Combine(VirtualEnvironmentProjectFolder, VirtualEnvironmentName);
                    PipFilePath = Path.Combine(VirtualEnvironmentProjectFolder, PipFileName);

                    // Only update ScriptFilePath if we're not using a custom path
                    if (!_useCustomScriptPath)
                    {
                        OnPropertyChanged(nameof(ScriptFilePath));
                    }

                    VirtualEnvironmentScriptsPath = Path.Combine(VirtualEnvironmentPath, "Scripts");
                    VirtualEnvironmentPythonExePath = Path.Combine(VirtualEnvironmentScriptsPath, "python.exe");
                    VirtualEnvironmentRequirementsTxtPath = Path.Combine(VirtualEnvironmentProjectFolder, RequirementsName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating directories: {ex.Message}");
            }

            PythonObjectValues();
        }

        public static bool CheckFileExists(string filePath)
        {
            if (File.Exists(filePath))
            {
                string fileName = Path.GetFileName(filePath);
                string directory = Path.GetDirectoryName(filePath);

                Debug.WriteLine("File Name: " + fileName);
                Debug.WriteLine("File Location: " + directory);

                return true;
            }
            else
            {
                Debug.WriteLine("File does not exist: " + filePath);
                return false;
            }
        }

        public string PythonObjectValues()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Current Version: {Version}");
            sb.AppendLine($"Settings are valid: " + (IsValidObject ? "True" : "False"));
            sb.AppendLine($"Using custom script path: {_useCustomScriptPath}");

            sb.AppendLine($"\nInstallation status:\n{GetVersionStatus()}");

            if (PipInstallCommands != null)
            {
                sb.AppendLine($"PipInstallCommands:\n{PipInstallCommands}");
            }

            sb.AppendLine($"# File Names:");
            sb.AppendLine($"ScriptFileName: {ScriptFileName}");
            sb.AppendLine($"PipFileName: {PipFileName}");
            sb.AppendLine($"RequirementsName: {RequirementsName}");
            sb.AppendLine($"VirtualEnvironmentName: {VirtualEnvironmentName}");
            sb.AppendLine($"ProjectFolderName: {ProjectFolderName}");

            sb.AppendLine($"\n# Full Paths:");
            sb.AppendLine($"CurrentDirectory: {CurrentDirectory}");
            sb.AppendLine($"VirtualEnvironmentProjectFolder: {VirtualEnvironmentProjectFolder}");
            sb.AppendLine($"ScriptFilePath: {ScriptFilePath}");
            sb.AppendLine($"PipFilePath: {PipFilePath}");
            sb.AppendLine($"VirtualEnvironmentRequirementsTxtPath: {VirtualEnvironmentRequirementsTxtPath}");
            sb.AppendLine($"VirtualEnvironmentPath: {VirtualEnvironmentPath}");
            sb.AppendLine($"VirtualEnvironmentScriptsPath: {VirtualEnvironmentScriptsPath}");
            sb.AppendLine($"VirtualEnvironmentPythonExePath: {VirtualEnvironmentPythonExePath}");

            return sb.ToString();
        }

        // Helper method to raise the PropertyChanged event
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Custom logic when Version changes
        private void OnVersionChanged()
        {
            UpdateProjectCompilePath();
            Debug.WriteLine($"\nVersion changed to: {Version}\n");
        }

        private void OnScriptFileNameChanged()
        {
            // Only update paths if we're not using a custom path
            if (!_useCustomScriptPath)
            {
                UpdateProjectCompilePath();
            }
            Debug.WriteLine($"\nScript File Name changed to: {_scriptFileName}\n");
        }

        private void OnVenvNameChanged()
        {
            UpdateProjectCompilePath();
            Debug.WriteLine($"\nVenv Name changed to: {_virtualEnvironmentName}\n");
        }
    }
}