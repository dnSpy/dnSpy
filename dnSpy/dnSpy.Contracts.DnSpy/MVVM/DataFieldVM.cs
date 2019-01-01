/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.DnSpy.Properties;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Contracts.MVVM {
	struct CachedValidationError {
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

		public CachedValidationError(Func<string> checkError) {
			this.checkError = checkError ?? throw new ArgumentNullException(nameof(checkError));
			errorMsgValid = false;
			errorMsg = null;
		}

		public void Invalidate() => errorMsgValid = false;

		void CheckError() {
			if (errorMsgValid)
				return;
			errorMsg = checkError();
			errorMsgValid = true;
		}
	}

	/// <summary>
	/// Data field base class
	/// </summary>
	public abstract class DataFieldVM : ViewModelBase {
		readonly Action<DataFieldVM> onUpdated;
		CachedValidationError cachedError;

		/// <summary>
		/// Gets/sets the value
		/// </summary>
		public abstract object ObjectValue { get; set; }

		/// <summary>
		/// Gets the string representation of the value. This could be an invalid string. Use
		/// <see cref="Validate()"/> to check whether it's valid.
		/// </summary>
		public string StringValue {
			get => stringValue;
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (stringValue != value)
					ForceWriteStringValue(value);
			}
		}
		string stringValue = string.Empty;

		/// <summary>
		/// Must only be called from the constructor
		/// </summary>
		/// <param name="value">Initial <see cref="StringValue"/></param>
		protected void WriteStringValueFromConstructor(string value) {
			Debug.Assert(stringValue == string.Empty);
			stringValue = value;
		}

		/// <summary>
		/// Force writing a new <see cref="StringValue"/> even if nothing changed
		/// </summary>
		/// <param name="value">New value</param>
		protected void ForceWriteStringValue(string value) {
			stringValue = value;
			cachedError.Invalidate();
			OnStringValueChanged();
			OnPropertyChanged(nameof(StringValue));
			onUpdated(this);
		}

		/// <summary>
		/// Revalidates the field for errors
		/// </summary>
		protected void Revalidate() {
			cachedError.Invalidate();
			HasErrorUpdated();
		}

		/// <summary>
		/// Called when <see cref="StringValue"/> gets updated
		/// </summary>
		protected virtual void OnStringValueChanged() { }

		/// <summary>
		/// true if the value is null (<see cref="StringValue"/> is empty)
		/// </summary>
		public bool IsNull => string.IsNullOrWhiteSpace(StringValue);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		protected DataFieldVM(Action<DataFieldVM> onUpdated) {
			this.onUpdated = onUpdated ?? throw new ArgumentNullException(nameof(onUpdated));
			cachedError = new CachedValidationError(() => Validate());
		}

		/// <summary>
		/// Validates the data. Returns null or an empty string if there was no error,
		/// or an error string that can be shown to the user.
		/// </summary>
		/// <returns></returns>
		protected abstract string Validate();

		/// <summary>
		/// Converts the string to the target value. Returns null or an empty string if
		/// there were no errors, else an error string that can be shown to the user.
		/// </summary>
		/// <param name="value">Result</param>
		/// <returns></returns>
		public abstract string ConvertToObjectValue(out object value);

		/// <summary>
		/// Checks the string for errors
		/// </summary>
		/// <param name="columnName">Property name</param>
		/// <returns></returns>
		protected override string Verify(string columnName) {
			if (columnName == nameof(StringValue))
				return cachedError.ErrorMessage;

			return string.Empty;
		}

		/// <summary>
		/// true if there's at least one error
		/// </summary>
		public override bool HasError => cachedError.HasError;
	}

	/// <summary>
	/// Data field base class
	/// </summary>
	/// <typeparam name="T">Type of data</typeparam>
	public abstract class DataFieldVM<T> : DataFieldVM {
		/// <summary>
		/// Gets/sets the value
		/// </summary>
		public override object ObjectValue {
			get => Value;
			set => Value = (T)value;
		}

		/// <summary>
		/// Gets/sets the value
		/// </summary>
		public T Value {
			get {
				var s = ConvertToValue(out var value);
				if (string.IsNullOrEmpty(s))
					return value;
				throw new FormatException(s);
			}
			set { SetValue(value); }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		protected DataFieldVM(Action<DataFieldVM> onUpdated)
			: base(onUpdated) {
		}

		/// <summary>
		/// Must only be called from the constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		protected void SetValueFromConstructor(T value) => WriteStringValueFromConstructor(OnNewValue(value));

		/// <summary>
		/// Writes a new value
		/// </summary>
		/// <param name="value">Value</param>
		protected void SetValue(T value) => StringValue = OnNewValue(value);

		/// <summary>
		/// Converts <paramref name="value"/> to a string
		/// </summary>
		/// <param name="value">New value</param>
		/// <returns></returns>
		protected abstract string OnNewValue(T value);

		/// <summary>
		/// Converts the current string to the real value. Returns null or an empty string if
		/// there were no errors, else an error string that can be shown to the user.
		/// </summary>
		/// <param name="value">Result</param>
		/// <returns></returns>
		protected abstract string ConvertToValue(out T value);

		/// <summary>
		/// Converts the string to the target value. Returns null or an empty string if
		/// there were no errors, else an error string that can be shown to the user.
		/// </summary>
		/// <param name="value">Result</param>
		/// <returns></returns>
		public override string ConvertToObjectValue(out object value) {
			var error = ConvertToValue(out var v);
			value = v;
			return error;
		}

		/// <summary>
		/// Validates the data. Returns null or an empty string if there was no error,
		/// or an error string that can be shown to the user.
		/// </summary>
		/// <returns></returns>
		protected override string Validate() {
			try {
				return ConvertToValue(out var value);
			}
			catch (Exception ex) {
				Debug.Fail("Exception caught in Validate(). ConvertToValue() should return an error string instead of throwing for performance reasons! Throwing is SLOOOOW!");
				if (!string.IsNullOrEmpty(ex.Message))
					return ex.Message;
				return string.Format(dnSpy_Contracts_DnSpy_Resources.CouldNotConvert, StringValue);
			}
		}
	}

	/// <summary>
	/// Number base class
	/// </summary>
	/// <typeparam name="T">Real type</typeparam>
	/// <typeparam name="U">If real type is a nullable type, this should be non-nullable type</typeparam>
	public abstract class NumberDataFieldVM<T, U> : DataFieldVM<T> {
		/// <summary>
		/// true to always use decimal, false to never use decimal (except if it's just one digit),
		/// and null to use decimal or hex depending on what number it is.
		/// </summary>
		public bool? UseDecimal {
			get => useDecimal;
			set {
				if (useDecimal != value) {
					useDecimal = value;
					if (!HasError)
						ForceWriteStringValue(OnNewValue(Value));
				}
			}
		}
		bool? useDecimal;

		/// <summary>
		/// Gets/sets the minimum value
		/// </summary>
		public U Min {
			get => min;
			set {
				min = value;
				Revalidate();
			}
		}
		U min;

		/// <summary>
		/// Gets/sets the maximum value
		/// </summary>
		public U Max {
			get => max;
			set {
				max = value;
				Revalidate();
			}
		}
		U max;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="min">Minimum value</param>
		/// <param name="max">Maximum value</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		protected NumberDataFieldVM(Action<DataFieldVM> onUpdated, U min, U max, bool? useDecimal)
			: base(onUpdated) {
			this.min = min;
			this.max = max;
			this.useDecimal = useDecimal;
		}
	}

	/// <summary>
	/// Nullable <see cref="Guid"/>
	/// </summary>
	public class NullableGuidVM : DataFieldVM<Guid?> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public NullableGuidVM(Action<DataFieldVM> onUpdated)
			: this(null, onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public NullableGuidVM(Guid? value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(Guid? value) => value == null ? string.Empty : value.Value.ToString();

		/// <inheritdoc/>
		protected override string ConvertToValue(out Guid? value) {
			string error = null;
			if (IsNull)
				value = null;
			else
				value = ParseGuid(StringValue, out error);
			return error;
		}

		internal static Guid ParseGuid(string s, out string error) {
			if (Guid.TryParse(s, out var res)) {
				error = null;
				return res;
			}

			error = dnSpy_Contracts_DnSpy_Resources.InvalidGuid;
			return Guid.Empty;
		}
	}

	/// <summary>
	/// Hex string
	/// </summary>
	public class HexStringVM : DataFieldVM<IList<byte>> {
		/// <summary>
		/// Gets/sets whether to use upper case hex digits
		/// </summary>
		public bool UpperCaseHex {
			get => upperCaseHex;
			set => upperCaseHex = value;
		}
		bool upperCaseHex = true;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public HexStringVM(Action<DataFieldVM> onUpdated)
			: this(null, onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public HexStringVM(IList<byte> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(IList<byte> value) => SimpleTypeConverter.ByteArrayToString(value, UpperCaseHex);

		/// <inheritdoc/>
		protected override string ConvertToValue(out IList<byte> value) {
			value = SimpleTypeConverter.ParseByteArray(StringValue, out string error);
			return error;
		}
	}

	/// <summary>
	/// Nullable <see cref="bool"/>
	/// </summary>
	public class NullableBooleanVM : DataFieldVM<bool?> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public NullableBooleanVM(Action<DataFieldVM> onUpdated)
			: this(false, onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public NullableBooleanVM(bool? value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(bool? value) => value == null ? string.Empty : SimpleTypeConverter.ToString(value.Value);

		/// <inheritdoc/>
		protected override string ConvertToValue(out bool? value) {
			string error = null;
			if (IsNull)
				value = null;
			else
				value = SimpleTypeConverter.ParseBoolean(StringValue, out error);
			return error;
		}
	}

	/// <summary>
	/// Nullable <see cref="sbyte"/>
	/// </summary>
	public class NullableSByteVM : NumberDataFieldVM<sbyte?, sbyte> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableSByteVM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(null, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableSByteVM(sbyte? value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, sbyte.MinValue, sbyte.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(sbyte? value) => value == null ? string.Empty : SimpleTypeConverter.ToString(value.Value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out sbyte? value) {
			string error = null;
			if (IsNull)
				value = null;
			else
				value = SimpleTypeConverter.ParseSByte(StringValue, Min, Max, out error);
			return error;
		}
	}

	/// <summary>
	/// Nullable <see cref="byte"/>
	/// </summary>
	public class NullableByteVM : NumberDataFieldVM<byte?, byte> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableByteVM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(null, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableByteVM(byte? value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, byte.MinValue, byte.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(byte? value) => value == null ? string.Empty : SimpleTypeConverter.ToString(value.Value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out byte? value) {
			string error = null;
			if (IsNull)
				value = null;
			else
				value = SimpleTypeConverter.ParseByte(StringValue, Min, Max, out error);
			return error;
		}
	}

	/// <summary>
	/// Nullable <see cref="short"/>
	/// </summary>
	public class NullableInt16VM : NumberDataFieldVM<short?, short> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableInt16VM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(null, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableInt16VM(short? value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, short.MinValue, short.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(short? value) => value == null ? string.Empty : SimpleTypeConverter.ToString(value.Value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out short? value) {
			string error = null;
			if (IsNull)
				value = null;
			else
				value = SimpleTypeConverter.ParseInt16(StringValue, Min, Max, out error);
			return error;
		}
	}

	/// <summary>
	/// Nullable <see cref="ushort"/>
	/// </summary>
	public class NullableUInt16VM : NumberDataFieldVM<ushort?, ushort> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableUInt16VM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(null, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableUInt16VM(ushort? value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, ushort.MinValue, ushort.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(ushort? value) => value == null ? string.Empty : SimpleTypeConverter.ToString(value.Value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out ushort? value) {
			string error = null;
			if (IsNull)
				value = null;
			else
				value = SimpleTypeConverter.ParseUInt16(StringValue, Min, Max, out error);
			return error;
		}
	}

	/// <summary>
	/// Nullable <see cref="int"/>
	/// </summary>
	public class NullableInt32VM : NumberDataFieldVM<int?, int> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableInt32VM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(null, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableInt32VM(int? value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, int.MinValue, int.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(int? value) => value == null ? string.Empty : SimpleTypeConverter.ToString(value.Value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out int? value) {
			string error = null;
			if (IsNull)
				value = null;
			else
				value = SimpleTypeConverter.ParseInt32(StringValue, Min, Max, out error);
			return error;
		}
	}

	/// <summary>
	/// Nullable <see cref="uint"/>
	/// </summary>
	public class NullableUInt32VM : NumberDataFieldVM<uint?, uint> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableUInt32VM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(null, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableUInt32VM(uint? value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, uint.MinValue, uint.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(uint? value) => value == null ? string.Empty : SimpleTypeConverter.ToString(value.Value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out uint? value) {
			string error = null;
			if (IsNull)
				value = null;
			else
				value = SimpleTypeConverter.ParseUInt32(StringValue, Min, Max, out error);
			return error;
		}
	}

	/// <summary>
	/// Nullable <see cref="long"/>
	/// </summary>
	public class NullableInt64VM : NumberDataFieldVM<long?, long> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableInt64VM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(null, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableInt64VM(long? value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, long.MinValue, long.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(long? value) => value == null ? string.Empty : SimpleTypeConverter.ToString(value.Value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out long? value) {
			string error = null;
			if (IsNull)
				value = null;
			else
				value = SimpleTypeConverter.ParseInt64(StringValue, Min, Max, out error);
			return error;
		}
	}

	/// <summary>
	/// Nullable <see cref="ulong"/>
	/// </summary>
	public class NullableUInt64VM : NumberDataFieldVM<ulong?, ulong> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableUInt64VM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(null, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public NullableUInt64VM(ulong? value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, ulong.MinValue, ulong.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(ulong? value) => value == null ? string.Empty : SimpleTypeConverter.ToString(value.Value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out ulong? value) {
			string error = null;
			if (IsNull)
				value = null;
			else
				value = SimpleTypeConverter.ParseUInt64(StringValue, Min, Max, out error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="bool"/>
	/// </summary>
	public class BooleanVM : DataFieldVM<bool> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public BooleanVM(Action<DataFieldVM> onUpdated)
			: this(false, onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public BooleanVM(bool value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(bool value) => SimpleTypeConverter.ToString(value);

		/// <inheritdoc/>
		protected override string ConvertToValue(out bool value) {
			value = SimpleTypeConverter.ParseBoolean(StringValue, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="char"/>
	/// </summary>
	public class CharVM : DataFieldVM<char> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public CharVM(Action<DataFieldVM> onUpdated)
			: this((char)0, onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public CharVM(char value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(char value) => SimpleTypeConverter.ToString(value);

		/// <inheritdoc/>
		protected override string ConvertToValue(out char value) {
			value = SimpleTypeConverter.ParseChar(StringValue, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="byte"/>
	/// </summary>
	public class ByteVM : NumberDataFieldVM<byte, byte> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public ByteVM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(0, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public ByteVM(byte value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, byte.MinValue, byte.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(byte value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out byte value) {
			value = SimpleTypeConverter.ParseByte(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="ushort"/>
	/// </summary>
	public class UInt16VM : NumberDataFieldVM<ushort, ushort> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public UInt16VM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(0, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public UInt16VM(ushort value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, ushort.MinValue, ushort.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(ushort value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out ushort value) {
			value = SimpleTypeConverter.ParseUInt16(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="uint"/>
	/// </summary>
	public class UInt32VM : NumberDataFieldVM<uint, uint> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public UInt32VM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(0, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public UInt32VM(uint value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, uint.MinValue, uint.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(uint value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out uint value) {
			value = SimpleTypeConverter.ParseUInt32(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="ulong"/>
	/// </summary>
	public class UInt64VM : NumberDataFieldVM<ulong, ulong> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public UInt64VM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(0, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public UInt64VM(ulong value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, ulong.MinValue, ulong.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(ulong value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out ulong value) {
			value = SimpleTypeConverter.ParseUInt64(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="sbyte"/>
	/// </summary>
	public class SByteVM : NumberDataFieldVM<sbyte, sbyte> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public SByteVM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(0, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public SByteVM(sbyte value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, sbyte.MinValue, sbyte.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(sbyte value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out sbyte value) {
			value = SimpleTypeConverter.ParseSByte(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="short"/>
	/// </summary>
	public class Int16VM : NumberDataFieldVM<short, short> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public Int16VM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(0, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public Int16VM(short value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, short.MinValue, short.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(short value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out short value) {
			value = SimpleTypeConverter.ParseInt16(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="int"/>
	/// </summary>
	public class Int32VM : NumberDataFieldVM<int, int> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public Int32VM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(0, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public Int32VM(int value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, int.MinValue, int.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(int value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out int value) {
			value = SimpleTypeConverter.ParseInt32(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="long"/>
	/// </summary>
	public class Int64VM : NumberDataFieldVM<long, long> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public Int64VM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(0, onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public Int64VM(long value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, long.MinValue, long.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(long value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out long value) {
			value = SimpleTypeConverter.ParseInt64(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="float"/>
	/// </summary>
	public class SingleVM : DataFieldVM<float> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public SingleVM(Action<DataFieldVM> onUpdated)
			: this(0, onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public SingleVM(float value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(float value) => SimpleTypeConverter.ToString(value);

		/// <inheritdoc/>
		protected override string ConvertToValue(out float value) {
			value = SimpleTypeConverter.ParseSingle(StringValue, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="double"/>
	/// </summary>
	public class DoubleVM : DataFieldVM<double> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public DoubleVM(Action<DataFieldVM> onUpdated)
			: this(0, onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public DoubleVM(double value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(double value) => SimpleTypeConverter.ToString(value);

		/// <inheritdoc/>
		protected override string ConvertToValue(out double value) {
			value = SimpleTypeConverter.ParseDouble(StringValue, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="string"/>
	/// </summary>
	public class StringVM : DataFieldVM<string> {
		readonly bool allowNullString;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="allowNullString">true to allow null strings</param>
		public StringVM(Action<DataFieldVM> onUpdated, bool allowNullString = false)
			: this(string.Empty, onUpdated, allowNullString) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="allowNullString">true to allow null strings</param>
		public StringVM(string value, Action<DataFieldVM> onUpdated, bool allowNullString = false)
			: base(onUpdated) {
			this.allowNullString = allowNullString;
			SetValueFromConstructor(value);
		}

		/// <inheritdoc/>
		protected override string OnNewValue(string value) => SimpleTypeConverter.ToString(value, allowNullString);

		/// <inheritdoc/>
		protected override string ConvertToValue(out string value) {
			value = SimpleTypeConverter.ParseString(StringValue, allowNullString, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="decimal"/>
	/// </summary>
	public class DecimalVM : DataFieldVM<decimal> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public DecimalVM(Action<DataFieldVM> onUpdated)
			: this(0, onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public DecimalVM(decimal value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(decimal value) => SimpleTypeConverter.ToString(value);

		/// <inheritdoc/>
		protected override string ConvertToValue(out decimal value) {
			value = SimpleTypeConverter.ParseDecimal(StringValue, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="DateTime"/>
	/// </summary>
	public class DateTimeVM : DataFieldVM<DateTime> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public DateTimeVM(Action<DataFieldVM> onUpdated)
			: this(DateTime.Now, onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public DateTimeVM(DateTime value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(DateTime value) => SimpleTypeConverter.ToString(value);

		/// <inheritdoc/>
		protected override string ConvertToValue(out DateTime value) {
			value = SimpleTypeConverter.ParseDateTime(StringValue, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="TimeSpan"/>
	/// </summary>
	public class TimeSpanVM : DataFieldVM<TimeSpan> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public TimeSpanVM(Action<DataFieldVM> onUpdated)
			: this(TimeSpan.Zero, onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public TimeSpanVM(TimeSpan value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(TimeSpan value) => SimpleTypeConverter.ToString(value);

		/// <inheritdoc/>
		protected override string ConvertToValue(out TimeSpan value) {
			value = SimpleTypeConverter.ParseTimeSpan(StringValue, out string error);
			return error;
		}
	}

	/// <summary>
	/// <see cref="Guid"/>
	/// </summary>
	public class GuidVM : DataFieldVM<Guid> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public GuidVM(Action<DataFieldVM> onUpdated)
			: this(new Guid(), onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public GuidVM(Guid value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(Guid value) => value.ToString();

		/// <inheritdoc/>
		protected override string ConvertToValue(out Guid value) {
			value = NullableGuidVM.ParseGuid(StringValue, out string error);
			return error;
		}
	}

	/// <summary>
	/// List of <see cref="bool"/>s
	/// </summary>
	public class BooleanListDataFieldVM : DataFieldVM<IList<bool>> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public BooleanListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(Array.Empty<bool>(), onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public BooleanListDataFieldVM(IList<bool> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(IList<bool> value) => SimpleTypeConverter.ToString(value);

		/// <inheritdoc/>
		protected override string ConvertToValue(out IList<bool> value) {
			value = SimpleTypeConverter.ParseBooleanList(StringValue, out string error);
			return error;
		}
	}

	/// <summary>
	/// List of <see cref="char"/>s
	/// </summary>
	public class CharListDataFieldVM : DataFieldVM<IList<char>> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public CharListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(Array.Empty<char>(), onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public CharListDataFieldVM(IList<char> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(IList<char> value) => SimpleTypeConverter.ToString(value);

		/// <inheritdoc/>
		protected override string ConvertToValue(out IList<char> value) {
			value = SimpleTypeConverter.ParseCharList(StringValue, out string error);
			return error;
		}
	}

	/// <summary>
	/// List of <see cref="byte"/>s
	/// </summary>
	public class ByteListDataFieldVM : NumberDataFieldVM<IList<byte>, byte> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public ByteListDataFieldVM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(Array.Empty<byte>(), onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public ByteListDataFieldVM(IList<byte> value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, byte.MinValue, byte.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(IList<byte> value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out IList<byte> value) {
			value = SimpleTypeConverter.ParseByteList(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// List of <see cref="ushort"/>s
	/// </summary>
	public class UInt16ListDataFieldVM : NumberDataFieldVM<IList<ushort>, ushort> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public UInt16ListDataFieldVM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(Array.Empty<ushort>(), onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public UInt16ListDataFieldVM(IList<ushort> value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, ushort.MinValue, ushort.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(IList<ushort> value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out IList<ushort> value) {
			value = SimpleTypeConverter.ParseUInt16List(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// List of <see cref="uint"/>s
	/// </summary>
	public class UInt32ListDataFieldVM : NumberDataFieldVM<IList<uint>, uint> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public UInt32ListDataFieldVM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(Array.Empty<uint>(), onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public UInt32ListDataFieldVM(IList<uint> value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, uint.MinValue, uint.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(IList<uint> value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out IList<uint> value) {
			value = SimpleTypeConverter.ParseUInt32List(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// List of <see cref="ulong"/>s
	/// </summary>
	public class UInt64ListDataFieldVM : NumberDataFieldVM<IList<ulong>, ulong> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public UInt64ListDataFieldVM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(Array.Empty<ulong>(), onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public UInt64ListDataFieldVM(IList<ulong> value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, ulong.MinValue, ulong.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(IList<ulong> value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out IList<ulong> value) {
			value = SimpleTypeConverter.ParseUInt64List(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// List of <see cref="sbyte"/>s
	/// </summary>
	public class SByteListDataFieldVM : NumberDataFieldVM<IList<sbyte>, sbyte> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public SByteListDataFieldVM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(Array.Empty<sbyte>(), onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public SByteListDataFieldVM(IList<sbyte> value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, sbyte.MinValue, sbyte.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(IList<sbyte> value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out IList<sbyte> value) {
			value = SimpleTypeConverter.ParseSByteList(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// List of <see cref="short"/>s
	/// </summary>
	public class Int16ListDataFieldVM : NumberDataFieldVM<IList<short>, short> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public Int16ListDataFieldVM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(Array.Empty<short>(), onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public Int16ListDataFieldVM(IList<short> value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, short.MinValue, short.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(IList<short> value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out IList<short> value) {
			value = SimpleTypeConverter.ParseInt16List(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// List of <see cref="int"/>s
	/// </summary>
	public class Int32ListDataFieldVM : NumberDataFieldVM<IList<int>, int> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public Int32ListDataFieldVM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(Array.Empty<int>(), onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public Int32ListDataFieldVM(IList<int> value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, int.MinValue, int.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(IList<int> value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out IList<int> value) {
			value = SimpleTypeConverter.ParseInt32List(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// List of <see cref="long"/>s
	/// </summary>
	public class Int64ListDataFieldVM : NumberDataFieldVM<IList<long>, long> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public Int64ListDataFieldVM(Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: this(Array.Empty<long>(), onUpdated, useDecimal) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="useDecimal">true to use decimal, false to use hex, or null if it depends on the value</param>
		public Int64ListDataFieldVM(IList<long> value, Action<DataFieldVM> onUpdated, bool? useDecimal = null)
			: base(onUpdated, long.MinValue, long.MaxValue, useDecimal) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(IList<long> value) => SimpleTypeConverter.ToString(value, Min, Max, UseDecimal);

		/// <inheritdoc/>
		protected override string ConvertToValue(out IList<long> value) {
			value = SimpleTypeConverter.ParseInt64List(StringValue, Min, Max, out string error);
			return error;
		}
	}

	/// <summary>
	/// List of <see cref="float"/>s
	/// </summary>
	public class SingleListDataFieldVM : DataFieldVM<IList<float>> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public SingleListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(Array.Empty<float>(), onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public SingleListDataFieldVM(IList<float> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(IList<float> value) => SimpleTypeConverter.ToString(value);

		/// <inheritdoc/>
		protected override string ConvertToValue(out IList<float> value) {
			value = SimpleTypeConverter.ParseSingleList(StringValue, out string error);
			return error;
		}
	}

	/// <summary>
	/// List of <see cref="double"/>s
	/// </summary>
	public class DoubleListDataFieldVM : DataFieldVM<IList<double>> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public DoubleListDataFieldVM(Action<DataFieldVM> onUpdated)
			: this(Array.Empty<double>(), onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public DoubleListDataFieldVM(IList<double> value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(IList<double> value) => SimpleTypeConverter.ToString(value);

		/// <inheritdoc/>
		protected override string ConvertToValue(out IList<double> value) {
			value = SimpleTypeConverter.ParseDoubleList(StringValue, out string error);
			return error;
		}
	}

	/// <summary>
	/// List of <see cref="string"/>s
	/// </summary>
	public class StringListDataFieldVM : DataFieldVM<IList<string>> {
		readonly bool allowNullString;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="allowNullString">true to allow null strings</param>
		public StringListDataFieldVM(Action<DataFieldVM> onUpdated, bool allowNullString = true)
			: this(Array.Empty<string>(), onUpdated, allowNullString) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		/// <param name="allowNullString">true to allow null strings</param>
		public StringListDataFieldVM(IList<string> value, Action<DataFieldVM> onUpdated, bool allowNullString = true)
			: base(onUpdated) {
			this.allowNullString = allowNullString;
			SetValueFromConstructor(value);
		}

		/// <inheritdoc/>
		protected override string OnNewValue(IList<string> value) => SimpleTypeConverter.ToString(value, allowNullString);

		/// <inheritdoc/>
		protected override string ConvertToValue(out IList<string> value) {
			value = SimpleTypeConverter.ParseStringList(StringValue, allowNullString, out string error);
			return error;
		}
	}

	/// <summary>
	/// Uses the default converter to convert the type to/from a string
	/// </summary>
	/// <typeparam name="T">Type</typeparam>
	public class DefaultConverterVM<T> : DataFieldVM<T> {
		static readonly TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));

		static DefaultConverterVM() {
			if (!converter.CanConvertTo(null, typeof(string)))
				throw new InvalidOperationException($"Converter can't convert a {typeof(T)} to a string");
			if (!converter.CanConvertFrom(null, typeof(string)))
				throw new InvalidOperationException($"Converter can't convert a string to a {typeof(T)}");
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public DefaultConverterVM(Action<DataFieldVM> onUpdated)
			: this(default, onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public DefaultConverterVM(T value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) => SetValueFromConstructor(value);

		/// <inheritdoc/>
		protected override string OnNewValue(T value) => (string)converter.ConvertTo(null, CultureInfo.InvariantCulture, value, typeof(string));

		/// <inheritdoc/>
		protected override string ConvertToValue(out T value) {
			string error;
			try {
				value = (T)converter.ConvertFrom(null, CultureInfo.InvariantCulture, StringValue);
				error = string.Empty;
			}
			catch (Exception ex) {
				value = default;
				error = string.Format(dnSpy_Contracts_DnSpy_Resources.ValueMustBeType, typeof(T).FullName, ex.Message);
			}
			return error;
		}
	}
}
