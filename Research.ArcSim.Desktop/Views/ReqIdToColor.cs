using System;
using System.Globalization;

namespace Research.ArcSim.Desktop.Views
{
	public class ReqIdToColor : IValueConverter
    {
        private Color[] majorColors = {
                Colors.Red,
                Colors.Blue,
                Colors.Green,
                Colors.Brown,
                Colors.Purple
            };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Colors.Black;
            else
                return majorColors[int.Parse(value.ToString()) % majorColors.Length];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

