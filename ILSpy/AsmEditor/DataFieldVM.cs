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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace ICSharpCode.ILSpy.AsmEditor
{
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

	abstract class DataFieldVM : ViewModelBase
	{
		readonly Action<DataFieldVM> onUpdated;
		CachedValidationError cachedError;

		public abstract object ObjectValue { get; set; }

		/// <summary>
		/// Gets the string representation of the value. This could be an invalid string. Use
		/// <see cref="Validate()"/> to check whether it's valid.
		/// </summary>
		public string StringValue {
			get { return stringValue; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				if (stringValue != value)
					ForceWriteStringValue(value);
			}
		}
		string stringValue = string.Empty;

		protected void WriteStringValueFromConstructor(string value)
		{
			Debug.Assert(stringValue == string.Empty);
			stringValue = value;
		}

		protected void ForceWriteStringValue(string value)
		{
			stringValue = value;
			cachedError.Invalidate();
			OnStringValueChanged();
			OnPropertyChanged("StringValue");
			onUpdated(this);
		}

		protected void Revalidate()
		{
			cachedError.Invalidate();
			HasErrorUpdated();
		}

		protected virtual void OnStringValueChanged()
		{
		}

		/// <summary>
		/// true if the value is null (<see cref="StringValue"/> is empty)
		/// </summary>
		public bool IsNull {
			get { return string.IsNullOrWhiteSpace(StringValue); }
		}

		protected DataFieldVM(Action<DataFieldVM> onUpdated)
		{
			if (onUpdated == null)
				throw new ArgumentNullException();
			this.onUpdated = onUpdated;
			this.cachedError = new CachedValidationError(() => Validate());
		}

		protected abstract string Validate();

		internal abstract string ConvertToObjectValue(out object value);

		protected override string Verify(string columnName)
		{
			if (columnName == "StringValue")
				return cachedError.ErrorMessage;

			return string.Empty;
		}

		public override bool HasError {
			get { return cachedError.HasError; }
		}
	}

	abstract class DataFieldVM<T> : DataFieldVM
	{
		public override object ObjectValue {
			get { return Value; }
			set { Value = (T)value; }
		}

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

		protected DataFieldVM(Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
		}

		protected void SetValueFromConstructor(T value)
		{
			WriteStringValueFromConstructor(OnNewValue(value));
		}

		protected void SetValue(T value)
		{
			StringValue = OnNewValue(value);
		}

		protected abstract string OnNewValue(T value);
		protected abstract string ConvertToValue(out T value);

		internal override string ConvertToObjectValue(out object value)
		{
			T v;
			var error = ConvertToValue(out v);
			value = v;
			return error;
		}

		protected override string Validate()
		{
			T value;
			try {
				return ConvertToValue(out value);
			}
			catch (Exception ex) {
				Debug.Fail("Exception caught in Validate(). ConvertToValue() should return an error string instead of throwing for performance reasons! Throwing is SLOOOOW!");
				if (!string.IsNullOrEmpty(ex.Message))
					return ex.Message;
				return string.Format("Could not convert '{0}'", StringValue);
			}
		}
	}

	abstract class NumberDataFieldVM<T, U> : DataFieldVM<T>
	{
		/// <summary>
		/// Use decimal by default if it's a number
		/// </summary>
		public bool UseDecimal { get; set; }

		public U Min {
			get { return min; }
			set {
				min = value;
				Revalidate();
			}
		}
		U min;

		public U Max {
			get { return max; }
			set {
				max = value;
				Revalidate();
			}
		}
		U max;

		protected NumberDataFieldVM(Action<DataFieldVM> onUpdated, U min, U max)
			: base(onUpdated)
		{
			this.min = min;
			this.max = max;
		}
	}

	sealed class NullableGuidVM : DataFieldVM<Guid?>
	{
		public NullableGuidVM(Action<DataFieldVM> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableGuidVM(Guid? value, Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(Guid? value)
		{
			return value == null ? string.Empty : value.Value.ToString();
		}

		protected override string ConvertToValue(out Guid? value)
		{
			string error = null;
			if (IsNull)
				value = null;
			else
				value = ParseGuid(StringValue, out error);
			return error;
		}

		internal static Guid ParseGuid(string s, out string error)
		{
			Guid res;
			if (Guid.TryParse(s, out res)) {
				error = null;
				return res;
			}

			error = "Invalid GUID";
			return Guid.Empty;
		}
	}

	sealed class HexStringVM : DataFieldVM<IList<byte>>
	{
		public bool UpperCaseHex {
			get { return upperCaseHex; }
			set { upperCaseHex = value; }
		}
		bool upperCaseHex = true;

		public HexStringVM(Action<DataFieldVM> onUpdated)
			: this(null, onUpdated)
		{
		}

		public HexStringVM(IList<byte> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<byte> value)
		{
			return NumberVMUtils.ByteArrayToString(value, UpperCaseHex);
		}

		protected override string ConvertToValue(out IList<byte> value)
		{
			string error;
			value = NumberVMUtils.ParseByteArray(StringValue, out error);
			return error;
		}
	}

	sealed class NullableByteVM : NumberDataFieldVM<byte?, byte>
	{
		public NullableByteVM(Action<DataFieldVM> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableByteVM(byte? value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, byte.MinValue, byte.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(byte? value)
		{
			return value == null ? string.Empty : NumberVMUtils.ToString(value.Value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out byte? value)
		{
			string error = null;
			if (IsNull)
				value = null;
			else
				value = NumberVMUtils.ParseByte(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class NullableUInt16VM : NumberDataFieldVM<ushort?, ushort>
	{
		public NullableUInt16VM(Action<DataFieldVM> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableUInt16VM(ushort? value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, ushort.MinValue, ushort.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(ushort? value)
		{
			return value == null ? string.Empty : NumberVMUtils.ToString(value.Value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out ushort? value)
		{
			string error = null;
			if (IsNull)
				value = null;
			else
				value = NumberVMUtils.ParseUInt16(StringValue, Min, Max, out error);
			return error;
		}
	}

	class NullableUInt32VM : NumberDataFieldVM<uint?, uint>
	{
		public NullableUInt32VM(Action<DataFieldVM> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableUInt32VM(uint? value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, uint.MinValue, uint.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(uint? value)
		{
			return value == null ? string.Empty : NumberVMUtils.ToString(value.Value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out uint? value)
		{
			string error = null;
			if (IsNull)
				value = null;
			else
				value = NumberVMUtils.ParseUInt32(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class NullableUInt64VM : NumberDataFieldVM<ulong?, ulong>
	{
		public NullableUInt64VM(Action<DataFieldVM> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableUInt64VM(ulong? value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, ulong.MinValue, ulong.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(ulong? value)
		{
			return value == null ? string.Empty : NumberVMUtils.ToString(value.Value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out ulong? value)
		{
			string error = null;
			if (IsNull)
				value = null;
			else
				value = NumberVMUtils.ParseUInt64(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class NullableCompressedUInt32 : NullableUInt32VM
	{
		public NullableCompressedUInt32(Action<DataFieldVM> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableCompressedUInt32(uint? value, Action<DataFieldVM> onUpdated)
			: base(value, onUpdated)
		{
			Min = DnlibDialogs.ModelUtils.COMPRESSED_UINT32_MIN;
			Max = DnlibDialogs.ModelUtils.COMPRESSED_UINT32_MAX;
		}
	}

	sealed class BooleanVM : DataFieldVM<bool>
	{
		public BooleanVM(Action<DataFieldVM> onUpdated)
			: this(false, onUpdated)
		{
		}

		public BooleanVM(bool value, Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(bool value)
		{
			return NumberVMUtils.ToString(value);
		}

		protected override string ConvertToValue(out bool value)
		{
			string error;
			value = NumberVMUtils.ParseBoolean(StringValue, out error);
			return error;
		}
	}

	sealed class CharVM : DataFieldVM<char>
	{
		public CharVM(Action<DataFieldVM> onUpdated)
			: this((char)0, onUpdated)
		{
		}

		public CharVM(char value, Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(char value)
		{
			return NumberVMUtils.ToString(value);
		}

		protected override string ConvertToValue(out char value)
		{
			string error;
			value = NumberVMUtils.ParseChar(StringValue, out error);
			return error;
		}
	}

	sealed class ByteVM : NumberDataFieldVM<byte, byte>
	{
		public ByteVM(Action<DataFieldVM> onUpdated)
			: this(0, onUpdated)
		{
		}

		public ByteVM(byte value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, byte.MinValue, byte.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(byte value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out byte value)
		{
			string error;
			value = NumberVMUtils.ParseByte(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class UInt16VM : NumberDataFieldVM<ushort, ushort>
	{
		public UInt16VM(Action<DataFieldVM> onUpdated)
			: this(0, onUpdated)
		{
		}

		public UInt16VM(ushort value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, ushort.MinValue, ushort.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(ushort value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out ushort value)
		{
			string error;
			value = NumberVMUtils.ParseUInt16(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class UInt32VM : NumberDataFieldVM<uint, uint>
	{
		public UInt32VM(Action<DataFieldVM> onUpdated)
			: this(0, onUpdated)
		{
		}

		public UInt32VM(uint value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, uint.MinValue, uint.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(uint value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out uint value)
		{
			string error;
			value = NumberVMUtils.ParseUInt32(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class UInt64VM : NumberDataFieldVM<ulong, ulong>
	{
		public UInt64VM(Action<DataFieldVM> onUpdated)
			: this(0, onUpdated)
		{
		}

		public UInt64VM(ulong value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, ulong.MinValue, ulong.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(ulong value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out ulong value)
		{
			string error;
			value = NumberVMUtils.ParseUInt64(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class SByteVM : NumberDataFieldVM<sbyte, sbyte>
	{
		public SByteVM(Action<DataFieldVM> onUpdated)
			: this(0, onUpdated)
		{
		}

		public SByteVM(sbyte value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, sbyte.MinValue, sbyte.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(sbyte value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out sbyte value)
		{
			string error;
			value = NumberVMUtils.ParseSByte(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class Int16VM : NumberDataFieldVM<short, short>
	{
		public Int16VM(Action<DataFieldVM> onUpdated)
			: this(0, onUpdated)
		{
		}

		public Int16VM(short value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, short.MinValue, short.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(short value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out short value)
		{
			string error;
			value = NumberVMUtils.ParseInt16(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class Int32VM : NumberDataFieldVM<int, int>
	{
		public Int32VM(Action<DataFieldVM> onUpdated)
			: this(0, onUpdated)
		{
		}

		public Int32VM(int value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, int.MinValue, int.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(int value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out int value)
		{
			string error;
			value = NumberVMUtils.ParseInt32(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class Int64VM : NumberDataFieldVM<long, long>
	{
		public Int64VM(Action<DataFieldVM> onUpdated)
			: this(0, onUpdated)
		{
		}

		public Int64VM(long value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, long.MinValue, long.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(long value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out long value)
		{
			string error;
			value = NumberVMUtils.ParseInt64(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class SingleVM : DataFieldVM<float>
	{
		public SingleVM(Action<DataFieldVM> onUpdated)
			: this(0, onUpdated)
		{
		}

		public SingleVM(float value, Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(float value)
		{
			return NumberVMUtils.ToString(value);
		}

		protected override string ConvertToValue(out float value)
		{
			string error;
			value = NumberVMUtils.ParseSingle(StringValue, out error);
			return error;
		}
	}

	sealed class DoubleVM : DataFieldVM<double>
	{
		public DoubleVM(Action<DataFieldVM> onUpdated)
			: this(0, onUpdated)
		{
		}

		public DoubleVM(double value, Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(double value)
		{
			return NumberVMUtils.ToString(value);
		}

		protected override string ConvertToValue(out double value)
		{
			string error;
			value = NumberVMUtils.ParseDouble(StringValue, out error);
			return error;
		}
	}

	sealed class StringVM : DataFieldVM<string>
	{
		readonly bool allowNullString;

		public StringVM(Action<DataFieldVM> onUpdated, bool allowNullString = false)
			: this(string.Empty, onUpdated, allowNullString)
		{
		}

		public StringVM(string value, Action<DataFieldVM> onUpdated, bool allowNullString = false)
			: base(onUpdated)
		{
			this.allowNullString = allowNullString;
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(string value)
		{
			return NumberVMUtils.ToString(value, allowNullString);
		}

		protected override string ConvertToValue(out string value)
		{
			string error;
			value = NumberVMUtils.ParseString(StringValue, allowNullString, out error);
			return error;
		}
	}

	sealed class DecimalVM : DataFieldVM<decimal>
	{
		public DecimalVM(Action<DataFieldVM> onUpdated)
			: this(0, onUpdated)
		{
		}

		public DecimalVM(decimal value, Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(decimal value)
		{
			return NumberVMUtils.ToString(value);
		}

		protected override string ConvertToValue(out decimal value)
		{
			string error;
			value = NumberVMUtils.ParseDecimal(StringValue, out error);
			return error;
		}
	}

	sealed class DateTimeVM : DataFieldVM<DateTime>
	{
		public DateTimeVM(Action<DataFieldVM> onUpdated)
			: this(DateTime.Now, onUpdated)
		{
		}

		public DateTimeVM(DateTime value, Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(DateTime value)
		{
			return NumberVMUtils.ToString(value);
		}

		protected override string ConvertToValue(out DateTime value)
		{
			string error;
			value = NumberVMUtils.ParseDateTime(StringValue, out error);
			return error;
		}
	}

	sealed class TimeSpanVM : DataFieldVM<TimeSpan>
	{
		public TimeSpanVM(Action<DataFieldVM> onUpdated)
			: this(TimeSpan.Zero, onUpdated)
		{
		}

		public TimeSpanVM(TimeSpan value, Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(TimeSpan value)
		{
			return NumberVMUtils.ToString(value);
		}

		protected override string ConvertToValue(out TimeSpan value)
		{
			string error;
			value = NumberVMUtils.ParseTimeSpan(StringValue, out error);
			return error;
		}
	}

	sealed class GuidVM : DataFieldVM<Guid>
	{
		public GuidVM(Action<DataFieldVM> onUpdated)
			: this(new Guid(), onUpdated)
		{
		}

		public GuidVM(Guid value, Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(Guid value)
		{
			return value.ToString();
		}

		protected override string ConvertToValue(out Guid value)
		{
			string error;
			value = NullableGuidVM.ParseGuid(StringValue, out error);
			return error;
		}
	}

	sealed class BooleanListDataFieldVM : DataFieldVM<IList<bool>>
	{
		public BooleanListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(new bool[0], onUpdated)
		{
		}

		public BooleanListDataFieldVM(IList<bool> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<bool> value)
		{
			return NumberVMUtils.ToString(value);
		}

		protected override string ConvertToValue(out IList<bool> value)
		{
			string error;
			value = NumberVMUtils.ParseBooleanList(StringValue, out error);
			return error;
		}
	}

	sealed class CharListDataFieldVM : DataFieldVM<IList<char>>
	{
		public CharListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(new char[0], onUpdated)
		{
		}

		public CharListDataFieldVM(IList<char> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<char> value)
		{
			return NumberVMUtils.ToString(value);
		}

		protected override string ConvertToValue(out IList<char> value)
		{
			string error;
			value = NumberVMUtils.ParseCharList(StringValue, out error);
			return error;
		}
	}

	sealed class ByteListDataFieldVM : NumberDataFieldVM<IList<byte>, byte>
	{
		public ByteListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(new byte[0], onUpdated)
		{
		}

		public ByteListDataFieldVM(IList<byte> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, byte.MinValue, byte.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<byte> value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out IList<byte> value)
		{
			string error;
			value = NumberVMUtils.ParseByteList(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class UInt16ListDataFieldVM : NumberDataFieldVM<IList<ushort>, ushort>
	{
		public UInt16ListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(new ushort[0], onUpdated)
		{
		}

		public UInt16ListDataFieldVM(IList<ushort> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, ushort.MinValue, ushort.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<ushort> value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out IList<ushort> value)
		{
			string error;
			value = NumberVMUtils.ParseUInt16List(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class UInt32ListDataFieldVM : NumberDataFieldVM<IList<uint>, uint>
	{
		public UInt32ListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(new uint[0], onUpdated)
		{
		}

		public UInt32ListDataFieldVM(IList<uint> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, uint.MinValue, uint.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<uint> value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out IList<uint> value)
		{
			string error;
			value = NumberVMUtils.ParseUInt32List(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class UInt64ListDataFieldVM : NumberDataFieldVM<IList<ulong>, ulong>
	{
		public UInt64ListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(new ulong[0], onUpdated)
		{
		}

		public UInt64ListDataFieldVM(IList<ulong> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, ulong.MinValue, ulong.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<ulong> value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out IList<ulong> value)
		{
			string error;
			value = NumberVMUtils.ParseUInt64List(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class SByteListDataFieldVM : NumberDataFieldVM<IList<sbyte>, sbyte>
	{
		public SByteListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(new sbyte[0], onUpdated)
		{
		}

		public SByteListDataFieldVM(IList<sbyte> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, sbyte.MinValue, sbyte.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<sbyte> value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out IList<sbyte> value)
		{
			string error;
			value = NumberVMUtils.ParseSByteList(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class Int16ListDataFieldVM : NumberDataFieldVM<IList<short>, short>
	{
		public Int16ListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(new short[0], onUpdated)
		{
		}

		public Int16ListDataFieldVM(IList<short> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, short.MinValue, short.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<short> value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out IList<short> value)
		{
			string error;
			value = NumberVMUtils.ParseInt16List(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class Int32ListDataFieldVM : NumberDataFieldVM<IList<int>, int>
	{
		public Int32ListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(new int[0], onUpdated)
		{
		}

		public Int32ListDataFieldVM(IList<int> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, int.MinValue, int.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<int> value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out IList<int> value)
		{
			string error;
			value = NumberVMUtils.ParseInt32List(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class Int64ListDataFieldVM : NumberDataFieldVM<IList<long>, long>
	{
		public Int64ListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(new long[0], onUpdated)
		{
		}

		public Int64ListDataFieldVM(IList<long> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated, long.MinValue, long.MaxValue)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<long> value)
		{
			return NumberVMUtils.ToString(value, Min, Max, UseDecimal);
		}

		protected override string ConvertToValue(out IList<long> value)
		{
			string error;
			value = NumberVMUtils.ParseInt64List(StringValue, Min, Max, out error);
			return error;
		}
	}

	sealed class SingleListDataFieldVM : DataFieldVM<IList<float>>
	{
		public SingleListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(new float[0], onUpdated)
		{
		}

		public SingleListDataFieldVM(IList<float> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<float> value)
		{
			return NumberVMUtils.ToString(value);
		}

		protected override string ConvertToValue(out IList<float> value)
		{
			string error;
			value = NumberVMUtils.ParseSingleList(StringValue, out error);
			return error;
		}
	}

	sealed class DoubleListDataFieldVM : DataFieldVM<IList<double>>
	{
		public DoubleListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(new double[0], onUpdated)
		{
		}

		public DoubleListDataFieldVM(IList<double> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<double> value)
		{
			return NumberVMUtils.ToString(value);
		}

		protected override string ConvertToValue(out IList<double> value)
		{
			string error;
			value = NumberVMUtils.ParseDoubleList(StringValue, out error);
			return error;
		}
	}

	sealed class StringListDataFieldVM : DataFieldVM<IList<string>>
	{
		readonly bool allowNullString;

		public StringListDataFieldVM(Action<DataFieldVM> onUpdated, bool allowNullString = true)
			: this(new string[0], onUpdated, allowNullString)
		{
		}

		public StringListDataFieldVM(IList<string> value, Action<DataFieldVM> onUpdated, bool allowNullString = true)
			: base(onUpdated)
		{
			this.allowNullString = allowNullString;
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<string> value)
		{
			return NumberVMUtils.ToString(value, allowNullString);
		}

		protected override string ConvertToValue(out IList<string> value)
		{
			string error;
			value = NumberVMUtils.ParseStringList(StringValue, allowNullString, out error);
			return error;
		}
	}

	sealed class DefaultConverterVM<T> : DataFieldVM<T>
	{
		static readonly TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));

		static DefaultConverterVM()
		{
			if (!converter.CanConvertTo(null, typeof(string)))
				throw new InvalidOperationException(string.Format("Converter can't convert a {0} to a string", typeof(T)));
			if (!converter.CanConvertFrom(null, typeof(string)))
				throw new InvalidOperationException(string.Format("Converter can't convert a string to a {0}", typeof(T)));
		}

		public DefaultConverterVM(Action<DataFieldVM> onUpdated)
			: this(default(T), onUpdated)
		{
		}

		public DefaultConverterVM(T value, Action<DataFieldVM> onUpdated)
			: base(onUpdated)
		{
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(T value)
		{
			return (string)converter.ConvertTo(null, CultureInfo.InvariantCulture, value, typeof(string));
		}

		protected override string ConvertToValue(out T value)
		{
			string error;
			try {
				value = (T)converter.ConvertFrom(null, CultureInfo.InvariantCulture, StringValue);
				error = string.Empty;
			}
			catch (Exception ex) {
				value = default(T);
				error = string.Format("Value must be a {0}.\nError: {1}", typeof(T).FullName, ex.Message);
			}
			return error;
		}
	}
}
