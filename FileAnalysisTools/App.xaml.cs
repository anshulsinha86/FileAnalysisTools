using System;
using System.Windows;

namespace FileAnalysisTools
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Handle any unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                MessageBox.Show($"An unexpected error occurred:\n\n{ex?.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }
    }
}