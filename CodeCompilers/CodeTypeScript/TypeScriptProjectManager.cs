using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCompilers.TypeScript
{
    /// <summary>
    /// Manages the creation, building, and execution of TypeScript projects.
    /// </summary>
    public class TypeScriptProjectManager
    {
        public event EventHandler<string>? OutputReceived;
        public event EventHandler<string>? ErrorReceived;

        public string ProjectPath { get; }
        public string ProjectName { get; }
        private const string MainTsFileName = "index.ts";
        private const string MainJsFileName = "index.js";

        public TypeScriptProjectManager(string projectName, string directory)
        {
            ProjectName = projectName;
            ProjectPath = Path.Combine(directory, projectName);
        }

        /// <summary>
        /// Creates the project directory, package.json, and tsconfig.json.
        /// </summary>
        public async Task CreateProjectAsync(CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(ProjectPath);
            await File.WriteAllTextAsync(Path.Combine(ProjectPath, "package.json"), GetPackageJsonContent(), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(ProjectPath, "tsconfig.json"), GetTsConfigContent(), cancellationToken);
        }

        /// <summary>
        /// Installs an NPM package and its type definitions (@types/...).
        /// </summary>
        public async Task<bool> AddDependencyAsync(string packageName, string? version = null, CancellationToken cancellationToken = default)
        {
            OutputReceived?.Invoke(this, $"--- Installing NPM package: {packageName} ---");
            string versionSuffix = string.IsNullOrEmpty(version) ? "" : $"@{version}";
            bool success = await RunCommandAsync("npm", $"install {packageName}{versionSuffix} --save", ProjectPath, cancellationToken);

            // Also try to install type definitions, common for TypeScript projects
            OutputReceived?.Invoke(this, $"--- Installing types for: {packageName} ---");
            await RunCommandAsync("npm", $"install @types/{packageName} --save-dev", ProjectPath, cancellationToken);
            return success;
        }

        /// <summary>
        /// Adds a TypeScript code file to the project.
        /// </summary>
        public async Task AddCodeFileAsync(string code, CancellationToken cancellationToken = default)
        {
            await File.WriteAllTextAsync(Path.Combine(ProjectPath, MainTsFileName), code, cancellationToken);
        }

        /// <summary>
        /// Transpiles the TypeScript code into JavaScript using 'tsc'.
        /// </summary>
        public async Task<bool> BuildAsync(CancellationToken cancellationToken = default)
        {
            OutputReceived?.Invoke(this, "--- Transpiling TypeScript to JavaScript... ---");
            return await RunCommandAsync("tsc", "", ProjectPath, cancellationToken);
        }

        /// <summary>
        /// Runs the transpiled JavaScript file using 'node'.
        /// </summary>
        public async Task<bool> RunAsync(CancellationToken cancellationToken = default)
        {
            OutputReceived?.Invoke(this, "--- Running transpiled JavaScript application with Node.js... ---");
            string jsFilePath = Path.Combine(ProjectPath, MainJsFileName);
            if (!File.Exists(jsFilePath))
            {
                ErrorReceived?.Invoke(this, $"[ERROR] JavaScript file not found: {jsFilePath}. Please build the project first.");
                return false;
            }
            return await RunCommandAsync("node", MainJsFileName, ProjectPath, cancellationToken);
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

        private string GetPackageJsonContent() => GetJavaScriptProjectManagerPackageJson();
        private string GetJavaScriptProjectManagerPackageJson() => $@"
{{
  ""name"": ""{ProjectName.ToLower()}"",
  ""version"": ""1.0.0"",
  ""description"": ""A TypeScript project generated by an autonomous agent."",
  ""main"": ""{MainJsFileName}"",
  ""scripts"": {{
    ""build"": ""tsc"",
    ""start"": ""node {MainJsFileName}""
  }},
  ""keywords"": [],
  ""author"": """",
  ""license"": ""ISC"",
  ""devDependencies"": {{
    ""typescript"": ""^5.0.0""
  }}
}}";

        private string GetTsConfigContent() => @"
{
  ""compilerOptions"": {
    ""target"": ""es2016"",
    ""module"": ""commonjs"",
    ""esModuleInterop"": true,
    ""forceConsistentCasingInFileNames"": true,
    ""strict"": true,
    ""skipLibCheck"": true
  }
}";
    }
}
