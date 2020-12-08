using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.Configuration;
using System.Runtime.InteropServices.ComTypes;

namespace FileAnalysisTools
{
    public static class FILETIMEExtensions
    {
        public static DateTime ToDateTime(this System.Runtime.InteropServices.ComTypes.FILETIME time)
        {
            ulong high = (ulong)time.dwHighDateTime;
            ulong low = (ulong)time.dwLowDateTime;
            long fileTime = (long)((high << 32) + low);
            return DateTime.FromFileTimeUtc(fileTime);
        }
    }
    public static class TheFasterWay
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr FindFirstFileW(string lpFileName, out WIN32_FIND_DATAW lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATAW lpFindFileData);

        [DllImport("kernel32.dll")]
        public static extern bool FindClose(IntPtr hFindFile);

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern int SHMultiFileProperties(IDataObject pdtobj, int flags);

        //[DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        //public static extern int SHGetSpecialFolderPath(IntPtr hwndOwner, IntPtr lpszPath, int nFolder, int fCreate);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]

        public struct WIN32_FIND_DATAW
        {
            public FileAttributes dwFileAttributes;
            internal System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public int nFileSizeHigh;
            public int nFileSizeLow;
            public int dwReserved0;
            public int dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        public static bool FindNextFilePInvokeRecursive(string path, out List<FileInformation> files, out List<DirectoryInformation> directories)
        {

            List<FileInformation> fileList = new List<FileInformation>();
            List<DirectoryInformation> directoryList = new List<DirectoryInformation>();
            WIN32_FIND_DATAW findData;
            IntPtr findHandle = INVALID_HANDLE_VALUE;
            try
            {
                findHandle = FindFirstFileW(path + @"\*", out findData);
                if (findHandle != INVALID_HANDLE_VALUE)
                {
                    do
                    {
                        // Skip current directory and parent directory symbols that are returned.
                        if (findData.cFileName != "." && findData.cFileName != "..")
                        {
                            string fullPath = path + @"\" + findData.cFileName;

                            // Check if this is a directory and not a symbolic link since symbolic links could lead to repeated files and folders as well as infinite loops.
                            if (findData.dwFileAttributes.HasFlag(FileAttributes.Directory) && !findData.dwFileAttributes.HasFlag(FileAttributes.ReparsePoint))
                            {
                                directoryList.Add(new DirectoryInformation { CreationTime = findData.ftCreationTime.ToDateTime(), LastAccessTime = findData.ftLastAccessTime.ToDateTime(), LastWriteTime = findData.ftLastWriteTime.ToDateTime(), Length = findData.nFileSizeLow, FullPath = fullPath });
                                List<FileInformation> subDirectoryFileList = new List<FileInformation>();
                                List<DirectoryInformation> subDirectoryDirectoryList = new List<DirectoryInformation>();
                                if (FindNextFilePInvokeRecursive(fullPath, out subDirectoryFileList, out subDirectoryDirectoryList))
                                {
                                    fileList.AddRange(subDirectoryFileList);
                                    directoryList.AddRange(subDirectoryDirectoryList);
                                }
                            }
                            else if (!findData.dwFileAttributes.HasFlag(FileAttributes.Directory))
                            {
                                fileList.Add(new FileInformation { Name = findData.cFileName, CreationTime = findData.ftCreationTime.ToDateTime(), LastAccessTime = findData.ftLastAccessTime.ToDateTime(), LastWriteTime = findData.ftLastWriteTime.ToDateTime(), Length = findData.nFileSizeLow, FullPath = fullPath });
                            }
                        }
                    }
                    while (FindNextFile(findHandle, out findData));
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Caught exception while trying to enumerate a directory. {0}", exception.ToString());
                if (findHandle != INVALID_HANDLE_VALUE) FindClose(findHandle);
                files = null;
                directories = null;
                return false;
            }
            if (findHandle != INVALID_HANDLE_VALUE) FindClose(findHandle);
            files = fileList;
            directories = directoryList;
            return true;
        }

        public static bool FindNextFilePInvokeRecursiveParalleled(string path, out List<FileInformation> files, out List<DirectoryInformation> directories)
        {
            List<FileInformation> fileList = new List<FileInformation>();
            object fileListLock = new object();
            List<DirectoryInformation> directoryList = new List<DirectoryInformation>();
            object directoryListLock = new object();
            WIN32_FIND_DATAW findData;
            IntPtr findHandle = INVALID_HANDLE_VALUE;

            try
            {
                path = path.EndsWith(@"\") ? path : path + @"\";
                findHandle = FindFirstFileW(path + @"*", out findData);
                if (findHandle != INVALID_HANDLE_VALUE)
                {
                    do
                    {
                        // Skip current directory and parent directory symbols that are returned.
                        if (findData.cFileName != "." && findData.cFileName != "..")
                        {
                            string fullPath = path + findData.cFileName;

                            // Check if this is a directory and not a symbolic link since symbolic links could lead to repeated files and folders as well as infinite loops.
                            if (findData.dwFileAttributes.HasFlag(FileAttributes.Directory) && !findData.dwFileAttributes.HasFlag(FileAttributes.ReparsePoint))
                            {
                                directoryList.Add(new DirectoryInformation { CreationTime = findData.ftCreationTime.ToDateTime(), LastAccessTime = findData.ftLastAccessTime.ToDateTime(), LastWriteTime = findData.ftLastWriteTime.ToDateTime(), Length = findData.nFileSizeLow, FullPath = fullPath });
                            }
                            else if (!findData.dwFileAttributes.HasFlag(FileAttributes.Directory))
                            {
                                fileList.Add(new FileInformation { Name = findData.cFileName, CreationTime = findData.ftCreationTime.ToDateTime(), LastAccessTime = findData.ftLastAccessTime.ToDateTime(), LastWriteTime = findData.ftLastWriteTime.ToDateTime(), Length = findData.nFileSizeLow, FullPath = fullPath });
                            }
                        }
                    }
                    while (FindNextFile(findHandle, out findData));
                    //directoryList.AsParallel().ForAll(x =>
                    Parallel.ForEach(directoryList, x =>
                    {
                        List<FileInformation> subDirectoryFileList = new List<FileInformation>();
                        List<DirectoryInformation> subDirectoryDirectoryList = new List<DirectoryInformation>();
                        if (FindNextFilePInvokeRecursive(x.FullPath, out subDirectoryFileList, out subDirectoryDirectoryList))
                        {
                            lock (fileListLock)
                            {
                                fileList.AddRange(subDirectoryFileList);
                            }
                            lock (directoryListLock)
                            {
                                directoryList.AddRange(subDirectoryDirectoryList);
                            }
                        }
                    });
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Caught exception while trying to enumerate a directory. {0}", exception.ToString());
                if (findHandle != INVALID_HANDLE_VALUE) FindClose(findHandle);
                files = null;
                directories = null;
                return false;
            }
            if (findHandle != INVALID_HANDLE_VALUE) FindClose(findHandle);
            files = fileList;
            directories = directoryList;
            return true;
        }
        public class FileInformation
        {
            public string Name;
            public DateTime CreationTime;
            public DateTime LastAccessTime;
            public DateTime LastWriteTime;
            public int Length;
            public string FullPath;
            //public string Owner;
        }
        public class DirectoryInformation
        {
            public DateTime CreationTime;
            public DateTime LastAccessTime;
            public DateTime LastWriteTime;
            public int Length;
            public string FullPath;
        }
    }
}
