using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MaaGui.Converters;

/// <summary>
/// Check if object is equal to parameter
/// </summary>
public class ObjectEqualConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return value == parameter;

        return value.ToString() == parameter.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
