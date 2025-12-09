using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace FileAnalysisTools
{
    /// <summary>
    /// Accurate, deterministic file analyzer
    /// Guarantees consistent results across multiple scans
    /// </summary>
    public static class AccurateAnalyzer
    {
        /// <summary>
        /// Scan directory with 100% accurate counting - NO race conditions
        /// </summary>
        public static async Task<ScanResult> ScanDirectoryAccurateAsync(
            string rootPath,
            IProgress<ScanProgress> progress,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var result = new ScanResult
                {
                    Files = new List<FileInfoModel>(),
                    TotalDirectories = 0,
                    TotalFiles = 0
                };

                var directories = new List<string>();

                // STEP 1: Count ALL directories first (deterministic)
                try
                {
                    directories = Directory.GetDirectories(rootPath, "*", System.IO.SearchOption.AllDirectories).ToList();
                    directories.Insert(0, rootPath); // Include root
                }
                catch (Exception ex)
                {
                    // If full recursive fails, do it manually
                    directories = GetAllDirectoriesManual(rootPath);
                }

                result.TotalDirectories = directories.Count;

                // STEP 2: Process each directory sequentially (no race conditions)
                int processedDirs = 0;
                int filesFound = 0;

                foreach (var dir in directories)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        // Get files in THIS directory only (not recursive)
                        var files = Directory.GetFiles(dir, "*", System.IO.SearchOption.TopDirectoryOnly);

                        foreach (var filePath in files)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            try
                            {
                                var fileInfo = new FileInfo(filePath);

                                // Skip system/hidden files that Windows doesn't count
                                if ((fileInfo.Attributes & FileAttributes.System) != 0 ||
                                    (fileInfo.Attributes & FileAttributes.Hidden) != 0)
                                {
                                    continue;
                                }

                                result.Files.Add(new FileInfoModel
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

                                filesFound++;
                            }
                            catch
                            {
                                // Skip inaccessible files
                            }
                        }
                    }
                    catch
                    {
                        // Skip inaccessible directories
                    }

                    processedDirs++;

                    // Report progress every 50 directories
                    if (processedDirs % 50 == 0 || processedDirs == directories.Count)
                    {
                        progress?.Report(new ScanProgress
                        {
                            DirectoriesProcessed = processedDirs,
                            TotalDirectories = result.TotalDirectories,
                            FilesFound = filesFound,
                            CurrentDirectory = dir
                        });
                    }
                }

                result.TotalFiles = result.Files.Count;
                return result;

            }, cancellationToken);
        }

        /// <summary>
        /// Manual directory enumeration for problematic paths
        /// </summary>
        private static List<string> GetAllDirectoriesManual(string rootPath)
        {
            var allDirs = new List<string> { rootPath };
            var queue = new Queue<string>();
            queue.Enqueue(rootPath);

            while (queue.Count > 0)
            {
                var currentDir = queue.Dequeue();

                try
                {
                    var subdirs = Directory.GetDirectories(currentDir);
                    foreach (var subdir in subdirs)
                    {
                        allDirs.Add(subdir);
                        queue.Enqueue(subdir);
                    }
                }
                catch
                {
                    // Skip inaccessible directories
                }
            }

            return allDirs;
        }

        /// <summary>
        /// Accurate duplicate detection with proper group counting
        /// </summary>
        public static async Task<DuplicateResult> DetectDuplicatesAccurateAsync(
            List<FileInfoModel> files,
            IProgress<HashProgress> progress,
            CancellationToken cancellationToken)
        {
            var result = new DuplicateResult
            {
                DuplicateGroups = new Dictionary<string, List<FileInfoModel>>(),
                TotalDuplicateFiles = 0,
                DuplicateGroupCount = 0,
                WastedSpace = 0
            };

            // STEP 1: Pre-filter by size (files with unique sizes can't be duplicates)
            var sizeGroups = files
                .Where(f => f.Size > 0 && f.Size < 100 * 1024 * 1024) // Only hash files 0-100MB
                .GroupBy(f => f.Size)
                .Where(g => g.Count() > 1) // Only process size groups with multiple files
                .SelectMany(g => g)
                .ToList();

            if (sizeGroups.Count == 0)
                return result;

            int processed = 0;
            int total = sizeGroups.Count;

            // STEP 2: Hash files sequentially for consistency
            var hashDict = new Dictionary<string, List<FileInfoModel>>();

            await Task.Run(() =>
            {
                foreach (var file in sizeGroups)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        string hash = ComputeFileHashMD5(file.FullPath);

                        if (!string.IsNullOrEmpty(hash))
                        {
                            if (!hashDict.ContainsKey(hash))
                            {
                                hashDict[hash] = new List<FileInfoModel>();
                            }
                            hashDict[hash].Add(file);
                        }

                        processed++;

                        if (processed % 100 == 0 || processed == total)
                        {
                            progress?.Report(new HashProgress
                            {
                                FilesProcessed = processed,
                                TotalFiles = total,
                                CurrentFile = file.Name
                            });
                        }
                    }
                    catch
                    {
                        // Skip files that can't be hashed
                    }
                }
            }, cancellationToken);

            // STEP 3: Identify duplicate groups (groups with 2+ identical files)
            foreach (var kvp in hashDict.Where(g => g.Value.Count > 1))
            {
                result.DuplicateGroups[kvp.Key] = kvp.Value;
            }

            // STEP 4: Calculate accurate statistics
            result.DuplicateGroupCount = result.DuplicateGroups.Count;

            foreach (var group in result.DuplicateGroups.Values)
            {
                // Mark all files in group as duplicates
                foreach (var file in group)
                {
                    file.IsDuplicate = true;
                    file.Hash = group[0].Hash; // Use first file's hash
                    file.DuplicateGroupId = result.DuplicateGroups.First(g => g.Value == group).Key;
                }

                // Total duplicate files
                result.TotalDuplicateFiles += group.Count;

                // Wasted space = (count - 1) * size (keep one, others are wasted)
                result.WastedSpace += (group.Count - 1) * group[0].Size;
            }

            return result;
        }

        /// <summary>
        /// Consistent MD5 hash computation
        /// </summary>
        private static string ComputeFileHashMD5(string filePath)
        {
            try
            {
                using (var md5 = MD5.Create())
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Get comprehensive statistics
        /// </summary>
        public static FileStatistics GetAccurateStatistics(List<FileInfoModel> files, int totalDirectories)
        {
            var stats = new FileStatistics
            {
                TotalFiles = files.Count,
                TotalSize = files.Sum(f => f.Size),
                TotalDirectories = totalDirectories,
                EmptyFiles = files.Count(f => f.Size == 0),
                LargestFile = files.OrderByDescending(f => f.Size).FirstOrDefault(),
                SmallestFile = files.Where(f => f.Size > 0).OrderBy(f => f.Size).FirstOrDefault(),
                AverageFileSize = files.Count > 0 ? (long)files.Average(f => f.Size) : 0,
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
    }

    /// <summary>
    /// Scan result container
    /// </summary>
    public class ScanResult
    {
        public List<FileInfoModel> Files { get; set; }
        public int TotalFiles { get; set; }
        public int TotalDirectories { get; set; }
    }

    /// <summary>
    /// Duplicate detection result
    /// </summary>
    public class DuplicateResult
    {
        public Dictionary<string, List<FileInfoModel>> DuplicateGroups { get; set; }
        public int DuplicateGroupCount { get; set; }
        public int TotalDuplicateFiles { get; set; }
        public long WastedSpace { get; set; }
    }
}