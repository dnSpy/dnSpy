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
using System.ComponentModel;
using System.Globalization;

namespace ICSharpCode.ILSpy.AsmEditor
{
	static class NumberVMUtils
	{
		public static string ToString(ulong value)
		{
			if (value <= 9)
				return value.ToString();
			return string.Format("0x{0:X}", value);
		}

		static ulong ParseUnsigned(string s, ulong max)
		{
			const ulong min = 0;
			ulong value;
			bool isValid;
			s = s.Trim();
			if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				isValid = ulong.TryParse(s.Substring(2), NumberStyles.HexNumber, null, out value);
			else
				isValid = ulong.TryParse(s, NumberStyles.Integer, null, out value);
			if (!isValid) {
				if (s.StartsWith("-"))
					throw new FormatException("Only non-negative integers are allowed.");
				throw new FormatException("The value is not an unsigned hexadecimal or decimal integer.");
			}
			if (value < min || value > max)
				throw new FormatException(string.Format("Value must be between {0} and {1} (0x{1:X}) inclusive.", min, max));
			return value;
		}

		public static byte ParseByte(string s)
		{
			return (byte)ParseUnsigned(s, byte.MaxValue);
		}

		public static ushort ParseUInt16(string s)
		{
			return (ushort)ParseUnsigned(s, ushort.MaxValue);
		}

		public static uint ParseUInt32(string s)
		{
			return (uint)ParseUnsigned(s, uint.MaxValue);
		}

		public static ulong ParseUInt64(string s)
		{
			return ParseUnsigned(s, ulong.MaxValue);
		}
	}

	struct CachedValidationError
	{
		readonly Func<string> checkError;
		bool errorMsgValid;
		string errorMsg;

		public bool HasError {
			get {
				CheckError();
				return !string.IsNullOrEmpty(errorMsg);
			}
		}

		public string ErrorMessage {
			get {
				CheckError();
				return errorMsg;
			}
		}

		public CachedValidationError(Func<string> checkError)
		{
			if (checkError == null)
				throw new ArgumentNullException();
			this.checkError = checkError;
			this.errorMsgValid = false;
			this.errorMsg = null;
		}

		public void Invalidate()
		{
			errorMsgValid = false;
		}

		void CheckError()
		{
			if (errorMsgValid)
				return;
			errorMsg = checkError();
			errorMsgValid = true;
		}
	}

	abstract class DataFieldVM<T> : INotifyPropertyChanged, IDataErrorInfo
	{
		readonly Action<DataFieldVM<T>> onUpdated;
		CachedValidationError cachedError;

		public T Value {
			get {
				T value;
				var s = ConvertToValue(out value);
				if (string.IsNullOrEmpty(s))
					return value;
				throw new FormatException(s);
			}
			set { SetValue(value); }
		}

		/// <summary>
		/// Gets the string representation of the value. This could be an invalid string. Use
		/// <see cref="Validate()"/> to check whether it's valid.
		/// </summary>
		public string StringValue {
			get { return stringValue; }
			set {
				if (stringValue != value) {
					stringValue = value;
					cachedError.Invalidate();
					OnPropertyChanged("StringValue");
					onUpdated(this);
				}
			}
		}
		string stringValue;

		/// <summary>
		/// true if the value is null (<see cref="StringValue"/> is empty)
		/// </summary>
		public bool IsNull {
			get { return string.IsNullOrWhiteSpace(StringValue); }
		}

		protected DataFieldVM(Action<DataFieldVM<T>> onUpdated)
		{
			if (onUpdated == null)
				throw new ArgumentNullException();
			this.onUpdated = onUpdated;
			this.cachedError = new CachedValidationError(() => Validate());
		}

		protected abstract void SetValue(T value);
		protected abstract string ConvertToValue(out T value);

		string Validate()
		{
			T value;
			try {
				return ConvertToValue(out value);
			}
			catch (Exception ex) {
				if (!string.IsNullOrEmpty(ex.Message))
					return ex.Message;
				return string.Format("Could not convert '{0}'.", StringValue);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

		public string Error {
			get { throw new NotImplementedException(); }
		}

		public string this[string columnName] {
			get {
				if (columnName == "StringValue")
					return cachedError.ErrorMessage;

				return string.Empty;
			}
		}

		public bool HasError {
			get { return cachedError.HasError; }
		}
	}

	sealed class NullableGuidVM : DataFieldVM<Guid?>
	{
		public NullableGuidVM(Action<DataFieldVM<Guid?>> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableGuidVM(Guid? value, Action<DataFieldVM<Guid?>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(Guid? value)
		{
			this.StringValue = value == null ? string.Empty : value.Value.ToString();
		}

		protected override string ConvertToValue(out Guid? value)
		{
			if (IsNull)
				value = null;
			else
				value = Guid.Parse(StringValue);
			return null;
		}
	}

	sealed class NullableByteVM : DataFieldVM<byte?>
	{
		public NullableByteVM(Action<DataFieldVM<byte?>> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableByteVM(byte? value, Action<DataFieldVM<byte?>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(byte? value)
		{
			this.StringValue = value == null ? string.Empty : NumberVMUtils.ToString(value.Value);
		}

		protected override string ConvertToValue(out byte? value)
		{
			if (IsNull)
				value = null;
			else
				value = NumberVMUtils.ParseByte(StringValue);
			return null;
		}
	}

	sealed class NullableUInt16VM : DataFieldVM<ushort?>
	{
		public NullableUInt16VM(Action<DataFieldVM<ushort?>> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableUInt16VM(ushort? value, Action<DataFieldVM<ushort?>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(ushort? value)
		{
			this.StringValue = value == null ? string.Empty : NumberVMUtils.ToString(value.Value);
		}

		protected override string ConvertToValue(out ushort? value)
		{
			if (IsNull)
				value = null;
			else
				value = NumberVMUtils.ParseUInt16(StringValue);
			return null;
		}
	}

	sealed class NullableUInt32VM : DataFieldVM<uint?>
	{
		public NullableUInt32VM(Action<DataFieldVM<uint?>> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableUInt32VM(uint? value, Action<DataFieldVM<uint?>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(uint? value)
		{
			this.StringValue = value == null ? string.Empty : NumberVMUtils.ToString(value.Value);
		}

		protected override string ConvertToValue(out uint? value)
		{
			if (IsNull)
				value = null;
			else
				value = NumberVMUtils.ParseUInt32(StringValue);
			return null;
		}
	}

	sealed class NullableUInt64VM : DataFieldVM<ulong?>
	{
		public NullableUInt64VM(Action<DataFieldVM<ulong?>> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableUInt64VM(ulong? value, Action<DataFieldVM<ulong?>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(ulong? value)
		{
			this.StringValue = value == null ? string.Empty : NumberVMUtils.ToString(value.Value);
		}

		protected override string ConvertToValue(out ulong? value)
		{
			if (IsNull)
				value = null;
			else
				value = NumberVMUtils.ParseUInt64(StringValue);
			return null;
		}
	}

	sealed class GuidVM : DataFieldVM<Guid>
	{
		public GuidVM(Action<DataFieldVM<Guid>> onUpdated)
			: this(new Guid(), onUpdated)
		{
		}

		public GuidVM(Guid value, Action<DataFieldVM<Guid>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(Guid value)
		{
			this.StringValue = value.ToString();
		}

		protected override string ConvertToValue(out Guid value)
		{
			value = Guid.Parse(StringValue);
			return null;
		}
	}
}
