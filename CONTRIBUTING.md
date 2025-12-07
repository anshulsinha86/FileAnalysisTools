# Contributing to File Analysis Dashboard

Thank you for your interest in contributing! We welcome contributions from everyone.

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How Can I Contribute?](#how-can-i-contribute)
- [Development Setup](#development-setup)
- [Pull Request Process](#pull-request-process)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Community](#community)

---

## 📜 Code of Conduct

This project and everyone participating in it is governed by our Code of Conduct. By participating, you are expected to uphold this code.

### Our Standards

**Positive behaviors:**
- Using welcoming and inclusive language
- Being respectful of differing viewpoints and experiences
- Gracefully accepting constructive criticism
- Focusing on what is best for the community
- Showing empathy towards other community members

**Unacceptable behaviors:**
- Trolling, insulting/derogatory comments, and personal or political attacks
- Public or private harassment
- Publishing others' private information without explicit permission
- Other conduct which could reasonably be considered inappropriate

---

## 🤝 How Can I Contribute?

### 🐛 Reporting Bugs

Before submitting a bug report:
1. Check the [existing issues](https://github.com/anshulsinha86/FileAnalysisTools/issues)
2. Try to reproduce the issue with the latest version
3. Gather relevant information (OS version, .NET version, error messages, logs)

When reporting bugs, include:
- **Clear title** describing the issue
- **Steps to reproduce** the problem
- **Expected behavior** vs actual behavior
- **Screenshots** if applicable
- **Environment details** (Windows version, RAM, etc.)
- **Error messages** or stack traces

Use the [Bug Report Template](.github/ISSUE_TEMPLATE/bug_report.md).

### 💡 Suggesting Features

Before suggesting a feature:
1. Check if it's already been suggested
2. Consider if it fits the project's scope
3. Think about how it benefits most users

When suggesting features, include:
- **Clear title** describing the feature
- **Use case** - why is this feature needed?
- **Proposed solution** - how should it work?
- **Alternatives considered** - other ways to solve the problem
- **Mockups or examples** if applicable

Use the [Feature Request Template](.github/ISSUE_TEMPLATE/feature_request.md).

### 📝 Improving Documentation

Documentation improvements are always welcome! This includes:
- Fixing typos or grammatical errors
- Adding more examples
- Improving clarity of explanations
- Adding missing documentation
- Translating documentation

### 💻 Contributing Code

We welcome code contributions! See the sections below for detailed guidelines.

---

## 🛠️ Development Setup

### Prerequisites

- Windows 10 version 1607+ or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) or [VS Code](https://code.visualstudio.com/)
- Git

### Setting Up Your Environment

1. **Fork the repository**
   - Click the "Fork" button on GitHub
   - This creates your own copy of the repository

2. **Clone your fork**
   ```bash
   git clone https://github.com/YOUR_USERNAME/FileAnalysisTools.git
   cd FileAnalysisTools
   ```

3. **Add upstream remote**
   ```bash
   git remote add upstream https://github.com/anshulsinha86/FileAnalysisTools.git
   ```

4. **Install dependencies**
   ```bash
   dotnet restore
   ```

5. **Build the project**
   ```bash
   dotnet build
   ```

6. **Run the application**
   ```bash
   dotnet run --project FileAnalysisTools
   ```

### Keeping Your Fork Updated

```bash
# Fetch latest changes from upstream
git fetch upstream

# Merge upstream changes to your main branch
git checkout main
git merge upstream/main

# Push to your fork
git push origin main
```

---

## 🔄 Pull Request Process

### Before Submitting

1. **Create a feature branch**
   ```bash
   git checkout -b feature/my-awesome-feature
   ```

2. **Make your changes**
   - Follow coding standards (see below)
   - Add comments for complex logic
   - Update documentation if needed

3. **Test your changes**
   - Build and run the application
   - Test affected features thoroughly
   - Ensure no regressions

4. **Commit your changes**
   ```bash
   git add .
   git commit -m "Add feature: brief description"
   ```
   
   Use clear, descriptive commit messages:
   - `Add: new feature description`
   - `Fix: bug description`
   - `Update: what was updated`
   - `Refactor: what was refactored`
   - `Docs: documentation changes`

5. **Push to your fork**
   ```bash
   git push origin feature/my-awesome-feature
   ```

### Submitting the Pull Request

1. Go to your fork on GitHub
2. Click "New Pull Request"
3. Select your feature branch
4. Fill out the PR template with:
   - **Description** of changes
   - **Related issues** (if any)
   - **Testing performed**
   - **Screenshots** (for UI changes)
   - **Breaking changes** (if any)

### PR Review Process

1. **Automated checks** will run (build, lint, etc.)
2. **Maintainers** will review your code
3. **Feedback** may be provided - please address it
4. Once approved, your PR will be **merged**!

### PR Best Practices

- Keep PRs focused on a single feature/fix
- Keep PRs reasonably sized (< 500 lines when possible)
- Reference related issues in the PR description
- Respond to review feedback promptly
- Be patient - reviews may take a few days

---

## 📏 Coding Standards

### C# Style Guide

Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

**Key points:**

#### Naming Conventions
```csharp
// Classes, Methods, Properties: PascalCase
public class FileAnalyzer { }
public void AnalyzeFiles() { }
public string FilePath { get; set; }

// Private fields: camelCase with underscore prefix
private string _selectedPath;
private List<FileInfo> _fileList;

// Local variables, parameters: camelCase
int fileCount = 0;
string fileName = "test.txt";

// Constants: PascalCase
private const int MaxThreadCount = 4;

// Interfaces: I prefix + PascalCase
public interface IFileScanner { }
```

#### Code Organization
```csharp
public class MyClass
{
    // 1. Constants
    private const int MaxSize = 100;
    
    // 2. Static fields
    private static readonly string DefaultPath = "C:\\";
    
    // 3. Instance fields
    private readonly List<string> _files;
    private string _currentPath;
    
    // 4. Constructors
    public MyClass() { }
    
    // 5. Properties
    public string Path { get; set; }
    
    // 6. Public methods
    public void Scan() { }
    
    // 7. Private methods
    private void ProcessFile() { }
}
```

#### Comments and Documentation
```csharp
/// <summary>
/// Scans a directory for files using parallel processing.
/// </summary>
/// <param name="path">The root directory path to scan</param>
/// <param name="progress">Optional progress reporter</param>
/// <returns>List of discovered files</returns>
public async Task<List<FileInfo>> ScanAsync(
    string path, 
    IProgress<ScanProgress>? progress = null)
{
    // Complex logic should have inline comments
    // explaining the "why", not the "what"
}
```

#### Modern C# Features
```csharp
// Use nullable reference types
string? optionalValue = null;

// Use pattern matching
if (obj is FileInfo file && file.Length > 0)
{
    // Process file
}

// Use range operators
string hash = fullHash[..16];  // First 16 characters

// Use string interpolation
string message = $"Found {count} files";

// Use collection expressions (.NET 8)
List<string> files = ["file1.txt", "file2.txt"];
```

### XAML Style Guide

```xml
<!-- Use consistent indentation (4 spaces) -->
<Window x:Class="App.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="My Window">
    
    <!-- Group related properties -->
    <Button Content="Click Me"
            Width="100"
            Height="30"
            Margin="10"
            Click="Button_Click" />
    
    <!-- Use resources for repeated values -->
    <Window.Resources>
        <SolidColorBrush x:Key="PrimaryColor" Color="#007ACC"/>
    </Window.Resources>
</Window>
```

---

## 🧪 Testing Guidelines

### Manual Testing

Before submitting a PR, test:

1. **Build succeeds** without errors or warnings
2. **Application launches** without crashes
3. **Core features work**:
   - Select folder
   - Scan files
   - View dashboard
   - Search/filter
   - Export reports
4. **Edge cases**:
   - Empty folders
   - Very large folders (100K+ files)
   - Long file paths
   - Special characters in filenames
   - Access denied folders

### Performance Testing

For performance-related changes:
- Test with folders containing 10K, 100K, and 500K files
- Monitor memory usage
- Check CPU utilization
- Measure execution time

### UI Testing

For UI changes:
- Test at different window sizes
- Test with different DPI settings
- Check dark theme consistency
- Verify accessibility (keyboard navigation, etc.)

---

## 🎯 Areas for Contribution

### 🌟 Good First Issues

Look for issues labeled `good first issue` - these are beginner-friendly:
- Documentation improvements
- UI tweaks
- Minor bug fixes
- Adding comments
- Refactoring simple methods

### 🔥 Help Wanted

Issues labeled `help wanted` need community support:
- Performance optimizations
- New features
- Complex bug fixes
- Test coverage improvements

### 💡 Enhancement Ideas

Consider contributing:
- **CLI interface** for automation
- **Additional export formats** (JSON, XML)
- **File categorization** by type
- **Scheduling** periodic scans
- **Database** for scan history
- **Localization** support

---

## 📬 Community

### Getting Help

- **Questions**: Use [GitHub Discussions](https://github.com/anshulsinha86/FileAnalysisTools/discussions)
- **Bugs**: Open an [issue](https://github.com/anshulsinha86/FileAnalysisTools/issues)
- **Chat**: Join our community (link coming soon)

### Stay Updated

- **Watch** the repository for notifications
- **Star** the project to show support
- **Follow** [@anshulsinha86](https://github.com/anshulsinha86) for updates

---

## 📝 License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

## 🙏 Thank You!

Thank you for considering contributing to File Analysis Dashboard! Every contribution, no matter how small, is valuable and appreciated.

**Happy coding!** 🚀