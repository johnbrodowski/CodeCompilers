using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

namespace AnthropicApp.CSharp
{
    /// <summary>
    /// Compiles and executes C# code dynamically using the Roslyn compiler API.
    /// This class is ideal for a lightweight scripting engine.
    /// </summary>
    public class CSharpDynamicCompiler
    {
        private readonly string _dependenciesPath;
        private readonly List<MetadataReference> _references = new();

        /// <summary>
        /// Initializes a new dynamic compiler.
        /// </summary>
        /// <param name="dependenciesPath">A path where NuGet packages will be downloaded and stored.</param>
        public CSharpDynamicCompiler(string dependenciesPath)
        {
            _dependenciesPath = dependenciesPath;
            Directory.CreateDirectory(_dependenciesPath);
            InitializeReferences();
        }

        /// <summary>
        /// Compiles a string of C# code and runs its Main method.
        /// Dependencies are handled in a temporary, unloadable context to prevent memory leaks.
        /// </summary>
        /// <param name="code">The C# code to run. Must contain a static Main method.</param>
        /// <returns>True if compilation and execution succeed, otherwise false.</returns>
        public async Task<bool> RunFromCodeAsync(string code)
        {
            var context = new AssemblyLoadContext(name: Guid.NewGuid().ToString(), isCollectible: true);
            bool success = false;
            try
            {
                // This event handler is scoped to the lifetime of the context
                context.Resolving += (ctx, name) => ResolveAssemblyFromPath(name, _dependenciesPath);

                var compilation = await CreateCompilationAsync(code, CancellationToken.None);
                if (compilation == null) return false;

                using var ms = new MemoryStream();
                var emitResult = compilation.Emit(ms);

                if (!HandleEmitResult(emitResult)) return false;

                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = context.LoadFromStream(ms);

                await ExecuteAssemblyMain(assembly);
                success = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Execution failed: {ex}");
                success = false;
            }
            finally
            {
                // This is the most critical step: unload the assembly and all its dependencies.
                context.Unload();
            }
            return success;
        }

        private async Task<CSharpCompilation?> CreateCompilationAsync(string code, CancellationToken cancellationToken)
        {
            await DownloadAndExtractDependenciesAsync(code, cancellationToken);

            var syntaxTree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
            return CSharpCompilation.Create(
                assemblyName: Path.GetRandomFileName(),
                syntaxTrees: new[] { syntaxTree },
                references: _references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private static async Task ExecuteAssemblyMain(Assembly assembly)
        {
            var entryPoint = assembly.EntryPoint;
            if (entryPoint == null)
            {
                Debug.WriteLine("[ERROR] No entry point (static Main method) found in the compiled code.");
                return;
            }

            object? instance = entryPoint.IsStatic ? null : Activator.CreateInstance(entryPoint.DeclaringType!);
            var parameters = entryPoint.GetParameters().Length == 0 ? null : new object[] { new string[0] };

            Debug.WriteLine("--- Starting dynamic execution ---");
            object? result = entryPoint.Invoke(instance, parameters);

            // If Main is async, await the returned task
            if (result is Task task)
            {
                await task;
            }
            Debug.WriteLine("--- Dynamic execution finished ---");
        }

        // --- Dependency and Reference Management ---

        private void InitializeReferences()
        {
            // Add references from the running AppDomain to cover base assemblies.
            var neededAssemblies = new[] { "System.Private.CoreLib", "System.Runtime", "System.Debug", "System.Linq", "System.Collections", "System.Net.Http" };
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (neededAssemblies.Contains(assembly.GetName().Name))
                {
                    _references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }
            AddReferencesFromDirectory(_dependenciesPath);
        }

        private async Task DownloadAndExtractDependenciesAsync(string code, CancellationToken cancellationToken)
        {
            // The comment format must be exactly: // Install-Package PackageName -Version X.Y.Z
            string pattern = @"//\s*Install-Package\s+([a-zA-Z0-9_.-]+)\s+-Version\s+([a-zA-Z0-9_.-]+)";
            var matches = Regex.Matches(code, pattern);

            foreach (Match match in matches)
            {
                string packageId = match.Groups[1].Value;
                string version = match.Groups[2].Value;
                await DownloadAndExtractPackageAsync(packageId, version, cancellationToken);
            }
        }

        private async Task DownloadAndExtractPackageAsync(string packageId, string version, CancellationToken cancellationToken)
        {
            try
            {
                var nugetRepo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
                var resource = await nugetRepo.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
                var nugetVersion = new NuGetVersion(version);

                string nupkgFileName = $"{packageId}.{version}.nupkg";
                string dllDir = Path.Combine(_dependenciesPath, $"{packageId}-{version}");

                if (Directory.Exists(dllDir)) return; // Assume already processed
                Directory.CreateDirectory(dllDir);

                using var packageStream = new MemoryStream();
                if (!await resource.CopyNupkgToStreamAsync(packageId, nugetVersion, packageStream, new SourceCacheContext(), NullLogger.Instance, cancellationToken))
                {
                    Debug.WriteLine($"[ERROR] Could not download package {packageId} v{version}.");
                    return;
                }

                packageStream.Seek(0, SeekOrigin.Begin);

                using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read);
                foreach (var entry in archive.Entries.Where(e => e.FullName.StartsWith("lib/") && e.FullName.EndsWith(".dll")))
                {
                    var dllPath = Path.Combine(dllDir, Path.GetFileName(entry.FullName));
                    entry.ExtractToFile(dllPath, overwrite: true);
                    if (_references.All(r => r.Display != dllPath))
                    {
                        _references.Add(MetadataReference.CreateFromFile(dllPath));
                    }
                }
                Debug.WriteLine($"Successfully processed package {packageId} v{version}.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to process package {packageId} v{version}: {ex.Message}");
            }
        }

        private Assembly? ResolveAssemblyFromPath(AssemblyName assemblyName, string searchPath)
        {
            string dllName = $"{assemblyName.Name}.dll";
            // Search in all subdirectories of the dependencies path
            var dllFiles = Directory.GetFiles(searchPath, dllName, SearchOption.AllDirectories);
            return dllFiles.Any() ? Assembly.LoadFrom(dllFiles.First()) : null;
        }

        private void AddReferencesFromDirectory(string directory)
        {
            foreach (var dll in Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories))
            {
                if (_references.All(r => r.Display != dll))
                {
                    try { _references.Add(MetadataReference.CreateFromFile(dll)); } catch { /* Ignore non-managed or invalid DLLs */ }
                }
            }
        }

        private bool HandleEmitResult(EmitResult result)
        {
            if (result.Success) return true;

            Debug.WriteLine("[ERROR] Compilation failed:");
            foreach (var diagnostic in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                Debug.WriteLine($"  {diagnostic.Id}: {diagnostic.GetMessage()} at {diagnostic.Location}");
            }
            return false;
        }
    }
}
