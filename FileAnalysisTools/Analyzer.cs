using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace FileAnalysisTools
{
    /// <summary>
    /// High-performance file analyzer with parallel processing
    /// </summary>
    public static class Analyzer
    {
        /// <summary>
        /// Scan directory with parallel processing for maximum performance
        /// </summary>
        public static async Task<List<FileInfoModel>> ScanDirectoryParallelAsync(
            string rootPath,
            IProgress<ScanProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var files = new ConcurrentBag<FileInfoModel>();
            var directories = new ConcurrentQueue<string>();
            directories.Enqueue(rootPath);

            int processedDirs = 0;
            int totalDirs = 1;

            await Task.Run(() =>
            {
                while (directories.TryDequeue(out var currentDir))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        // Get subdirectories
                        var subdirs = Directory.GetDirectories(currentDir);
                        totalDirs += subdirs.Length;

                        foreach (var subdir in subdirs)
                            directories.Enqueue(subdir);

                        // Get files in parallel
                        var dirFiles = Directory.GetFiles(currentDir);

                        Parallel.ForEach(dirFiles,
                            new ParallelOptions
                            {
                                MaxDegreeOfParallelism = 4,
                                CancellationToken = cancellationToken
                            },
                            filePath =>
                            {
                                try
                                {
                                    var fileInfo = new FileInfo(filePath);
                                    files.Add(new FileInfoModel
                                    {
                                        Name = fileInfo.Name,
                                        Extension = fileInfo.Extension.ToLower(),
                                        Size = fileInfo.Length,
                                        DirectoryName = fileInfo.DirectoryName ?? string.Empty,
                                        FullPath = fileInfo.FullName,
                                        LastModified = fileInfo.LastWriteTime,
                                        CreatedDate = fileInfo.CreationTime,
                                        IsReadOnly = fileInfo.IsReadOnly,
                                        Attributes = fileInfo.Attributes.ToString()
                                    });
                                }
                                catch
                                {
                                    // Skip inaccessible files
                                }
                            });

                        processedDirs++;

                        // Report progress every 10 directories
                        if (processedDirs % 10 == 0 || processedDirs == totalDirs)
                        {
                            progress?.Report(new ScanProgress
                            {
                                DirectoriesProcessed = processedDirs,
                                TotalDirectories = totalDirs,
                                FilesFound = files.Count,
                                CurrentDirectory = currentDir
                            });
                        }
                    }
                    catch
                    {
                        // Skip inaccessible directories
                    }
                }
            }, cancellationToken);

            return files.ToList();
        }

        /// <summary>
        /// Detect duplicate files using MD5 hashing
        /// </summary>
        public static async Task<Dictionary<string, List<FileInfoModel>>> DetectDuplicatesAsync(
            List<FileInfoModel> files,
            IProgress<HashProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var hashGroups = new ConcurrentDictionary<string, List<FileInfoModel>>();
            int processed = 0;
            int total = files.Count(f => f.Size > 0 && f.Size < 100 * 1024 * 1024);

            await Task.Run(() =>
            {
                Parallel.ForEach(
                    files.Where(f => f.Size > 0 && f.Size < 100 * 1024 * 1024),
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = 4,
                        CancellationToken = cancellationToken
                    },
                    file =>
                    {
                        try
                        {
                            string hash = ComputeFileHash(file.FullPath);

                            if (!string.IsNullOrEmpty(hash))
                            {
                                hashGroups.AddOrUpdate(hash,
                                    new List<FileInfoModel> { file },
                                    (key, list) =>
                                    {
                                        lock (list)
                                        {
                                            list.Add(file);
                                        }
                                        return list;
                                    });
                            }

                            int current = Interlocked.Increment(ref processed);

                            // Report progress every 100 files
                            if (current % 100 == 0 || current == total)
                            {
                                progress?.Report(new HashProgress
                                {
                                    FilesProcessed = current,
                                    TotalFiles = total,
                                    CurrentFile = file.Name
                                });
                            }
                        }
                        catch
                        {
                            // Skip files that can't be hashed
                        }
                    });
            }, cancellationToken);

            return hashGroups
                .Where(g => g.Value.Count > 1)
                .ToDictionary(g => g.Key, g => g.Value);
        }

        /// <summary>
        /// Compute MD5 hash of a file
        /// </summary>
        private static string ComputeFileHash(string filePath)
        {
            try
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filePath);
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "");
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Get file statistics summary
        /// </summary>
        public static FileStatistics GetStatistics(List<FileInfoModel> files)
        {
            var stats = new FileStatistics
            {
                TotalFiles = files.Count,
                TotalSize = files.Sum(f => f.Size),
                TotalDirectories = files.Select(f => f.DirectoryName).Distinct().Count(),
                EmptyFiles = files.Count(f => f.Size == 0),
                LargestFile = files.OrderByDescending(f => f.Size).FirstOrDefault(),
                SmallestFile = files.Where(f => f.Size > 0).OrderBy(f => f.Size).FirstOrDefault(),
                AverageFileSize = files.Any() ? (long)files.Average(f => f.Size) : 0,
                MedianFileSize = CalculateMedian(files.Select(f => f.Size).ToList()),
                OldestFile = files.OrderBy(f => f.LastModified).FirstOrDefault(),
                NewestFile = files.OrderByDescending(f => f.LastModified).FirstOrDefault()
            };

            // Extension breakdown
            stats.ExtensionBreakdown = files
                .GroupBy(f => string.IsNullOrEmpty(f.Extension) ? "No Extension" : f.Extension)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => new ExtensionStats
                {
                    Count = g.Count(),
                    TotalSize = g.Sum(f => f.Size),
                    AverageSize = (long)g.Average(f => f.Size)
                });

            // Size distribution
            stats.SizeDistribution = new Dictionary<string, int>
            {
                ["0-1KB"] = files.Count(f => f.Size < 1024),
                ["1KB-100KB"] = files.Count(f => f.Size >= 1024 && f.Size < 100 * 1024),
                ["100KB-1MB"] = files.Count(f => f.Size >= 100 * 1024 && f.Size < 1024 * 1024),
                ["1MB-10MB"] = files.Count(f => f.Size >= 1024 * 1024 && f.Size < 10 * 1024 * 1024),
                ["10MB-100MB"] = files.Count(f => f.Size >= 10 * 1024 * 1024 && f.Size < 100 * 1024 * 1024),
                ["100MB+"] = files.Count(f => f.Size >= 100 * 1024 * 1024)
            };

            return stats;
        }

        /// <summary>
        /// Calculate median file size
        /// </summary>
        private static long CalculateMedian(List<long> values)
        {
            if (values.Count == 0) return 0;

            values.Sort();
            int mid = values.Count / 2;

            return values.Count % 2 == 0
                ? (values[mid - 1] + values[mid]) / 2
                : values[mid];
        }

        /// <summary>
        /// Find files by pattern
        /// </summary>
        public static List<FileInfoModel> FindFiles(
            List<FileInfoModel> files,
            string pattern,
            SearchOption searchOption = SearchOption.Name)
        {
            pattern = pattern.ToLower();

            return searchOption switch
            {
                SearchOption.Name => files.Where(f => f.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList(),
                SearchOption.Extension => files.Where(f => f.Extension.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList(),
                SearchOption.Path => files.Where(f => f.FullPath.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => files
            };
        }

        /// <summary>
        /// Filter files by size range
        /// </summary>
        public static List<FileInfoModel> FilterBySize(
            List<FileInfoModel> files,
            long minSize,
            long maxSize)
        {
            return files.Where(f => f.Size >= minSize && f.Size <= maxSize).ToList();
        }

        /// <summary>
        /// Filter files by date range
        /// </summary>
        public static List<FileInfoModel> FilterByDate(
            List<FileInfoModel> files,
            DateTime startDate,
            DateTime endDate)
        {
            return files.Where(f => f.LastModified >= startDate && f.LastModified <= endDate).ToList();
        }
    }

    /// <summary>
    /// File information model
    /// </summary>
    public class FileInfoModel
    {
        public string Name { get; set; }
        public string Extension { get; set; }
        public long Size { get; set; }
        public string DirectoryName { get; set; }
        public string FullPath { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsReadOnly { get; set; }
        public string Attributes { get; set; }
        public string Hash { get; set; }
        public bool IsDuplicate { get; set; }
        public bool IsSelected { get; set; }

        public string SizeFormatted => Common.FormatBytes(Size);
        public string LastModifiedFormatted => LastModified.ToString("yyyy-MM-dd HH:mm:ss");

        public FileInfoModel()
        {
            Name = string.Empty;
            Extension = string.Empty;
            DirectoryName = string.Empty;
            FullPath = string.Empty;
            Attributes = string.Empty;
            Hash = string.Empty;
        }
    }

    /// <summary>
    /// Scan progress information
    /// </summary>
    public class ScanProgress
    {
        public int DirectoriesProcessed { get; set; }
        public int TotalDirectories { get; set; }
        public int FilesFound { get; set; }
        public string CurrentDirectory { get; set; } = string.Empty;
        public double Percentage => TotalDirectories > 0
            ? (DirectoriesProcessed * 100.0) / TotalDirectories
            : 0;
    }

    /// <summary>
    /// Hash computation progress
    /// </summary>
    public class HashProgress
    {
        public int FilesProcessed { get; set; }
        public int TotalFiles { get; set; }
        public string CurrentFile { get; set; } = string.Empty;
        public double Percentage => TotalFiles > 0
            ? (FilesProcessed * 100.0) / TotalFiles
            : 0;
    }

    /// <summary>
    /// File statistics summary
    /// </summary>
    public class FileStatistics
    {
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public int TotalDirectories { get; set; }
        public int EmptyFiles { get; set; }
        public FileInfoModel? LargestFile { get; set; }
        public FileInfoModel? SmallestFile { get; set; }
        public long AverageFileSize { get; set; }
        public long MedianFileSize { get; set; }
        public FileInfoModel? OldestFile { get; set; }
        public FileInfoModel? NewestFile { get; set; }
        public Dictionary<string, ExtensionStats> ExtensionBreakdown { get; set; } = new();
        public Dictionary<string, int> SizeDistribution { get; set; } = new();
    }

    /// <summary>
    /// Extension statistics
    /// </summary>
    public class ExtensionStats
    {
        public int Count { get; set; }
        public long TotalSize { get; set; }
        public long AverageSize { get; set; }
    }

    /// <summary>
    /// Search options for file finding
    /// </summary>
    public enum SearchOption
    {
        Name,
        Extension,
        Path
    }
}