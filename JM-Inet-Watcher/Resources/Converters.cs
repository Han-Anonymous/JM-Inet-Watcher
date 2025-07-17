using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace JM_Inet_Watcher.Resources
{
    /// <summary>
    /// Converts service status text to appropriate brush color
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                if (status.Contains("Running"))
                    return Application.Current.FindResource("SuccessBrush");
                else if (status.Contains("Stopped"))
                    return Application.Current.FindResource("ErrorBrush");
                else
                    return Application.Current.FindResource("WarningBrush");
            }
            return Application.Current.FindResource("WarningBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean to visibility with optional inversion
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (Invert) boolValue = !boolValue;
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool result = visibility == Visibility.Visible;
                return Invert ? !result : result;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts service status to appropriate icon geometry
    /// </summary>
    public class StatusToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                if (status.Contains("Running"))
                    return Application.Current.FindResource("StatusIconGeometry");
                else if (status.Contains("Stopped"))
                    return Application.Current.FindResource("ErrorIconGeometry");
                else
                    return Application.Current.FindResource("WarningIconGeometry");
            }
            return Application.Current.FindResource("WarningIconGeometry");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}