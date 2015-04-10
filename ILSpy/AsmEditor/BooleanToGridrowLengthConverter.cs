
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ICSharpCode.ILSpy.AsmEditor
{
	/// <summary>
	/// Converts a <see cref="bool"/> to a <see cref="GridLength"/>. If the value is true, it's
	/// converted to a "1*" or a "<user-parameter>*" value, else to a 0px length. The user can set
	/// ConverterParameter to the desired value. 1 is default.
	/// </summary>
	sealed class BooleanToGridrowLengthConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			double starValue = 1;
			if (parameter != null)
				starValue = System.Convert.ToDouble(parameter, culture);
			return (bool)value ? new GridLength(starValue, GridUnitType.Star) : new GridLength(0);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
