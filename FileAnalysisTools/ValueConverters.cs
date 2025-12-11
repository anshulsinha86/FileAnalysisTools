// Required for XAML boolean to visibility conversions
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FileAnalysisTools
{
    /// <summary>
    /// Converts boolean values to Visibility
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// Inverts boolean values
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }
    }

    /// <summary>
    /// Converts file size to formatted string
    /// </summary>
    [ValueConversion(typeof(long), typeof(string))]
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long size)
            {
                return Common.FormatBytes(size);
            }
            return "0 B";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Highlights primary files with special color
    /// </summary>
    [ValueConversion(typeof(bool), typeof(System.Windows.Media.Brush))]
    public class PrimaryFileColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPrimary && isPrimary)
            {
                return System.Windows.Media.Brushes.Gold;
            }
            return System.Windows.Media.Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

// ADD TO App.xaml:
// Register converters globally so they can be used throughout the app

/*
<Application x:Class="FileAnalysisTools.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:FileAnalysisTools"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <!-- Register Value Converters -->
        <local:BoolToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
        <local:InverseBooleanConverter x:Key="InverseBooleanConverter"/>
        <local:FileSizeConverter x:Key="FileSizeConverter"/>
        <local:PrimaryFileColorConverter x:Key="PrimaryFileColorConverter"/>
    </Application.Resources>
</Application>
*/