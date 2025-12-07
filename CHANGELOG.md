# Changelog

All notable changes to File Analysis Dashboard will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- Command-line interface (CLI) mode
- Scheduled scanning
- Database storage for scan history
- File content search

---

## [2.0.0] - 2024-12-08

### 🎉 Major Release - Complete Rewrite

#### Added
- ✨ Modern dark theme UI with professional aesthetics
- 📊 Interactive dashboard with summary cards
- 📈 Real-time charts using LiveCharts (bar chart and pie chart)
- 🔄 Smart duplicate detection using MD5 hashing
- 📄 Empty file identification
- 🔍 Advanced search with real-time filtering
- 💾 Export to Excel (.xlsx) with multiple sheets
- 💾 Export to CSV format
- ⏱️ Detailed progress tracking with time estimates
- 📦 Single-file deployment (no installation required)
- 🎯 Filter options: All Files, Duplicates, Empty Files, Large Files, Recent Files
- ⚡ Real-time speed metrics (files/sec)
- 📊 File size distribution visualization
- 🎯 Top 8 extensions breakdown

#### Changed
- ⚡ **4x faster** - Migrated to parallel processing (4 threads)
- 🔧 Upgraded from .NET Framework 4.8 to **.NET 8** (latest LTS)
- 📦 Replaced Office Interop with **EPPlus** (faster, no Excel required)
- 🗂️ Removed Delimon.Win32.IO dependency (native long path support)
- 🎨 Complete UI redesign with modern WPF styling
- 📊 Improved data grid with better performance (10,000 row limit)
- 🔍 Enhanced search with case-insensitive comparison

#### Performance Improvements
- Parallel directory traversal
- Concurrent hash computation
- Smart hashing (only files < 100MB)
- Memory-efficient data structures (ConcurrentBag)
- Async/await patterns throughout
- Progress reporting optimization

#### Technical Details
- Upgraded to C# 12 with latest language features
- Added nullable reference types
- Implemented modern C# patterns (range operators, pattern matching)
- Added XML documentation comments
- Improved error handling and logging
- Added proper resource disposal

---

## [1.0.0] - 2023-XX-XX

### Initial Release

#### Features
- Basic file scanning functionality
- Simple UI with Windows Forms
- Excel export using Office Interop
- File list display
- Basic statistics
- Delimon.Win32.IO for long path support

---

## Version Comparison

| Feature | v1.0.0 | v2.0.0 |
|---------|--------|--------|
| .NET Version | Framework 4.8 | .NET 8 |
| UI Framework | Windows Forms | WPF |
| Theme | Light | Dark |
| Parallel Processing | No | Yes (4 threads) |
| Duplicate Detection | No | Yes (MD5) |
| Charts | No | Yes (LiveCharts) |
| Progress Details | Basic | Detailed with time estimates |
| Export Formats | Excel only | Excel + CSV |
| Excel Library | Office Interop | EPPlus |
| Long Path Support | Delimon library | Native .NET 8 |
| Deployment | Multi-file | Single .exe |
| File Size | ~5MB + dependencies | ~80MB (all included) |
| Speed (100K files) | ~8 min | ~2 min |

---

## Migration Guide (v1.0 → v2.0)

### Breaking Changes
- Minimum Windows version increased to Windows 10 1607+
- Requires .NET 8 runtime (included in standalone build)
- Project structure changed to SDK-style format

### Data Compatibility
- No data migration needed (application doesn't store persistent data)
- Export file formats remain compatible

### For Developers
- Update to .NET 8 SDK
- Replace old .csproj with SDK-style format
- Remove Delimon.Win32.IO references
- Replace Office Interop with EPPlus
- Update to use async/await patterns

---

## Links

- [GitHub Repository](https://github.com/anshulsinha86/FileAnalysisTools)
- [Latest Release](https://github.com/anshulsinha86/FileAnalysisTools/releases/latest)
- [Report Issues](https://github.com/anshulsinha86/FileAnalysisTools/issues)
- [Documentation](https://github.com/anshulsinha86/FileAnalysisTools/wiki)