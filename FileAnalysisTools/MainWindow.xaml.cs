using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
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
        // Change the declaration of `allFiles` from `readonly` to a regular field.  
        private List<FileInfoModel> allFiles = new List<FileInfoModel>();
        private readonly ObservableCollection<FileInfoModel> displayedFiles = new ObservableCollection<FileInfoModel>();
        private CancellationTokenSource cts;

        public MainWindow()
        {
            InitializeComponent();
            FilesDataGrid.ItemsSource = displayedFiles;

            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
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
                    PathText.Text = $"Selected: {selectedPath}";
                    AnalyzeBtn.IsEnabled = true;
                    ExportBtn.IsEnabled = false;

                    // Reset UI
                    SummaryGrid.Visibility = Visibility.Collapsed;
                    ChartsGrid.Visibility = Visibility.Collapsed;
                    DataGridPanel.Visibility = Visibility.Collapsed;

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

            try
            {
                // Phase 1: Scan files
                var scanProgress = new Progress<ScanProgress>(p =>
                {
                    ProgressBar.Value = p.Percentage * 0.5;

                    var elapsed = DateTime.Now - startTime;
                    var estimatedTotal = elapsed.TotalSeconds / (p.Percentage / 100.0);
                    var remaining = TimeSpan.FromSeconds(estimatedTotal - elapsed.TotalSeconds);

                    var currentDir = p.CurrentDirectory.Length > 60
                        ? "..." + p.CurrentDirectory.Substring(p.CurrentDirectory.Length - 57)
                        : p.CurrentDirectory;

                    ProgressText.Text = $"📂 Scanning: {p.FilesFound:N0} files found in {p.DirectoriesProcessed:N0}/{p.TotalDirectories:N0} directories\n" +
                                      $"📁 Current: {currentDir}\n" +
                                      $"⏱️ Elapsed: {elapsed:mm\\:ss} | Estimated remaining: {(remaining.TotalSeconds > 0 ? remaining.ToString(@"mm\:ss") : "calculating...")}";
                });

                //allFiles.AddRange(await Analyzer.ScanDirectoryParallelAsync(selectedPath, scanProgress, cts.Token));
                allFiles = await FastAnalyzer.ScanDirectoryOptimizedAsync(selectedPath, scanProgress, cts.Token);

                if (allFiles.Count == 0)
                {
                    MessageBox.Show("No files found in the selected folder.", "No Files",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Phase 2: Detect duplicates
                var hashStartTime = DateTime.Now;
                var filesToHash = allFiles.Where(f => f.Size > 0 && f.Size < 100 * 1024 * 1024).Count();

                ProgressText.Text = $"🔐 Preparing to hash {filesToHash:N0} files for duplicate detection...";
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

                //var duplicates = await Analyzer.DetectDuplicatesAsync(allFiles, hashProgress, cts.Token);
                var duplicates = await FastAnalyzer.DetectDuplicatesSmartAsync(allFiles, hashProgress, cts.Token);
                // Mark duplicates
                foreach (var group in duplicates)
                {
                    foreach (var file in group.Value)
                    {
                        file.IsDuplicate = true;
                        file.Hash = group.Key[..16];
                    }
                }

                ProgressBar.Value = 100;

                var totalTime = DateTime.Now - startTime;
                ProgressText.Text = $"✅ Analysis complete! Total time: {totalTime:mm\\:ss}";

                // Update dashboard
                UpdateDashboard();

                // Show results
                SummaryGrid.Visibility = Visibility.Visible;
                ChartsGrid.Visibility = Visibility.Visible;
                DataGridPanel.Visibility = Visibility.Visible;
                ExportBtn.IsEnabled = true;

                // Hide progress after a moment
                await Task.Delay(1500);
                ProgressPanel.Visibility = Visibility.Collapsed;

                // Show summary
                var stats = Analyzer.GetStatistics(allFiles);
                var duplicateCount = allFiles.Count(f => f.IsDuplicate);
                var duplicateSize = allFiles.Where(f => f.IsDuplicate).Sum(f => f.Size);

                MessageBox.Show(
                    $"✅ Analysis Complete!\n\n" +
                    $"📁 Total Files: {stats.TotalFiles:N0}\n" +
                    $"💾 Total Size: {Common.FormatBytes(stats.TotalSize)}\n" +
                    $"📂 Folders: {stats.TotalDirectories:N0}\n" +
                    $"🔄 Duplicates: {duplicateCount:N0} ({Common.FormatBytes(duplicateSize)} wasted)\n" +
                    $"📄 Empty Files: {stats.EmptyFiles:N0}\n" +
                    $"📊 Largest File: {stats.LargestFile?.Name} ({stats.LargestFile?.SizeFormatted})\n\n" +
                    $"⏱️ Analysis Time: {totalTime:mm\\:ss}",
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

        private void UpdateDashboard()
        {
            var stats = Analyzer.GetStatistics(allFiles);

            // Summary cards
            TotalFilesText.Text = stats.TotalFiles.ToString("N0");
            TotalSizeText.Text = Common.FormatBytes(stats.TotalSize);

            var duplicates = allFiles.Where(f => f.IsDuplicate).ToList();
            DuplicatesText.Text = duplicates.Count.ToString("N0");
            DuplicatesSizeText.Text = $"{Common.FormatBytes(duplicates.Sum(f => f.Size))} wasted";

            EmptyFilesText.Text = stats.EmptyFiles.ToString("N0");

            // Large files card
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
                    LabelPoint = point => $"{point.Y:N0}"
                }
            };

            SizeChart.AxisX[0].Labels = labels;
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
                    LabelPoint = point => $"{point.Y:N0} files"
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

            // Update display (limit to 10,000 for performance)
            var results = filtered.Take(10000).ToList();

            displayedFiles.Clear();
            foreach (var file in results)
            {
                displayedFiles.Add(file);
            }

            ResultCountText.Text = displayedFiles.Count < filtered.Count()
                ? $"Showing {displayedFiles.Count:N0} of {filtered.Count():N0} files (limited to 10,000 for performance)"
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
        }

        private void ShowDuplicates_Click(object sender, RoutedEventArgs e)
        {
            FilterCombo.SelectedIndex = 1;
            DataGridPanel.Visibility = Visibility.Visible;
        }

        private void ShowEmptyFiles_Click(object sender, RoutedEventArgs e)
        {
            FilterCombo.SelectedIndex = 2;
            DataGridPanel.Visibility = Visibility.Visible;
        }

        private void ShowLargeFiles_Click(object sender, RoutedEventArgs e)
        {
            FilterCombo.SelectedIndex = 3;
            DataGridPanel.Visibility = Visibility.Visible;
        }

        // EXPORT FUNCTIONALITY - CONTINUED IN NEXT COMMENT
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
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
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
                        $"Report exported successfully!\n\n" +
                        $"Location: {dialog.FileName}\n\n" +
                        $"Would you like to open the file now?",
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
                    MessageBox.Show($"Error exporting report:\n\n{ex.Message}",
                        "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportToExcel(string filePath)
        {
            using (var package = new ExcelPackage())
            {
                var summarySheet = package.Workbook.Worksheets.Add("Summary");
                var stats = Analyzer.GetStatistics(allFiles);

                summarySheet.Cells[1, 1].Value = "File Analysis Report";
                summarySheet.Cells[1, 1].Style.Font.Size = 18;
                summarySheet.Cells[1, 1].Style.Font.Bold = true;

                summarySheet.Cells[3, 1].Value = "Analysis Date:";
                summarySheet.Cells[3, 2].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                summarySheet.Cells[4, 1].Value = "Analyzed Path:";
                summarySheet.Cells[4, 2].Value = selectedPath;

                summarySheet.Cells[6, 1].Value = "Total Files:";
                summarySheet.Cells[6, 2].Value = stats.TotalFiles;

                summarySheet.Cells[7, 1].Value = "Total Size:";
                summarySheet.Cells[7, 2].Value = Common.FormatBytes(stats.TotalSize);

                summarySheet.Cells[8, 1].Value = "Total Directories:";
                summarySheet.Cells[8, 2].Value = stats.TotalDirectories;

                summarySheet.Cells[9, 1].Value = "Empty Files:";
                summarySheet.Cells[9, 2].Value = stats.EmptyFiles;

                summarySheet.Cells[10, 1].Value = "Duplicate Files:";
                summarySheet.Cells[10, 2].Value = allFiles.Count(f => f.IsDuplicate);

                summarySheet.Column(1).Width = 25;
                summarySheet.Column(2).Width = 40;

                var filesSheet = package.Workbook.Worksheets.Add("All Files");

                filesSheet.Cells[1, 1].Value = "File Name";
                filesSheet.Cells[1, 2].Value = "Extension";
                filesSheet.Cells[1, 3].Value = "Size (Bytes)";
                filesSheet.Cells[1, 4].Value = "Size";
                filesSheet.Cells[1, 5].Value = "Directory";
                filesSheet.Cells[1, 6].Value = "Modified Date";
                filesSheet.Cells[1, 7].Value = "Is Duplicate";

                filesSheet.Row(1).Style.Font.Bold = true;

                int row = 2;
                foreach (var file in allFiles)
                {
                    filesSheet.Cells[row, 1].Value = file.Name;
                    filesSheet.Cells[row, 2].Value = file.Extension;
                    filesSheet.Cells[row, 3].Value = file.Size;
                    filesSheet.Cells[row, 4].Value = file.SizeFormatted;
                    filesSheet.Cells[row, 5].Value = file.DirectoryName;
                    filesSheet.Cells[row, 6].Value = file.LastModifiedFormatted;
                    filesSheet.Cells[row, 7].Value = file.IsDuplicate ? "Yes" : "No";
                    row++;
                }

                filesSheet.Cells[filesSheet.Dimension.Address].AutoFitColumns();

                package.SaveAs(new FileInfo(filePath));
            }
        }

        private void ExportToCsv(string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("File Name,Extension,Size (Bytes),Size,Directory,Modified Date,Is Duplicate");

                foreach (var file in allFiles)
                {
                    writer.WriteLine($"\"{file.Name}\",\"{file.Extension}\",{file.Size}," +
                        $"\"{file.SizeFormatted}\",\"{file.DirectoryName}\"," +
                        $"\"{file.LastModifiedFormatted}\",{(file.IsDuplicate ? "Yes" : "No")}");
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                var result = MessageBox.Show(
                    "Analysis is still running. Are you sure you want to close?",
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

        private async void CleanEmptyFolders_Click(object sender, RoutedEventArgs e)
        {
            var emptyFolders = await Task.Run(() =>
                FastAnalyzer.FindEmptyFolders(selectedPath));

            if (emptyFolders.Count > 0)
            {
                var result = MessageBox.Show(
                    $"Found {emptyFolders.Count:N0} empty folders.\n\nDelete them?",
                    "Empty Folders Found",
                    MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    foreach (var folder in emptyFolders)
                    {
                        try { Directory.Delete(folder); } catch { }
                    }
                }
            }
        }
    }
}