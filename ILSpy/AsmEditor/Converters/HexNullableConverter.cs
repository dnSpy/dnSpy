/*
    Copyright (C) 2014-2015 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ICSharpCode.ILSpy.AsmEditor.Converters
{
	sealed class HexNullableConverter : IValueConverter
	{
		// We need state. Doesn't seem to be possible to pass in the TextBox itself or its Text
		// property to the 'parameter' arg since it would require binding or extra code.
		string lastValue;

		enum NumberBase
		{
			Unknown,
			Decimal,
			Hex,
		}

		NumberBase GetNumberBase()
		{
			if (lastValue == null)
				return NumberBase.Unknown;
			var s = lastValue;
			if (s.StartsWith("-", StringComparison.OrdinalIgnoreCase))
				s = s.Substring(1);
			if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				return NumberBase.Hex;
			return NumberBase.Decimal;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return string.Empty;

			var valueType = value.GetType();

			if (valueType == typeof(byte))
				return ConvertUnsigned((byte)value);
			if (valueType == typeof(ushort))
				return ConvertUnsigned((ushort)value);
			if (valueType == typeof(uint))
				return ConvertUnsigned((uint)value);
			if (valueType == typeof(ulong))
				return ConvertUnsigned((ulong)value);

			if (valueType == typeof(sbyte))
				return ConvertSigned((sbyte)value);
			if (valueType == typeof(short))
				return ConvertSigned((short)value);
			if (valueType == typeof(int))
				return ConvertSigned((int)value);
			if (valueType == typeof(long))
				return ConvertSigned((long)value);

			throw new ArgumentException("Input is an unsupported type");
		}

		string ConvertUnsigned(ulong value)
		{
			var numBase = GetNumberBase();
			if ((numBase == NumberBase.Unknown && value <= 9) || numBase == NumberBase.Decimal)
				return value.ToString();
			return string.Format("0x{0:X}", value);
		}

		string ConvertSigned(long value)
		{
			var numBase = GetNumberBase();
			if ((numBase == NumberBase.Unknown && -9 <= value && value <= 9) || numBase == NumberBase.Decimal)
				return value.ToString();
			if (value >= 0)
				return string.Format("0x{0:X}", value);
			return string.Format("-0x{0:X}", -value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var s = (string)value;
			s = s.Trim();
			lastValue = s;
			if (s == string.Empty)
				return null;

			try {
				checked {
					if (targetType == typeof(byte?))
						return (byte?)ConvertUnsigned(s);
					if (targetType == typeof(ushort?))
						return (ushort?)ConvertUnsigned(s);
					if (targetType == typeof(uint?))
						return (uint?)ConvertUnsigned(s);
					if (targetType == typeof(ulong?))
						return (ulong?)ConvertUnsigned(s);

					if (targetType == typeof(sbyte?))
						return (sbyte?)ConvertSigned(s);
					if (targetType == typeof(short?))
						return (short?)ConvertSigned(s);
					if (targetType == typeof(int?))
						return (int?)ConvertSigned(s);
					if (targetType == typeof(long?))
						return (long?)ConvertSigned(s);
				}
			}
			catch {
				return DependencyProperty.UnsetValue;
			}

			throw new ArgumentException("Input is an unsupported type");
		}

		ulong ConvertUnsigned(string s)
		{
			if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				return ulong.Parse(s.Substring(2), NumberStyles.HexNumber);
			return ulong.Parse(s, NumberStyles.Integer);
		}

		long ConvertSigned(string s)
		{
			bool isNeg = s.StartsWith("-", StringComparison.OrdinalIgnoreCase);
			if (isNeg)
				s = s.Substring(1);
			long val = (long)ConvertUnsigned(s);
			return isNeg ? -val : val;
		}
	}
}
