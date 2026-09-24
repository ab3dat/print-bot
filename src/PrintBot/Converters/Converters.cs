using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PrintBot.Models;

namespace PrintBot.Converters;

public class BooleanNegationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;
}

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToEmojiConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PrintJobStatus status)
        {
            return status switch
            {
                PrintJobStatus.Queued => "⬜",
                PrintJobStatus.Printing => "🟡",
                PrintJobStatus.Printed => "✅",
                PrintJobStatus.Failed => "❌",
                PrintJobStatus.Skipped => "⏭️",
                _ => "❓"
            };
        }
        return "❓";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class DuplexToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Printing.Duplexing duplex)
            return duplex != System.Printing.Duplexing.OneSided;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isDuplex)
            return isDuplex ? System.Printing.Duplexing.TwoSidedLongEdge : System.Printing.Duplexing.OneSided;
        return System.Printing.Duplexing.OneSided;
    }
}
