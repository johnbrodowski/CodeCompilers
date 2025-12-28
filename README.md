# About this code
This example was extracted from AGPA — my fully autonomous general-purpose agent (closed-source, ~150k LOC).

# CodeCompilers

A comprehensive .NET library for compiling and executing code in multiple programming languages. Ideal for building code execution environments, educational tools, testing frameworks, and development tools.

## 🚀 Features

- **Multi-Language Support**: Python, C#, Go, Rust, TypeScript, and C++
- **Event-Driven Architecture**: Real-time output and error handling through events
- **Async/Await**: Modern asynchronous patterns throughout
- **Virtual Environment Management**: Automatic Python virtual environment setup
- **NuGet Package Integration**: Dynamic C# compilation with NuGet package support
- **Process Management**: Built-in timeout handling and process cleanup
- **Cross-Platform Ready**: Works with .NET 10.0+

## 📦 Supported Languages

| Language | Status | Compiler/Runtime | Features |
|----------|--------|-----------------|----------|
| **Python** | ✅ Tested | CPython 3.8-3.13 | Virtual environments, pip packages, multiple versions |
| **C#** | ✅ Tested | Roslyn + dotnet CLI | Dynamic compilation, NuGet packages, project generation |
| **C++** | ✅ Tested | Clang/MSVC | Configurable compiler flags, Windows GUI support |
| **Go** | ⚠️ Experimental | go CLI | Module management, build & run |
| **Rust** | ⚠️ Experimental | Cargo | Crate dependencies, build & run |
| **TypeScript** | ⚠️ Experimental | tsc + Node.js | NPM packages, transpilation |

**Legend:**
- ✅ **Tested** - Fully tested with comprehensive test suite
- ⚠️ **Experimental** - Code complete but not yet tested; contributions welcome!

## 🔧 Installation

### Prerequisites

