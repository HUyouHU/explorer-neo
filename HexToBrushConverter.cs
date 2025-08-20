using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Globalization;

namespace CustomExplorer
{
    public class HexToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var hexColor = value as string;

            if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith("#"))
            {
                // Return default brush if color is not set or invalid
                return new SolidColorBrush(Colors.Black);
            }

            try
            {
                byte r = (byte)System.Convert.ToUInt32(hexColor.Substring(1, 2), 16);
                byte g = (byte)System.Convert.ToUInt32(hexColor.Substring(3, 2), 16);
                byte b = (byte)System.Convert.ToUInt32(hexColor.Substring(5, 2), 16);
                byte a = hexColor.Length > 7 ? (byte)System.Convert.ToUInt32(hexColor.Substring(7, 2), 16) : (byte)255;
                return new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
            }
            catch
            {
                // Return default brush on parsing error
                return new SolidColorBrush(Colors.Black);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
