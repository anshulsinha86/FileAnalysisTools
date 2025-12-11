// PART 2: Enhanced MainWindow.xaml.cs with Complete File Management Features
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using OfficeOpenXml;
using MessageBox = System.Windows.MessageBox;

namespace FileAnalysisTools
{
    public partial class MainWindow : Window
    {
        private string selectedPath;
        private readonly List<FileInfoModel> allFiles = new List<FileInfoModel>();
        private readonly ObservableCollection<FileInfoModel> displayedFiles = new ObservableCollection<FileInfoModel>();
        private CancellationTokenSource cts;
        private string stagingFolderPath;
        private DuplicateResult currentDuplicateResult;

        public MainWindow()
        {
            InitializeComponent();
            FilesDataGrid.ItemsSource = displayedFiles;

            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Add double-click handler for file preview
            FilesDataGrid.MouseDoubleClick += FilesDataGrid_MouseDoubleClick;
        }

        private void FilesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FilesDataGrid.SelectedItem is FileInfoModel selectedFile)
            {
                ShowFilePreview(selectedFile);
            }
        }

        private void ShowFilePreview(FileInfoModel file)
        {
            try
            {
                var previewWindow = new FilePreviewWindow(file)
                {
                    Owner = this
                };

                if (previewWindow.ShowDialog() == true)
                {
                    if (previewWindow.RemoveFile)
                    {
                        file.MarkedForRemoval = true;
                        file.IsSelected = true;
                        RefreshDataGrid();
                        UpdateSelectionSummary();
                    }
                    else if (previewWindow.KeepFile)
                    {
                        file.MarkedForRemoval = false;
                        file.IsSelected = false;
                        RefreshDataGrid();
                        UpdateSelectionSummary();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening preview: {ex.Message}", "Preview Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a folder or drive to analyze";
                dialog.ShowNewFolderButton = false;
                dialog.UseDescriptionForTitle = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    selectedPath = dialog.SelectedPath;
                    stagingFolderPath = Path.Combine(selectedPath, "FileAnalysis_Staging");

                    PathText.Text = $"Selected: {selectedPath}";
                    AnalyzeBtn.IsEnabled = true;
                    ExportBtn.IsEnabled = false;

                    // Reset UI
                    SummaryGrid.Visibility = Visibility.Collapsed;
                    ChartsGrid.Visibility = Visibility.Collapsed;
                    DataGridPanel.Visibility = Visibility.Collapsed;
                    FileOperationsPanel.Visibility = Visibility.Collapsed;

                    // Clear previous data
                    allFiles.Clear();
                    displayedFiles.Clear();
                }
            }
        }

        private async void Analyze_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedPath))
            {
                MessageBox.Show("Please select a folder first.", "No Folder Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Reset
            allFiles.Clear();
            displayedFiles.Clear();
            cts = new CancellationTokenSource();

            // Show progress
            ProgressPanel.Visibility = Visibility.Visible;
            ProgressBar.Value = 0;
            ProgressText.Text = "Initializing scan...";

            // Disable buttons
            SelectFolderBtn.IsEnabled = false;
            AnalyzeBtn.IsEnabled = false;

            var startTime = DateTime.Now;
            int totalDirectories = 0;

            try
            {
                // Phase 1: Scan files
                var scanProgress = new Progress<ScanProgress>(p =>
                {
                    ProgressBar.Value = p.Percentage * 0.5;

                    var elapsed = DateTime.Now - startTime;
                    var estimatedTotal = p.Percentage > 5 ? elapsed.TotalSeconds / (p.Percentage / 100.0) : 0;
                    var remaining = TimeSpan.FromSeconds(Math.Max(0, estimatedTotal - elapsed.TotalSeconds));

                    var currentDir = p.CurrentDirectory.Length > 60
                        ? "..." + p.CurrentDirectory.Substring(p.CurrentDirectory.Length - 57)
                        : p.CurrentDirectory;

                    ProgressText.Text = $"📂 Scanning: {p.FilesFound:N0} files in {p.DirectoriesProcessed:N0}/{p.TotalDirectories:N0} directories\n" +
                                      $"📁 Current: {currentDir}\n" +
                                      $"⏱️ Elapsed: {elapsed:mm\\:ss} | Remaining: {(remaining.TotalSeconds > 0 && p.Percentage > 5 ? remaining.ToString(@"mm\:ss") : "calculating...")}";
                });

                var scanResult = await AccurateAnalyzer.ScanDirectoryAccurateAsync(selectedPath, scanProgress, cts.Token);

                allFiles.AddRange(scanResult.Files);
                totalDirectories = scanResult.TotalDirectories;

                if (allFiles.Count == 0)
                {
                    ProgressPanel.Visibility = Visibility.Collapsed;
                    MessageBox.Show("No files found in the selected folder.", "No Files",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Phase 2: Detect duplicates
                var hashStartTime = DateTime.Now;
                var filesToHash = allFiles.Where(f => f.Size > 0 && f.Size < 100 * 1024 * 1024)
                    .GroupBy(f => f.Size)
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g)
                    .Count();

                ProgressText.Text = $"🔐 Preparing to hash {filesToHash:N0} files (only files with duplicate sizes)...";
                await Task.Delay(500);

                var hashProgress = new Progress<HashProgress>(p =>
                {
                    ProgressBar.Value = 50 + (p.Percentage * 0.5);

                    var elapsed = DateTime.Now - hashStartTime;
                    var filesPerSecond = p.FilesProcessed / Math.Max(elapsed.TotalSeconds, 1);
                    var remainingFiles = p.TotalFiles - p.FilesProcessed;
                    var estimatedRemaining = TimeSpan.FromSeconds(remainingFiles / Math.Max(filesPerSecond, 1));

                    var currentFile = p.CurrentFile.Length > 50
                        ? "..." + p.CurrentFile.Substring(p.CurrentFile.Length - 47)
                        : p.CurrentFile;

                    ProgressText.Text = $"🔐 Hashing: {p.FilesProcessed:N0}/{p.TotalFiles:N0} files ({p.Percentage:F1}%)\n" +
                                      $"📄 Current: {currentFile}\n" +
                                      $"⚡ Speed: {filesPerSecond:F0} files/sec | ⏱️ Remaining: {(estimatedRemaining.TotalSeconds > 0 && estimatedRemaining.TotalSeconds < 3600 ? estimatedRemaining.ToString(@"mm\:ss") : "calculating...")}";
                });

                currentDuplicateResult = await AccurateAnalyzer.DetectDuplicatesAccurateAsync(allFiles, hashProgress, cts.Token);

                // Phase 3: Identify primary files for each duplicate group
                IdentifyPrimaryFiles(currentDuplicateResult);

                ProgressBar.Value = 100;

                var totalTime = DateTime.Now - startTime;
                ProgressText.Text = $"✅ Analysis complete! Total time: {totalTime:mm\\:ss}";

                // Update dashboard
                UpdateDashboardAccurate(totalDirectories, currentDuplicateResult);

                // Show results
                SummaryGrid.Visibility = Visibility.Visible;
                ChartsGrid.Visibility = Visibility.Visible;
                DataGridPanel.Visibility = Visibility.Visible;
                FileOperationsPanel.Visibility = Visibility.Visible;
                ExportBtn.IsEnabled = true;

                // Enable operation buttons
                SelectAllBtn.IsEnabled = true;
                DeselectAllBtn.IsEnabled = true;

                // Hide progress after a moment
                await Task.Delay(1500);
                ProgressPanel.Visibility = Visibility.Collapsed;

                // Show summary
                MessageBox.Show(
                    $"✅ Analysis Complete!\n\n" +
                    $"📁 Total Files: {allFiles.Count:N0}\n" +
                    $"💾 Total Size: {Common.FormatBytes(allFiles.Sum(f => f.Size))}\n" +
                    $"📂 Total Folders: {totalDirectories:N0}\n" +
                    $"🔄 Duplicate Files: {currentDuplicateResult.TotalDuplicateFiles:N0} in {currentDuplicateResult.DuplicateGroupCount:N0} groups\n" +
                    $"💰 Wasted Space: {Common.FormatBytes(currentDuplicateResult.WastedSpace)}\n" +
                    $"📄 Empty Files: {allFiles.Count(f => f.Size == 0):N0}\n\n" +
                    $"⏱️ Analysis Time: {totalTime:mm\\:ss}\n\n" +
                    $"💡 Tips:\n" +
                    $"• Double-click any file to preview\n" +
                    $"• Check boxes to select multiple files\n" +
                    $"• Click 'Primary File' links to see original\n" +
                    $"• Use bulk operations to manage files easily",
                    "Analysis Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Analysis was cancelled.", "Cancelled",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during analysis:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ProgressPanel.Visibility = Visibility.Collapsed;
                SelectFolderBtn.IsEnabled = true;
                AnalyzeBtn.IsEnabled = true;
            }
        }

        /// <summary>
        /// NEW: Identify the primary file (first occurrence) in each duplicate group
        /// </summary>
        private void IdentifyPrimaryFiles(DuplicateResult duplicateResult)
        {
            foreach (var group in duplicateResult.DuplicateGroups.Values)
            {
                if (group.Count > 1)
                {
                    // Sort by creation date (oldest is primary)
                    var sortedGroup = group.OrderBy(f => f.CreatedDate).ToList();
                    var primaryFile = sortedGroup[0];

                    // Mark primary file
                    primaryFile.IsPrimaryFile = true;
                    primaryFile.PrimaryFilePath = "★ PRIMARY FILE";

                    // Set primary file path for all duplicates
                    for (int i = 1; i < sortedGroup.Count; i++)
                    {
                        sortedGroup[i].IsPrimaryFile = false;
                        sortedGroup[i].PrimaryFilePath = primaryFile.FullPath;
                    }
                }
            }
        }

        private void UpdateDashboardAccurate(int totalDirectories, DuplicateResult duplicateResult)
        {
            var stats = AccurateAnalyzer.GetAccurateStatistics(allFiles, totalDirectories);

            // Summary cards
            TotalFilesText.Text = stats.TotalFiles.ToString("N0");
            TotalSizeText.Text = Common.FormatBytes(stats.TotalSize);

            DuplicatesText.Text = duplicateResult.TotalDuplicateFiles.ToString("N0");
            DuplicatesSizeText.Text = $"{Common.FormatBytes(duplicateResult.WastedSpace)} wasted ({duplicateResult.DuplicateGroupCount:N0} groups)";

            EmptyFilesText.Text = stats.EmptyFiles.ToString("N0");

            var largeFiles = allFiles.Where(f => f.Size > 100 * 1024 * 1024).ToList();
            LargeFilesText.Text = largeFiles.Count.ToString("N0");
            LargeFilesSizeText.Text = $"{Common.FormatBytes(largeFiles.Sum(f => f.Size))} (>100MB)";

            // Update charts
            UpdateSizeDistributionChart(stats);
            UpdateExtensionChart(stats);

            // Update data grid
            RefreshDataGrid();
        }

        private void UpdateSizeDistributionChart(FileStatistics stats)
        {
            var labels = stats.SizeDistribution.Keys.ToArray();
            var values = stats.SizeDistribution.Values.ToArray();

            SizeChart.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "File Count",
                    Values = new ChartValues<int>(values),
                    Fill = System.Windows.Media.Brushes.DodgerBlue,
                    DataLabels = true,
                    LabelPoint = point => $"{point.Y:N0}",
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold
                }
            };

            SizeChart.AxisX[0].Labels = labels;
            SizeChart.AxisX[0].Foreground = System.Windows.Media.Brushes.WhiteSmoke;
            SizeChart.AxisY[0].Foreground = System.Windows.Media.Brushes.WhiteSmoke;
        }

        private void UpdateExtensionChart(FileStatistics stats)
        {
            var topExtensions = stats.ExtensionBreakdown
                .OrderByDescending(e => e.Value.Count)
                .Take(8)
                .ToList();

            var seriesCollection = new SeriesCollection();

            foreach (var ext in topExtensions)
            {
                seriesCollection.Add(new PieSeries
                {
                    Title = $"{ext.Key} ({ext.Value.Count:N0})",
                    Values = new ChartValues<int> { ext.Value.Count },
                    DataLabels = true,
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    LabelPoint = point => $"{point.Participation:P0}"
                });
            }

            ExtensionChart.Series = seriesCollection;
        }

        private void RefreshDataGrid()
        {
            var filter = FilterCombo.SelectedIndex;
            var searchText = SearchBox.Text.ToLower().Trim();

            var filtered = allFiles.AsEnumerable();

            // Apply filter
            filtered = filter switch
            {
                1 => filtered.Where(f => f.IsDuplicate),
                2 => filtered.Where(f => f.Size == 0),
                3 => filtered.Where(f => f.Size > 100 * 1024 * 1024),
                4 => filtered.Where(f => f.LastModified >= DateTime.Now.AddDays(-7)),
                _ => filtered
            };

            // Apply search
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(f =>
                    f.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    f.Extension.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    f.DirectoryName.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            var results = filtered.Take(10000).ToList();

            displayedFiles.Clear();
            foreach (var file in results)
            {
                // Set convenience properties
                //file.IsEmpty = file.Size == 0;
                //file.IsLarge = file.Size > 100 * 1024 * 1024;

                displayedFiles.Add(file);
            }

            ResultCountText.Text = displayedFiles.Count < filtered.Count()
                ? $"Showing {displayedFiles.Count:N0} of {filtered.Count():N0} files (limited to 10,000)"
                : $"Showing {displayedFiles.Count:N0} files";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (allFiles.Count > 0)
                RefreshDataGrid();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (allFiles.Count > 0)
                RefreshDataGrid();
        }

        // Card click handlers
        private void ShowAllFiles_Click(object sender, RoutedEventArgs e)
        {
            FilterCombo.SelectedIndex = 0;
            DataGridPanel.Visibility = Visibility.Visible;
            ScrollToDataGrid();
        }

        private void ShowDuplicates_Click(object sender, RoutedEventArgs e)
        {
            FilterCombo.SelectedIndex = 1;
            DataGridPanel.Visibility = Visibility.Visible;
            ScrollToDataGrid();
            ShowDuplicateManager();
        }

        private void ShowEmptyFiles_Click(object sender, RoutedEventArgs e)
        {
            FilterCombo.SelectedIndex = 2;
            DataGridPanel.Visibility = Visibility.Visible;
            ScrollToDataGrid();
        }

        private void ShowLargeFiles_Click(object sender, RoutedEventArgs e)
        {
            FilterCombo.SelectedIndex = 3;
            DataGridPanel.Visibility = Visibility.Visible;
            ScrollToDataGrid();
        }

        private void ScrollToDataGrid()
        {
            DataGridPanel.BringIntoView();
        }

        private void ShowDuplicateManager()
        {
            if (currentDuplicateResult == null || currentDuplicateResult.DuplicateGroupCount == 0)
            {
                MessageBox.Show("No duplicate files found.", "No Duplicates",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var message = $"📊 Duplicate File Summary\n\n" +
                         $"🔄 Total Duplicate Files: {currentDuplicateResult.TotalDuplicateFiles:N0}\n" +
                         $"📦 Duplicate Groups: {currentDuplicateResult.DuplicateGroupCount:N0}\n" +
                         $"💰 Wasted Space: {Common.FormatBytes(currentDuplicateResult.WastedSpace)}\n\n" +
                         $"📌 How to manage duplicates:\n\n" +
                         $"1. Each duplicate group has a PRIMARY FILE (★)\n" +
                         $"   - This is the original/oldest file\n" +
                         $"   - It's marked with 'PRIMARY FILE' in the grid\n\n" +
                         $"2. Other files show a link to the primary file\n" +
                         $"   - Click the link to navigate to primary\n" +
                         $"   - Verify before deleting!\n\n" +
                         $"3. Check boxes to select duplicates\n" +
                         $"   - Use filters to show 'Duplicates Only'\n" +
                         $"   - Select non-primary files to delete\n\n" +
                         $"4. Click 'Delete Selected' to remove\n" +
                         $"   - Files move to staging folder first\n" +
                         $"   - Review before permanent deletion\n\n" +
                         $"⚠️ Safety Tip: Always keep at least one copy!\n" +
                         $"The app prevents deleting ALL copies of a file.";

            MessageBox.Show(message, "Duplicate File Manager - Guide",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// NEW: Handle selection changes in grid
        /// </summary>
        private void FilesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectionSummary();
        }

        /// <summary>
        /// NEW: Update selection summary text
        /// </summary>
        private void UpdateSelectionSummary()
        {
            var selectedFiles = displayedFiles.Where(f => f.IsSelected).ToList();
            var selectedCount = selectedFiles.Count;
            var selectedSize = selectedFiles.Sum(f => f.Size);

            if (selectedCount == 0)
            {
                SelectionSummaryText.Text = "No files selected";
                MoveFilesBtn.IsEnabled = false;
                CopyFilesBtn.IsEnabled = false;
                DeleteSelectedBtn.IsEnabled = false;
            }
            else
            {
                SelectionSummaryText.Text = $"{selectedCount:N0} files selected ({Common.FormatBytes(selectedSize)})";
                MoveFilesBtn.IsEnabled = true;
                CopyFilesBtn.IsEnabled = true;
                DeleteSelectedBtn.IsEnabled = true;
            }
        }

        /// <summary>
        /// NEW: Select all visible files
        /// </summary>
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var file in displayedFiles)
            {
                file.IsSelected = true;
            }
            FilesDataGrid.Items.Refresh();
            UpdateSelectionSummary();
        }

        /// <summary>
        /// NEW: Deselect all files
        /// </summary>
        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var file in displayedFiles)
            {
                file.IsSelected = false;
            }
            FilesDataGrid.Items.Refresh();
            UpdateSelectionSummary();
        }

        /// <summary>
        /// NEW: Handle primary file link clicks
        /// </summary>
        private void PrimaryFileLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink hyperlink && hyperlink.Tag is FileInfoModel file)
            {
                if (file.IsPrimaryFile)
                {
                    MessageBox.Show(
                        $"This is the PRIMARY file in its duplicate group.\n\n" +
                        $"File: {file.Name}\n" +
                        $"Path: {file.FullPath}\n" +
                        $"Size: {file.SizeFormatted}\n" +
                        $"Created: {file.CreatedDate:yyyy-MM-dd HH:mm:ss}\n\n" +
                        $"This is the oldest/original file that should be kept.",
                        "Primary File",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else if (!string.IsNullOrEmpty(file.PrimaryFilePath))
                {
                    var result = MessageBox.Show(
                        $"Primary File Location:\n\n{file.PrimaryFilePath}\n\n" +
                        $"Would you like to:\n" +
                        $"• Open the primary file's location in Explorer?\n" +
                        $"• Highlight it in the grid?",
                        "Navigate to Primary File",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            // Open Explorer and select the primary file
                            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{file.PrimaryFilePath}\"");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error opening Explorer: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// NEW: Move selected files to another location
        /// </summary>
        private async void MoveFiles_Click(object sender, RoutedEventArgs e)
        {
            var selectedFiles = allFiles.Where(f => f.IsSelected).ToList();
            if (selectedFiles.Count == 0)
            {
                MessageBox.Show("No files selected.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select destination folder";
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    await MoveFilesAsync(selectedFiles, dialog.SelectedPath);
                }
            }
        }

        /// <summary>
        /// NEW: Copy selected files to another location
        /// </summary>
        private async void CopyFiles_Click(object sender, RoutedEventArgs e)
        {
            var selectedFiles = allFiles.Where(f => f.IsSelected).ToList();
            if (selectedFiles.Count == 0)
            {
                MessageBox.Show("No files selected.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select destination folder";
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    await CopyFilesAsync(selectedFiles, dialog.SelectedPath);
                }
            }
        }

        /// <summary>
        /// ENHANCED: Delete selected files with safety checks
        /// </summary>
        private async void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedFiles = allFiles.Where(f => f.IsSelected).ToList();

            if (selectedFiles.Count == 0)
            {
                MessageBox.Show(
                    "No files selected.\n\n" +
                    "Check the boxes next to files you want to delete.",
                    "No Files Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // SAFETY CHECK: Prevent deleting ALL copies of duplicates
            var safetyWarnings = new List<string>();
            var duplicateGroupsToDelete = selectedFiles
                .Where(f => f.IsDuplicate)
                .GroupBy(f => f.DuplicateGroupId)
                .ToList();

            foreach (var group in duplicateGroupsToDelete)
            {
                var groupId = group.Key;
                var allFilesInGroup = allFiles.Where(f => f.DuplicateGroupId == groupId).ToList();
                var selectedInGroup = group.Count();
                var totalInGroup = allFilesInGroup.Count;

                if (selectedInGroup == totalInGroup)
                {
                    safetyWarnings.Add($"• Group '{groupId.Substring(0, 8)}...': Trying to delete ALL {totalInGroup} copies!");
                }
            }

            if (safetyWarnings.Count > 0)
            {
                var warningMessage = "⚠️ SAFETY WARNING ⚠️\n\n" +
                                   "You're trying to delete ALL copies of these duplicate groups:\n\n" +
                                   string.Join("\n", safetyWarnings) +
                                   "\n\n❌ This will result in data loss!\n\n" +
                                   "You must keep at least one copy of each file.\n\n" +
                                   "Would you like to:\n" +
                                   "• Automatically keep primary files (recommended)\n" +
                                   "• Cancel and review selection";

                var result = MessageBox.Show(warningMessage,
                    "Prevent Data Loss",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Deselect primary files automatically
                    foreach (var file in selectedFiles)
                    {
                        if (file.IsPrimaryFile)
                        {
                            file.IsSelected = false;
                        }
                    }

                    // Update selection
                    selectedFiles = allFiles.Where(f => f.IsSelected).ToList();
                    FilesDataGrid.Items.Refresh();
                    UpdateSelectionSummary();

                    if (selectedFiles.Count == 0)
                    {
                        MessageBox.Show("All selections removed for safety. No files will be deleted.",
                            "Operation Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
                else
                {
                    return; // Cancel operation
                }
            }

            // Show confirmation dialog
            var totalSize = selectedFiles.Sum(f => f.Size);
            var duplicateCount = selectedFiles.Count(f => f.IsDuplicate);
            var primaryCount = selectedFiles.Count(f => f.IsPrimaryFile);

            var confirmMessage = $"Delete {selectedFiles.Count:N0} selected files?\n\n" +
                               $"📊 Summary:\n" +
                               $"• Total files: {selectedFiles.Count:N0}\n" +
                               $"• Total size: {Common.FormatBytes(totalSize)}\n" +
                               $"• Duplicate files: {duplicateCount:N0}\n" +
                               $"• Primary files: {primaryCount:N0}\n\n";

            if (primaryCount > 0)
            {
                confirmMessage += $"⚠️ WARNING: You're deleting {primaryCount} PRIMARY files!\n" +
                                $"These are the original files that other duplicates reference.\n\n";
            }

            confirmMessage += $"Files will be moved to:\n{stagingFolderPath}\n\n" +
                            $"✅ This is SAFE - files are moved, not permanently deleted!\n" +
                            $"You can review and recover them later if needed.\n\n" +
                            $"Continue with deletion?";

            var confirmResult = MessageBox.Show(confirmMessage,
                "Confirm File Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult == MessageBoxResult.Yes)
            {
                await MoveFilesToStagingAsync(selectedFiles);
            }
        }

        private async Task MoveFilesAsync(List<FileInfoModel> files, string destinationPath)
        {
            ProgressPanel.Visibility = Visibility.Visible;
            ProgressBar.Value = 0;
            ProgressText.Text = "Moving files...";

            int moved = 0;
            int failed = 0;

            try
            {
                await Task.Run(() =>
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        var file = files[i];
                        try
                        {
                            if (File.Exists(file.FullPath))
                            {
                                var destFile = Path.Combine(destinationPath, file.Name);

                                // Handle name collisions
                                int counter = 1;
                                while (File.Exists(destFile))
                                {
                                    var nameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                                    var ext = Path.GetExtension(file.Name);
                                    destFile = Path.Combine(destinationPath, $"{nameWithoutExt}_{counter}{ext}");
                                    counter++;
                                }

                                File.Move(file.FullPath, destFile);
                                moved++;

                                // Update file path
                                file.FullPath = destFile;
                                file.DirectoryName = destinationPath;
                                file.IsSelected = false;
                            }
                        }
                        catch
                        {
                            failed++;
                        }

                        var progress = ((i + 1) * 100.0 / files.Count);
                        Dispatcher.Invoke(() =>
                        {
                            ProgressBar.Value = progress;
                            ProgressText.Text = $"Moving files: {i + 1:N0}/{files.Count:N0}\n" +
                                              $"✓ Moved: {moved:N0} | ✗ Failed: {failed:N0}";
                        });
                    }
                });

                ProgressPanel.Visibility = Visibility.Collapsed;
                RefreshDataGrid();
                UpdateSelectionSummary();

                MessageBox.Show(
                    $"Move operation complete!\n\n" +
                    $"✓ Moved: {moved:N0} files\n" +
                    $"✗ Failed: {failed:N0} files",
                    "Move Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ProgressPanel.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Error moving files:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CopyFilesAsync(List<FileInfoModel> files, string destinationPath)
        {
            ProgressPanel.Visibility = Visibility.Visible;
            ProgressBar.Value = 0;
            ProgressText.Text = "Copying files...";

            int copied = 0;
            int failed = 0;

            try
            {
                await Task.Run(() =>
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        var file = files[i];
                        try
                        {
                            if (File.Exists(file.FullPath))
                            {
                                var destFile = Path.Combine(destinationPath, file.Name);

                                // Handle name collisions
                                int counter = 1;
                                while (File.Exists(destFile))
                                {
                                    var nameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                                    var ext = Path.GetExtension(file.Name);
                                    destFile = Path.Combine(destinationPath, $"{nameWithoutExt}_{counter}{ext}");
                                    counter++;
                                }

                                File.Copy(file.FullPath, destFile);
                                copied++;
                            }
                        }
                        catch
                        {
                            failed++;
                        }

                        var progress = ((i + 1) * 100.0 / files.Count);
                        Dispatcher.Invoke(() =>
                        {
                            ProgressBar.Value = progress;
                            ProgressText.Text = $"Copying files: {i + 1:N0}/{files.Count:N0}\n" +
                                              $"✓ Copied: {copied:N0} | ✗ Failed: {failed:N0}";
                        });
                    }
                });

                ProgressPanel.Visibility = Visibility.Collapsed;

                MessageBox.Show(
                    $"Copy operation complete!\n\n" +
                    $"✓ Copied: {copied:N0} files\n" +
                    $"✗ Failed: {failed:N0} files",
                    "Copy Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ProgressPanel.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Error copying files:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task MoveFilesToStagingAsync(List<FileInfoModel> files)
        {
            ProgressPanel.Visibility = Visibility.Visible;
            ProgressBar.Value = 0;
            ProgressText.Text = "Creating staging folder...";

            try
            {
                if (!Directory.Exists(stagingFolderPath))
                {
                    Directory.CreateDirectory(stagingFolderPath);
                }

                int moved = 0;
                int failed = 0;
                long freedSpace = 0;

                await Task.Run(() =>
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        var file = files[i];

                        try
                        {
                            if (File.Exists(file.FullPath))
                            {
                                var relativePath = file.DirectoryName.Replace(selectedPath, "").TrimStart('\\', '/');
                                var stagingSubDir = Path.Combine(stagingFolderPath, relativePath);

                                if (!Directory.Exists(stagingSubDir))
                                {
                                    Directory.CreateDirectory(stagingSubDir);
                                }

                                var destPath = Path.Combine(stagingSubDir, file.Name);

                                int counter = 1;
                                while (File.Exists(destPath))
                                {
                                    var nameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                                    var ext = Path.GetExtension(file.Name);
                                    destPath = Path.Combine(stagingSubDir, $"{nameWithoutExt}_{counter}{ext}");
                                    counter++;
                                }

                                File.Move(file.FullPath, destPath);
                                moved++;
                                freedSpace += file.Size;

                                Dispatcher.Invoke(() => allFiles.Remove(file));
                            }
                        }
                        catch
                        {
                            failed++;
                        }

                        var progress = ((i + 1) * 100.0 / files.Count);
                        Dispatcher.Invoke(() =>
                        {
                            ProgressBar.Value = progress;
                            ProgressText.Text = $"Moving files to staging: {i + 1:N0}/{files.Count:N0}\n" +
                                              $"✓ Moved: {moved:N0} | ✗ Failed: {failed:N0}\n" +
                                              $"💾 Space freed: {Common.FormatBytes(freedSpace)}";
                        });
                    }
                });

                ProgressPanel.Visibility = Visibility.Collapsed;

                UpdateDashboardAccurate(allFiles.Select(f => f.DirectoryName).Distinct().Count(), currentDuplicateResult);
                RefreshDataGrid();
                UpdateSelectionSummary();

                var resultMsg = $"Files moved to staging folder!\n\n" +
                              $"✓ Successfully moved: {moved:N0} files\n" +
                              $"✗ Failed: {failed:N0} files\n" +
                              $"💾 Space freed: {Common.FormatBytes(freedSpace)}\n\n" +
                              $"Files are in: {stagingFolderPath}\n\n" +
                              $"Would you like to open the staging folder?";

                var openFolder = MessageBox.Show(resultMsg, "Move Complete",
                    MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (openFolder == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", stagingFolderPath);
                }
            }
            catch (Exception ex)
            {
                ProgressPanel.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Error moving files:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (allFiles.Count == 0)
            {
                MessageBox.Show("No data to export. Please run an analysis first.",
                    "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv",
                DefaultExt = ".xlsx",
                FileName = $"FileAnalysis_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var extension = Path.GetExtension(dialog.FileName).ToLower();

                    if (extension == ".xlsx")
                    {
                        ExportToExcel(dialog.FileName);
                    }
                    else
                    {
                        ExportToCsv(dialog.FileName);
                    }

                    var result = MessageBox.Show(
                        $"Report exported successfully!\n\nLocation: {dialog.FileName}\n\nOpen now?",
                        "Export Complete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = dialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting:\n\n{ex.Message}",
                        "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportToExcel(string filePath)
        {
            using (var package = new ExcelPackage())
            {
                // Summary sheet
                var summarySheet = package.Workbook.Worksheets.Add("Summary");
                summarySheet.Cells["A1"].Value = "File Analysis Report";
                summarySheet.Cells["A1"].Style.Font.Size = 16;
                summarySheet.Cells["A1"].Style.Font.Bold = true;

                summarySheet.Cells["A3"].Value = "Total Files:";
                summarySheet.Cells["B3"].Value = allFiles.Count;
                summarySheet.Cells["A4"].Value = "Total Size:";
                summarySheet.Cells["B4"].Value = Common.FormatBytes(allFiles.Sum(f => f.Size));
                summarySheet.Cells["A5"].Value = "Duplicate Files:";
                summarySheet.Cells["B5"].Value = currentDuplicateResult?.TotalDuplicateFiles ?? 0;

                // Files sheet
                var filesSheet = package.Workbook.Worksheets.Add("All Files");
                filesSheet.Cells["A1"].Value = "Name";
                filesSheet.Cells["B1"].Value = "Extension";
                filesSheet.Cells["C1"].Value = "Size";
                filesSheet.Cells["D1"].Value = "Path";
                filesSheet.Cells["E1"].Value = "Modified";
                filesSheet.Cells["F1"].Value = "Duplicate";
                filesSheet.Cells["G1"].Value = "Primary File";

                int row = 2;
                foreach (var file in allFiles)
                {
                    filesSheet.Cells[row, 1].Value = file.Name;
                    filesSheet.Cells[row, 2].Value = file.Extension;
                    filesSheet.Cells[row, 3].Value = file.SizeFormatted;
                    filesSheet.Cells[row, 4].Value = file.FullPath;
                    filesSheet.Cells[row, 5].Value = file.LastModifiedFormatted;
                    filesSheet.Cells[row, 6].Value = file.IsDuplicate ? "Yes" : "No";
                    filesSheet.Cells[row, 7].Value = file.PrimaryFilePath ?? "";
                    row++;
                }

                filesSheet.Cells.AutoFitColumns();
                package.SaveAs(new FileInfo(filePath));
            }
        }

        private void ExportToCsv(string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Name,Extension,Size,Path,Modified,Duplicate,Primary File");

                foreach (var file in allFiles)
                {
                    writer.WriteLine($"\"{file.Name}\",\"{file.Extension}\",\"{file.SizeFormatted}\",\"{file.FullPath}\",\"{file.LastModifiedFormatted}\",\"{(file.IsDuplicate ? "Yes" : "No")}\",\"{file.PrimaryFilePath ?? ""}\"");
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                var result = MessageBox.Show(
                    "Analysis is running. Close anyway?",
                    "Confirm Close",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                cts?.Cancel();
            }

            base.OnClosing(e);
        }
    }
}