Ensure the following are installed on your system:
- **.NET 10.0 SDK** or later
- **Language runtimes** for languages you want to use:
  - Python 3.8+ (for Python support)
  - .NET SDK (for C# support)
  - Go (for Go support)
  - Rust + Cargo (for Rust support)
  - Node.js + TypeScript (for TypeScript support)
  - Clang or MSVC (for C++ support)

### Add to Your Project

```bash
# Clone the repository
git clone https://github.com/johnbrodowski/CodeCompilers.git

# Add project reference
dotnet add reference path/to/CodeCompilers/CodeCompilers.csproj
```

## 📚 Usage Examples

### Python

```csharp
using CodeCompilers.Python;

// Create Python manager
var pythonManager = new PythonManager("3.11", "request-001");

// Subscribe to events
pythonManager.PyOutPutMessage += (sender, e) => Console.WriteLine($"Output: {e.Message}");
pythonManager.PyErrorOccurred += (sender, e) => Console.WriteLine($"Error: {e.ErrorMessage}");

// Configure Python settings
var settings = new PythonSettingsObject();
settings.Version = "3.11";
settings.VirtualEnvironmentProjectFolder = @"C:\MyProjects\PythonApp";
settings.VirtualEnvironmentName = "myenv";
settings.Code = @"
print('Hello from Python!')
import numpy as np
print(f'NumPy version: {np.__version__}')
";
settings.PipCommands = "numpy pandas matplotlib";

// Run the code (creates venv, installs packages, executes)
await pythonManager.RunTheCode(settings);
```

### C# Dynamic Compilation (Roslyn)

```csharp
using CodeCompilers.CSharp;

// Create dynamic compiler with dependency cache
var compiler = new CSharpDynamicCompiler(@"C:\Temp\NuGetCache");

// Code with NuGet package comment
string code = @"
// Install-Package Newtonsoft.Json -Version 13.0.3
using System;
using Newtonsoft.Json;

public class Program
{
    public static void Main()
    {
        var obj = new { Name = ""John"", Age = 30 };
        string json = JsonConvert.SerializeObject(obj);
        Console.WriteLine(json);
    }
}
";

// Compile and execute
bool success = await compiler.RunFromCodeAsync(code);
Console.WriteLine($"Execution {(success ? "succeeded" : "failed")}");
```

### C# Project Manager

```csharp
using CodeCompilers.CSharp;

// Create project manager
var projectManager = new CSharpProjectManager(
    "MyConsoleApp",
    @"C:\Projects",
    CSharpProjectType.Console
);

// Subscribe to events
projectManager.OutputReceived += (s, output) => Console.WriteLine(output);
projectManager.ErrorReceived += (s, error) => Console.WriteLine($"Error: {error}");

// Create project
await projectManager.CreateProjectAsync();

// Add NuGet packages
await projectManager.AddNuGetPackageAsync("Newtonsoft.Json", "13.0.3");

// Add code file
string code = @"
using System;
using Newtonsoft.Json;

class Program
{
    static void Main()
    {
        Console.WriteLine(""Hello World!"");
    }
}
";
await projectManager.AddCodeFileAsync("Program.cs", code);

// Build and run
bool buildSuccess = await projectManager.RestoreAndBuildAsync();
if (buildSuccess)
{
    await projectManager.RunProjectAsync();
}
```

### Go

```csharp
using CodeCompilers.Go;

var goManager = new GoProjectManager("github.com/myuser/myapp", @"C:\Projects");

// Subscribe to events
goManager.OutputReceived += (s, msg) => Console.WriteLine(msg);
goManager.ErrorReceived += (s, err) => Console.WriteLine($"Error: {err}");

// Create Go module
await goManager.CreateProjectAsync();

// Add dependencies
await goManager.AddDependencyAsync("github.com/gin-gonic/gin@v1.9.1");

// Add main.go
string goCode = @"
package main

import ""fmt""

func main() {
    fmt.Println(""Hello from Go!"")
}
";
await goManager.AddCodeFileAsync(goCode);

// Build and run
await goManager.BuildAsync();
await goManager.RunAsync();
```

### Rust

```csharp
using CodeCompilers.Rust;

var rustManager = new RustProjectManager("my_rust_app", @"C:\Projects");

// Subscribe to events
rustManager.OutputReceived += (s, msg) => Console.WriteLine(msg);

// Create Cargo project
await rustManager.CreateProjectAsync();

// Add dependencies
await rustManager.AddDependencyAsync("serde", "1.0");
await rustManager.AddDependencyAsync("tokio", "1.0");

// Update main.rs
string rustCode = @"
fn main() {
    println!(""Hello from Rust!"");
}
";
await rustManager.UpdateMainCodeAsync(rustCode);

// Build and run
await rustManager.BuildAsync();
await rustManager.RunAsync();
```

### TypeScript

```csharp
using CodeCompilers.TypeScript;

var tsManager = new TypeScriptProjectManager("MyTypeScriptApp", @"C:\Projects");

// Subscribe to events
tsManager.OutputReceived += (s, msg) => Console.WriteLine(msg);

// Create project (generates package.json and tsconfig.json)
await tsManager.CreateProjectAsync();

// Add NPM packages
await tsManager.AddDependencyAsync("axios", "1.6.0");
await tsManager.AddDependencyAsync("lodash", "4.17.21");

// Add TypeScript code
string tsCode = @"
import axios from 'axios';

async function main() {
    console.log('Hello from TypeScript!');
    const response = await axios.get('https://api.github.com');
    console.log(`Status: ${response.status}`);
}

main();
";
await tsManager.AddCodeFileAsync(tsCode);

// Transpile and run
await tsManager.BuildAsync(); // Runs tsc
await tsManager.RunAsync();   // Runs node
```

### C++

```csharp
using CodeCompilers.Cpp;

// Configure compiler options
var options = new CPlusPlusCompilerOptions
{
    CompilerPath = @"C:\Program Files\LLVM\bin\clang.exe",
    DefaultConsoleFlags = "-std=c++20 -Wall -Wextra"
};

var cppCompiler = new CPlusPlusCompiler(options);

// Subscribe to events
cppCompiler.CompilationCompleted += (s, e) => Console.WriteLine(e.Message);
cppCompiler.ExecutionOutputReceived += (s, e) => Console.WriteLine(e.Output);

// C++ code
string cppCode = @"
#include <iostream>
#include <vector>

int main() {
    std::vector<int> numbers = {1, 2, 3, 4, 5};
    std::cout << ""Hello from C++!"" << std::endl;

    for (int n : numbers) {
        std::cout << n << "" "";
    }
    return 0;
}
";

// Compile and execute
await cppCompiler.CompileAndExecuteAsync(
    code: cppCode,
    outputExecutableName: "myapp.exe",
    customCompilerFlags: "-std=c++20 -O2",
    outputMode: OutputMode.Structured
);
```

## ⚙️ Configuration

### Python Virtual Environments

```csharp
var settings = new PythonSettingsObject();
settings.Version = "3.11";
settings.VirtualEnvironmentProjectFolder = @"C:\MyProject";
settings.VirtualEnvironmentName = "venv";
settings.Code = "print('Hello')";

// Automatic venv creation, package installation, and execution
await pythonManager.RunTheCode(settings);
```

### C++ Compiler Paths

```csharp
// Windows with Visual Studio
var options = new CPlusPlusCompilerOptions
{
    CompilerPath = @"C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\LLVM\x64\bin\clang.exe"
};

// Windows with standalone LLVM
var options = new CPlusPlusCompilerOptions
{
    CompilerPath = @"C:\Program Files\LLVM\bin\clang.exe"
};

// macOS
var options = new CPlusPlusCompilerOptions
{
    CompilerPath = "/usr/bin/clang++"
};
```

## 🔒 Security Warnings

**⚠️ IMPORTANT: This library executes arbitrary code provided to it.**

- Only use with **trusted input**
- **Never** execute user-provided code without sandboxing
- Consider running in isolated containers or VMs
- Implement proper input validation and sanitization
- Set appropriate timeouts to prevent infinite loops
- Monitor resource usage (CPU, memory, disk)

### Recommended Security Practices

1. **Sandboxing**: Run code execution in Docker containers or VMs
2. **Resource Limits**: Use process timeouts and memory limits
3. **Input Validation**: Validate all code inputs before execution
4. **Network Isolation**: Restrict network access for executed code
5. **File System Restrictions**: Limit file system access

## 🏗️ Architecture

### Event-Driven Pattern

All compiler/project managers follow a consistent event-driven pattern:

```csharp
public class LanguageManager
{
    // Output events
    public event EventHandler<string> OutputReceived;
    public event EventHandler<string> ErrorReceived;

    // Async methods
    public async Task<bool> CompileAsync(...);
    public async Task<bool> RunAsync(...);
}
```

### Process Management

- Automatic process lifecycle management
- Built-in timeout handling (default: 2 minutes for Python)
- Proper cleanup on cancellation or errors
- Support for `CancellationToken`

## 🐛 Troubleshooting

### Python Issues

**Virtual environment not found:**
- Ensure Python is installed and in PATH
- Check `VirtualEnvironmentProjectFolder` path is valid
- Verify Python version is installed (3.8-3.13)

**Package installation fails:**
- Check internet connection
- Verify pip is installed: `python -m pip --version`
- Check package names in `PipCommands`

### C# Issues

**NuGet package not found:**
- Check package name and version
- Ensure internet connectivity for NuGet.org
- Clear NuGet cache: `dotnet nuget locals all --clear`

**Compilation fails:**
- Check code syntax
- Verify all using statements are present
- Ensure target framework compatibility

### C++ Issues

**Compiler not found:**
- Verify `CompilerPath` points to valid clang/cl.exe
- Check compiler is installed
- Ensure proper permissions

## 🤝 Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues.

### Help Wanted: Testing Experimental Languages

The **Go, Rust, and TypeScript** implementations are code-complete but haven't been tested yet. If you have these tools installed, we'd love your help:

1. Run the test suite: `dotnet test`
2. Report any failures or issues
3. Submit fixes if you find bugs
4. Add more comprehensive tests

These languages need real-world validation to move from ⚠️ Experimental to ✅ Tested status.

### Areas for Improvement

- Add more language support (Java, PHP, Ruby, etc.)
- Improve error messages and diagnostics
- Add comprehensive unit tests
- Add support for Linux/macOS (especially C++ paths)
- Add Docker integration for sandboxing
- Improve documentation

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [Roslyn](https://github.com/dotnet/roslyn) for C# compilation
- Uses [NuGet](https://www.nuget.org/) for package management
- Inspired by various code execution platforms

## 📞 Support

- **Issues**: https://github.com/johnbrodowski/CodeCompilers/issues
- **Discussions**: https://github.com/johnbrodowski/CodeCompilers/discussions

---

**⚠️ Disclaimer**: This library is provided "as is" without warranty. Use at your own risk, especially when executing untrusted code.
