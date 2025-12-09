#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileAnalysisTools
{
    /// <summary>
    /// Common utility methods for file analysis
    /// Updated for .NET 8 with native long path support
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Gets all files in a directory recursively with error handling
        /// </summary>
        public static List<FileInfo> GetAllFiles(string path)
        {
            var files = new List<FileInfo>();

            try
            {
                var dir = new DirectoryInfo(path);

                // Use EnumerateFiles for better performance
                var enumOptions = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true, // Skip access denied errors
                    AttributesToSkip = FileAttributes.System,
                    ReturnSpecialDirectories = false
                };

                files.AddRange(dir.EnumerateFiles("*.*", enumOptions));
            }
            catch (Exception ex)
            {
                // Log error if needed
                Console.WriteLine($"Error accessing {path}: {ex.Message}");
            }

            return files;
        }

        /// <summary>
        /// Gets all files with progress callback - better for large directories
        /// </summary>
        public static IEnumerable<FileInfo> GetAllFilesWithProgress(
            string path,
            Action<int>? progressCallback = null)
        {
            int count = 0;
            var enumOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.System
            };

            var dir = new DirectoryInfo(path);

            foreach (var file in dir.EnumerateFiles("*.*", enumOptions))
            {
                count++;
                if (count % 100 == 0)
                {
                    progressCallback?.Invoke(count);
                }
                yield return file;
            }
        }

        /// <summary>
        /// Gets all directories in a path recursively
        /// </summary>
        public static List<DirectoryInfo> GetAllDirectories(string path)
        {
            var directories = new List<DirectoryInfo>();

            try
            {
                var dir = new DirectoryInfo(path);

                var enumOptions = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.System
                };

                directories.AddRange(dir.EnumerateDirectories("*", enumOptions));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing directories in {path}: {ex.Message}");
            }

            return directories;
        }

        /// <summary>
        /// Format bytes to human-readable string
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Check if a file is accessible
        /// </summary>
        public static bool IsFileAccessible(string filePath)
        {
            try
            {
                using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get file extension without dot
        /// </summary>
        public static string GetExtensionWithoutDot(string fileName)
        {
            var ext = Path.GetExtension(fileName);
            return string.IsNullOrEmpty(ext) ? "No Extension" : ext.TrimStart('.').ToLower();
        }

        /// <summary>
        /// Check if path is valid
        /// </summary>
        public static bool IsValidPath(string path)
        {
            try
            {
                return Directory.Exists(path) || File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get safe file name (removes invalid characters)
        /// </summary>
        public static string GetSafeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Calculate directory size recursively
        /// </summary>
        public static long GetDirectorySize(string path)
        {
            long size = 0;

            try
            {
                var dir = new DirectoryInfo(path);
                var enumOptions = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true
                };

                size = dir.EnumerateFiles("*", enumOptions).Sum(file => file.Length);
            }
            catch
            {
                // Return 0 if error
            }

            return size;
        }

        /// <summary>
        /// Group files by extension
        /// </summary>
        public static Dictionary<string, List<FileInfo>> GroupByExtension(List<FileInfo> files)
        {
            return files
                .GroupBy(f => GetExtensionWithoutDot(f.Name))
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Get file age in days
        /// </summary>
        public static int GetFileAgeInDays(FileInfo file)
        {
            return (DateTime.Now - file.LastWriteTime).Days;
        }

        /// <summary>
        /// Check if file is empty
        /// </summary>
        public static bool IsFileEmpty(FileInfo file)
        {
            return file.Length == 0;
        }

        /// <summary>
        /// Get largest files from a list
        /// </summary>
        public static List<FileInfo> GetLargestFiles(List<FileInfo> files, int count = 10)
        {
            return files.OrderByDescending(f => f.Length).Take(count).ToList();
        }

        /// <summary>
        /// Get oldest files from a list
        /// </summary>
        public static List<FileInfo> GetOldestFiles(List<FileInfo> files, int count = 10)
        {
            return files.OrderBy(f => f.LastWriteTime).Take(count).ToList();
        }

        /// <summary>
        /// Get newest files from a list
        /// </summary>
        public static List<FileInfo> GetNewestFiles(List<FileInfo> files, int count = 10)
        {
            return files.OrderByDescending(f => f.LastWriteTime).Take(count).ToList();
        }
    }
}