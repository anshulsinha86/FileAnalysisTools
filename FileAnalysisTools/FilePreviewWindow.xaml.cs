using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FileAnalysisTools
{
    public partial class FilePreviewWindow : Window
    {
        private FileInfoModel currentFile;
        public bool KeepFile { get; private set; }
        public bool RemoveFile { get; private set; }

        public FilePreviewWindow(FileInfoModel file)
        {
            InitializeComponent();
            currentFile = file;
            LoadPreview();
        }

        private void LoadPreview()
        {
            FileNameText.Text = currentFile.Name;
            FileInfoText.Text = $"Size: {currentFile.SizeFormatted} | Modified: {currentFile.LastModified:yyyy-MM-dd HH:mm:ss}";

            // Show generic info by default
            PathText.Text = currentFile.FullPath;
            SizeText.Text = currentFile.SizeFormatted;
            TypeText.Text = currentFile.Extension.ToUpper();
            CreatedText.Text = currentFile.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
            ModifiedText.Text = currentFile.LastModified.ToString("yyyy-MM-dd HH:mm:ss");
            AttributesText.Text = currentFile.Attributes;

            // Try to load specific preview based on file type
            var extension = currentFile.Extension.ToLower();

            try
            {
                if (IsTextFile(extension))
                {
                    LoadTextPreview();
                }
                else if (IsImageFile(extension))
                {
                    LoadImagePreview();
                }
                else
                {
                    GenericPreview.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                GenericPreview.Visibility = Visibility.Visible;
            }
        }

        private bool IsTextFile(string extension)
        {
            string[] textExtensions = { ".txt", ".log", ".xml", ".json", ".csv", ".md", ".cs", ".xaml",
                                       ".html", ".css", ".js", ".py", ".java", ".cpp", ".h", ".sql" };
            return Array.Exists(textExtensions, ext => ext == extension);
        }

        private bool IsImageFile(string extension)
        {
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".ico", ".tiff" };
            return Array.Exists(imageExtensions, ext => ext == extension);
        }

        private void LoadTextPreview()
        {
            if (currentFile.Size > 1024 * 1024) // Limit to 1MB for text preview
            {
                TextPreview.Text = "File too large to preview (>1MB). Showing file properties instead.";
                GenericPreview.Visibility = Visibility.Visible;
                return;
            }

            var content = File.ReadAllText(currentFile.FullPath);
            if (content.Length > 10000) // Limit display to 10,000 characters
            {
                content = content.Substring(0, 10000) + "\n\n... (content truncated for preview)";
            }

            TextPreview.Text = content;
            TextPreview.Visibility = Visibility.Visible;
            GenericPreview.Visibility = Visibility.Visible;
        }

        private void LoadImagePreview()
        {
            if (currentFile.Size > 10 * 1024 * 1024) // Limit to 10MB for image preview
            {
                GenericPreview.Visibility = Visibility.Visible;
                return;
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(currentFile.FullPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            ImagePreview.Source = bitmap;
            ImagePreview.Visibility = Visibility.Visible;
            GenericPreview.Visibility = Visibility.Visible;
        }

        private void OpenInExplorer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{currentFile.FullPath}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Explorer: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void KeepFile_Click(object sender, RoutedEventArgs e)
        {
            KeepFile = true;
            RemoveFile = false;
            DialogResult = true;
            Close();
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            RemoveFile = true;
            KeepFile = false;
            DialogResult = true;
            Close();
        }
    }
}