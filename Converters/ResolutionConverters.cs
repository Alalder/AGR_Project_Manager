using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using AGR_Project_Manager.Services;

namespace AGR_Project_Manager.Converters
{
    /// <summary>
    /// Конвертер разрешения текстуры в цвет фона бейджа
    /// </summary>
    public class ResolutionToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string resolution = value as string;

            if (string.IsNullOrEmpty(resolution))
                return Application.Current.FindResource("TextureZoneEmpty");

            string colorKey = TextureResolutionHelper.GetResolutionColor(resolution);
            return Application.Current.FindResource(colorKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер для видимости бейджа разрешения
    /// </summary>
    public class ResolutionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string resolution = value as string;
            return string.IsNullOrEmpty(resolution) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}