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
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using ICSharpCode.ILSpy.AsmEditor.ViewHelpers;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	enum ConstantType
	{
		Null,

		Object,	// Can only be used by CANamedArgumentVM
		Boolean,
		Char,
		SByte,
		Byte,
		Int16,
		UInt16,
		Int32,
		UInt32,
		Int64,
		UInt64,
		Single,
		Double,
		String,
		Enum,
		Type,

		ObjectArray,
		BooleanArray,
		CharArray,
		SByteArray,
		ByteArray,
		Int16Array,
		UInt16Array,
		Int32Array,
		UInt32Array,
		Int64Array,
		UInt64Array,
		SingleArray,
		DoubleArray,
		StringArray,
		EnumArray,
		TypeArray,
	}

	sealed class ConstantTypeVM : ViewModelBase
	{
		public IDnlibTypePicker DnlibTypePicker {
			set {
				@enum.DnlibTypePicker = value;
				enumArray.DnlibTypePicker = value;
			}
		}

		public ITypeSigCreator TypeSigCreator {
			set {
				type.TypeSigCreator = value;
				typeArray.TypeSigCreator = value;
			}
		}

		public ICreateConstantType CreateConstantType {
			set { objectArray.CreateConstantType = value; }
		}

		public EnumListVM ConstantTypeEnumList {
			get { return constantTypeEnumListVM; }
		}
		readonly EnumListVM constantTypeEnumListVM;

		static readonly Dictionary<ConstantType, EnumVM> typeToEnumVM = new Dictionary<ConstantType, EnumVM>() {
			{ ConstantType.Null,		new EnumVM(ConstantType.Null,			"null") },
			{ ConstantType.Object,		new EnumVM(ConstantType.Object,			"Object") },
			{ ConstantType.Boolean,		new EnumVM(ConstantType.Boolean,		"Boolean") },
			{ ConstantType.Char,		new EnumVM(ConstantType.Char,			"Char") },
			{ ConstantType.SByte,		new EnumVM(ConstantType.SByte,			"SByte") },
			{ ConstantType.Int16,		new EnumVM(ConstantType.Int16,			"Int16") },
			{ ConstantType.Int32,		new EnumVM(ConstantType.Int32,			"Int32") },
			{ ConstantType.Int64,		new EnumVM(ConstantType.Int64,			"Int64") },
			{ ConstantType.Byte,		new EnumVM(ConstantType.Byte,			"Byte") },
			{ ConstantType.UInt16,		new EnumVM(ConstantType.UInt16,			"UInt16") },
			{ ConstantType.UInt32,		new EnumVM(ConstantType.UInt32,			"UInt32") },
			{ ConstantType.UInt64,		new EnumVM(ConstantType.UInt64,			"UInt64") },
			{ ConstantType.Single,		new EnumVM(ConstantType.Single,			"Single") },
			{ ConstantType.Double,		new EnumVM(ConstantType.Double,			"Double") },
			{ ConstantType.String,		new EnumVM(ConstantType.String,			"String") },
			{ ConstantType.Enum,		new EnumVM(ConstantType.Enum,			"Enum") },
			{ ConstantType.Type,		new EnumVM(ConstantType.Type,			"Type") },
			{ ConstantType.ObjectArray,	new EnumVM(ConstantType.ObjectArray,	"Object[]") },
			{ ConstantType.BooleanArray,new EnumVM(ConstantType.BooleanArray,	"Boolean[]") },
			{ ConstantType.CharArray,	new EnumVM(ConstantType.CharArray,		"Char[]") },
			{ ConstantType.SByteArray,	new EnumVM(ConstantType.SByteArray,		"SByte[]") },
			{ ConstantType.Int16Array,	new EnumVM(ConstantType.Int16Array,		"Int16[]") },
			{ ConstantType.Int32Array,	new EnumVM(ConstantType.Int32Array,		"Int32[]") },
			{ ConstantType.Int64Array,	new EnumVM(ConstantType.Int64Array,		"Int64[]") },
			{ ConstantType.ByteArray,	new EnumVM(ConstantType.ByteArray,		"Byte[]") },
			{ ConstantType.UInt16Array,	new EnumVM(ConstantType.UInt16Array,	"UInt16[]") },
			{ ConstantType.UInt32Array,	new EnumVM(ConstantType.UInt32Array,	"UInt32[]") },
			{ ConstantType.UInt64Array,	new EnumVM(ConstantType.UInt64Array,	"UInt64[]") },
			{ ConstantType.SingleArray,	new EnumVM(ConstantType.SingleArray,	"Single[]") },
			{ ConstantType.DoubleArray,	new EnumVM(ConstantType.DoubleArray,	"Double[]") },
			{ ConstantType.StringArray,	new EnumVM(ConstantType.StringArray,	"String[]") },
			{ ConstantType.EnumArray,	new EnumVM(ConstantType.EnumArray,		"Enum[]") },
			{ ConstantType.TypeArray,	new EnumVM(ConstantType.TypeArray,		"Type[]") },
		};

		public static EnumVM[] CreateEnumArray(IEnumerable<ConstantType> constants)
		{
			return constants.Select(a => typeToEnumVM[a]).ToArray();
		}

		public bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					isEnabled = value;
					OnPropertyChanged("IsEnabled");
					if (!isEnabled)
						Value = null;
					HasErrorUpdated();
				}
			}
		}
		bool isEnabled = true;

		public ConstantType ValueType {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem; }
		}

		public object ValueNoSpecialNull {
			get { return ConvertValueNoSpecialNull(Value); }
		}

		public static object ConvertValueNoSpecialNull(object value)
		{
			if (value == null)
				return value;
			if (value is Null)
				return null;
			if (value.GetType() == typeof(List<object>) || value.GetType() == typeof(object[]))
				return ((IList<object>)value).Select(a => ConvertValueNoSpecialNull(a)).ToArray();
			return value;
		}

		public object Value {
			get {
				switch ((ConstantType)ConstantTypeEnumList.SelectedItem) {
				case ConstantType.Null:			return null;
				case ConstantType.Boolean:		return Boolean.Value;
				case ConstantType.Char:			return Char.Value;
				case ConstantType.SByte:		return SByte.Value;
				case ConstantType.Int16:		return Int16.Value;
				case ConstantType.Int32:		return Int32.Value;
				case ConstantType.Int64:		return Int64.Value;
				case ConstantType.Byte:			return Byte.Value;
				case ConstantType.UInt16:		return UInt16.Value;
				case ConstantType.UInt32:		return UInt32.Value;
				case ConstantType.UInt64:		return UInt64.Value;
				case ConstantType.Single:		return Single.Value;
				case ConstantType.Double:		return Double.Value;
				case ConstantType.String:		return (object)String.Value ?? Null<string>.Instance;
				case ConstantType.Enum:			return Enum.Value;
				case ConstantType.Type:			return (object)Type.Value ?? Null<TypeSig>.Instance;
				case ConstantType.ObjectArray:	return ArraysCanBeNull && ObjectArrayIsNull	? (object)Null<object[]>.Instance : ObjectArray.Value;
				case ConstantType.BooleanArray:	return ArraysCanBeNull && BooleanArrayIsNull? (object)Null<bool[]>.Instance : BooleanArray.Value;
				case ConstantType.CharArray:	return ArraysCanBeNull && CharArrayIsNull	? (object)Null<char[]>.Instance : CharArray.Value;
				case ConstantType.SByteArray:	return ArraysCanBeNull && SByteArrayIsNull	? (object)Null<sbyte[]>.Instance : SByteArray.Value;
				case ConstantType.Int16Array:	return ArraysCanBeNull && Int16ArrayIsNull	? (object)Null<short[]>.Instance : Int16Array.Value;
				case ConstantType.Int32Array:	return ArraysCanBeNull && Int32ArrayIsNull	? (object)Null<int[]>.Instance : Int32Array.Value;
				case ConstantType.Int64Array:	return ArraysCanBeNull && Int64ArrayIsNull	? (object)Null<long[]>.Instance : Int64Array.Value;
				case ConstantType.ByteArray:	return ArraysCanBeNull && ByteArrayIsNull	? (object)Null<byte[]>.Instance : ByteArray.Value;
				case ConstantType.UInt16Array:	return ArraysCanBeNull && UInt16ArrayIsNull	? (object)Null<ushort[]>.Instance : UInt16Array.Value;
				case ConstantType.UInt32Array:	return ArraysCanBeNull && UInt32ArrayIsNull	? (object)Null<uint[]>.Instance : UInt32Array.Value;
				case ConstantType.UInt64Array:	return ArraysCanBeNull && UInt64ArrayIsNull	? (object)Null<ulong[]>.Instance : UInt64Array.Value;
				case ConstantType.SingleArray:	return ArraysCanBeNull && SingleArrayIsNull	? (object)Null<float[]>.Instance : SingleArray.Value;
				case ConstantType.DoubleArray:	return ArraysCanBeNull && DoubleArrayIsNull	? (object)Null<double[]>.Instance : DoubleArray.Value;
				case ConstantType.StringArray:	return ArraysCanBeNull && StringArrayIsNull	? (object)Null<string[]>.Instance : StringArray.Value;
				case ConstantType.EnumArray:	return ArraysCanBeNull && EnumArrayIsNull	? EnumArray.NullValue : EnumArray.Value;
				case ConstantType.TypeArray:	return ArraysCanBeNull && TypeArrayIsNull	? (object)Null<Type[]>.Instance : TypeArray.Value;
				default: throw new InvalidOperationException();
				}
			}
			set {
				var valueType = value == null ? null : value.GetType();
				if (value is bool) {
					SetSelectedItem(ConstantType.Boolean);
					Boolean.Value = (bool)value;
				}
				else if (value is char) {
					SetSelectedItem(ConstantType.Char);
					Char.Value = (char)value;
				}
				else if (value is sbyte) {
					SetSelectedItem(ConstantType.SByte);
					SByte.Value = (sbyte)value;
				}
				else if (value is short) {
					SetSelectedItem(ConstantType.Int16);
					Int16.Value = (short)value;
				}
				else if (value is int) {
					SetSelectedItem(ConstantType.Int32);
					Int32.Value = (int)value;
				}
				else if (value is long) {
					SetSelectedItem(ConstantType.Int64);
					Int64.Value = (long)value;
				}
				else if (value is byte) {
					SetSelectedItem(ConstantType.Byte);
					Byte.Value = (byte)value;
				}
				else if (value is ushort) {
					SetSelectedItem(ConstantType.UInt16);
					UInt16.Value = (ushort)value;
				}
				else if (value is uint) {
					SetSelectedItem(ConstantType.UInt32);
					UInt32.Value = (uint)value;
				}
				else if (value is ulong) {
					SetSelectedItem(ConstantType.UInt64);
					UInt64.Value = (ulong)value;
				}
				else if (value is float) {
					SetSelectedItem(ConstantType.Single);
					Single.Value = (float)value;
				}
				else if (value is double) {
					SetSelectedItem(ConstantType.Double);
					Double.Value = (double)value;
				}
				else if (value is string) {
					SetSelectedItem(ConstantType.String);
					String.Value = (string)value;
				}
				else if (value == Null<string>.Instance) {
					SetSelectedItem(ConstantType.String);
					String.Value = null;
				}
				else if (value is EnumInfo) {
					var enumInfo = (EnumInfo)value;
					if (enumInfo.IsArray) {
						Debug.Assert(enumInfo.Value == null || enumInfo.Value is System.Collections.IList);
						SetSelectedItem(ConstantType.EnumArray);
						EnumArray.Value = enumInfo;
						if (ArraysCanBeNull && enumInfo.Value == null) EnumArrayIsNull = true;
					}
					else {
						Debug.Assert(enumInfo.Value != null && !(enumInfo.Value is System.Collections.IList));
						SetSelectedItem(ConstantType.Enum);
						Enum.Value = enumInfo;
					}
				}
				else if (value is TypeSig) {
					SetSelectedItem(ConstantType.Type);
					Type.Value = (TypeSig)value;
				}
				else if (value == Null<TypeSig>.Instance) {
					SetSelectedItem(ConstantType.Type);
					Type.Value = null;
				}
				else if (value is IList<bool>) {
					SetSelectedItem(ConstantType.BooleanArray);
					BooleanArray.Value = (IList<bool>)value;
				}
				else if (value == Null<bool[]>.Instance) {
					SetSelectedItem(ConstantType.BooleanArray);
					BooleanArray.Value = null;
					if (ArraysCanBeNull) BooleanArrayIsNull = true;
				}
				else if (value is IList<char>) {
					SetSelectedItem(ConstantType.CharArray);
					CharArray.Value = (IList<char>)value;
				}
				else if (value == Null<char[]>.Instance) {
					SetSelectedItem(ConstantType.CharArray);
					CharArray.Value = null;
					if (ArraysCanBeNull) CharArrayIsNull = true;
				}
				else if (value is IList<sbyte> && valueType != typeof(byte[])) {
					SetSelectedItem(ConstantType.SByteArray);
					SByteArray.Value = (IList<sbyte>)value;
				}
				else if (value == Null<sbyte[]>.Instance) {
					SetSelectedItem(ConstantType.SByteArray);
					SByteArray.Value = null;
					if (ArraysCanBeNull) SByteArrayIsNull = true;
				}
				else if (value is IList<short> && valueType != typeof(ushort[])) {
					SetSelectedItem(ConstantType.Int16Array);
					Int16Array.Value = (IList<short>)value;
				}
				else if (value == Null<short[]>.Instance) {
					SetSelectedItem(ConstantType.Int16Array);
					Int16Array.Value = null;
					if (ArraysCanBeNull) Int16ArrayIsNull = true;
				}
				else if (value is IList<int> && valueType != typeof(uint[])) {
					SetSelectedItem(ConstantType.Int32Array);
					Int32Array.Value = (IList<int>)value;
				}
				else if (value == Null<int[]>.Instance) {
					SetSelectedItem(ConstantType.Int32Array);
					Int32Array.Value = null;
					if (ArraysCanBeNull) Int32ArrayIsNull = true;
				}
				else if (value is IList<long> && valueType != typeof(ulong[])) {
					SetSelectedItem(ConstantType.Int64Array);
					Int64Array.Value = (IList<long>)value;
				}
				else if (value == Null<long[]>.Instance) {
					SetSelectedItem(ConstantType.Int64Array);
					Int64Array.Value = null;
					if (ArraysCanBeNull) Int64ArrayIsNull = true;
				}
				else if (value is IList<byte> && valueType != typeof(sbyte[])) {
					SetSelectedItem(ConstantType.ByteArray);
					ByteArray.Value = (IList<byte>)value;
				}
				else if (value == Null<byte[]>.Instance) {
					SetSelectedItem(ConstantType.ByteArray);
					ByteArray.Value = null;
					if (ArraysCanBeNull) ByteArrayIsNull = true;
				}
				else if (value is IList<ushort> && valueType != typeof(short[])) {
					SetSelectedItem(ConstantType.UInt16Array);
					UInt16Array.Value = (IList<ushort>)value;
				}
				else if (value == Null<ushort[]>.Instance) {
					SetSelectedItem(ConstantType.UInt16Array);
					UInt16Array.Value = null;
					if (ArraysCanBeNull) UInt16ArrayIsNull = true;
				}
				else if (value is IList<uint> && valueType != typeof(int[])) {
					SetSelectedItem(ConstantType.UInt32Array);
					UInt32Array.Value = (IList<uint>)value;
				}
				else if (value == Null<uint[]>.Instance) {
					SetSelectedItem(ConstantType.UInt32Array);
					UInt32Array.Value = null;
					if (ArraysCanBeNull) UInt32ArrayIsNull = true;
				}
				else if (value is IList<ulong> && valueType != typeof(long[])) {
					SetSelectedItem(ConstantType.UInt64Array);
					UInt64Array.Value = (IList<ulong>)value;
				}
				else if (value == Null<ulong[]>.Instance) {
					SetSelectedItem(ConstantType.UInt64Array);
					UInt64Array.Value = null;
					if (ArraysCanBeNull) UInt64ArrayIsNull = true;
				}
				else if (value is IList<float>) {
					SetSelectedItem(ConstantType.SingleArray);
					SingleArray.Value = (IList<float>)value;
				}
				else if (value == Null<float[]>.Instance) {
					SetSelectedItem(ConstantType.SingleArray);
					SingleArray.Value = null;
					if (ArraysCanBeNull) SingleArrayIsNull = true;
				}
				else if (value is IList<double>) {
					SetSelectedItem(ConstantType.DoubleArray);
					DoubleArray.Value = (IList<double>)value;
				}
				else if (value == Null<double[]>.Instance) {
					SetSelectedItem(ConstantType.DoubleArray);
					DoubleArray.Value = null;
					if (ArraysCanBeNull) DoubleArrayIsNull = true;
				}
				else if (value is IList<string>) {
					SetSelectedItem(ConstantType.StringArray);
					StringArray.Value = (IList<string>)value;
				}
				else if (value == Null<string[]>.Instance) {
					SetSelectedItem(ConstantType.StringArray);
					StringArray.Value = null;
					if (ArraysCanBeNull) StringArrayIsNull = true;
				}
				else if (value is IList<TypeSig>) {
					SetSelectedItem(ConstantType.TypeArray);
					TypeArray.Value = (IList<TypeSig>)value;
				}
				else if (value == Null<TypeSig[]>.Instance) {
					SetSelectedItem(ConstantType.TypeArray);
					TypeArray.Value = null;
					if (ArraysCanBeNull) TypeArrayIsNull = true;
				}
				else if (value is IList<object>) {
					SetSelectedItem(ConstantType.ObjectArray);
					ObjectArray.Value = (IList<object>)value;
				}
				else if (value == Null<object[]>.Instance) {
					SetSelectedItem(ConstantType.ObjectArray);
					ObjectArray.Value = null;
					if (ArraysCanBeNull) ObjectArrayIsNull = true;
				}
				else {
					SetSelectedItem(ConstantType.Null);
				}
				OnPropertyChanged("Modified");
			}
		}

		void SetSelectedItem(ConstantType ct)
		{
			if (ConstantTypeEnumList.Has(ct))
				ConstantTypeEnumList.SelectedItem = ct;
			else
				ConstantTypeEnumList.SelectedIndex = 0;
		}

		public bool NullIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Null; }
		}

		public bool BooleanIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Boolean; }
		}

		public bool CharIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Char; }
		}

		public bool SByteIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.SByte; }
		}

		public bool Int16IsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Int16; }
		}

		public bool Int32IsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Int32; }
		}

		public bool Int64IsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Int64; }
		}

		public bool ByteIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Byte; }
		}

		public bool UInt16IsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.UInt16; }
		}

		public bool UInt32IsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.UInt32; }
		}

		public bool UInt64IsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.UInt64; }
		}

		public bool SingleIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Single; }
		}

		public bool DoubleIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Double; }
		}

		public bool StringIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.String; }
		}

		public bool EnumIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Enum; }
		}

		public bool TypeIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Type; }
		}

		public bool ObjectArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.ObjectArray; }
		}

		public bool BooleanArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.BooleanArray; }
		}

		public bool CharArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.CharArray; }
		}

		public bool SByteArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.SByteArray; }
		}

		public bool Int16ArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Int16Array; }
		}

		public bool Int32ArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Int32Array; }
		}

		public bool Int64ArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Int64Array; }
		}

		public bool ByteArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.ByteArray; }
		}

		public bool UInt16ArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.UInt16Array; }
		}

		public bool UInt32ArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.UInt32Array; }
		}

		public bool UInt64ArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.UInt64Array; }
		}

		public bool SingleArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.SingleArray; }
		}

		public bool DoubleArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.DoubleArray; }
		}

		public bool StringArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.StringArray; }
		}

		public bool EnumArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.EnumArray; }
		}

		public bool TypeArrayIsSelected {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.TypeArray; }
		}

		public bool ObjectArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.ObjectArray && !ObjectArrayIsNull; }
		}

		public bool BooleanArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.BooleanArray && !BooleanArrayIsNull; }
		}

		public bool CharArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.CharArray && !CharArrayIsNull; }
		}

		public bool SByteArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.SByteArray && !SByteArrayIsNull; }
		}

		public bool Int16ArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Int16Array && !Int16ArrayIsNull; }
		}

		public bool Int32ArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Int32Array && !Int32ArrayIsNull; }
		}

		public bool Int64ArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Int64Array && !Int64ArrayIsNull; }
		}

		public bool ByteArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.ByteArray && !ByteArrayIsNull; }
		}

		public bool UInt16ArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.UInt16Array && !UInt16ArrayIsNull; }
		}

		public bool UInt32ArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.UInt32Array && !UInt32ArrayIsNull; }
		}

		public bool UInt64ArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.UInt64Array && !UInt64ArrayIsNull; }
		}

		public bool SingleArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.SingleArray && !SingleArrayIsNull; }
		}

		public bool DoubleArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.DoubleArray && !DoubleArrayIsNull; }
		}

		public bool StringArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.StringArray && !StringArrayIsNull; }
		}

		public bool EnumArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.EnumArray && !EnumArrayIsNull; }
		}

		public bool TypeArrayIsSelectedAndNotNull {
			get { return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.TypeArray && !TypeArrayIsNull; }
		}

		public BooleanVM Boolean {
			get { return boolean; }
		}
		BooleanVM boolean;

		public CharVM Char {
			get { return @char; }
		}
		CharVM @char;

		public SByteVM SByte {
			get { return @sbyte; }
		}
		SByteVM @sbyte;

		public Int16VM Int16 {
			get { return int16; }
		}
		Int16VM int16;

		public Int32VM Int32 {
			get { return int32; }
		}
		Int32VM int32;

		public Int64VM Int64 {
			get { return int64; }
		}
		Int64VM int64;

		public ByteVM Byte {
			get { return @byte; }
		}
		ByteVM @byte;

		public UInt16VM UInt16 {
			get { return uint16; }
		}
		UInt16VM uint16;

		public UInt32VM UInt32 {
			get { return uint32; }
		}
		UInt32VM uint32;

		public UInt64VM UInt64 {
			get { return uint64; }
		}
		UInt64VM uint64;

		public SingleVM Single {
			get { return single; }
		}
		SingleVM single;

		public DoubleVM Double {
			get { return @double; }
		}
		DoubleVM @double;

		public StringVM String {
			get { return @string; }
		}
		StringVM @string;

		public EnumDataFieldVM Enum {
			get { return @enum; }
		}
		EnumDataFieldVM @enum;

		public TypeSigVM Type {
			get { return type; }
		}
		TypeSigVM type;

		public ObjectListDataFieldVM ObjectArray {
			get { return objectArray; }
		}
		ObjectListDataFieldVM objectArray;

		public BooleanListDataFieldVM BooleanArray {
			get { return booleanArray; }
		}
		BooleanListDataFieldVM booleanArray;

		public CharListDataFieldVM CharArray {
			get { return charArray; }
		}
		CharListDataFieldVM charArray;

		public SByteListDataFieldVM SByteArray {
			get { return sbyteArray; }
		}
		SByteListDataFieldVM sbyteArray;

		public Int16ListDataFieldVM Int16Array {
			get { return int16Array; }
		}
		Int16ListDataFieldVM int16Array;

		public Int32ListDataFieldVM Int32Array {
			get { return int32Array; }
		}
		Int32ListDataFieldVM int32Array;

		public Int64ListDataFieldVM Int64Array {
			get { return int64Array; }
		}
		Int64ListDataFieldVM int64Array;

		public ByteListDataFieldVM ByteArray {
			get { return byteArray; }
		}
		ByteListDataFieldVM byteArray;

		public UInt16ListDataFieldVM UInt16Array {
			get { return uint16Array; }
		}
		UInt16ListDataFieldVM uint16Array;

		public UInt32ListDataFieldVM UInt32Array {
			get { return uint32Array; }
		}
		UInt32ListDataFieldVM uint32Array;

		public UInt64ListDataFieldVM UInt64Array {
			get { return uint64Array; }
		}
		UInt64ListDataFieldVM uint64Array;

		public SingleListDataFieldVM SingleArray {
			get { return singleArray; }
		}
		SingleListDataFieldVM singleArray;

		public DoubleListDataFieldVM DoubleArray {
			get { return doubleArray; }
		}
		DoubleListDataFieldVM doubleArray;

		public StringListDataFieldVM StringArray {
			get { return stringArray; }
		}
		StringListDataFieldVM stringArray;

		public EnumListDataFieldVM EnumArray {
			get { return enumArray; }
		}
		EnumListDataFieldVM enumArray;

		public TypeSigListDataFieldVM TypeArray {
			get { return typeArray; }
		}
		TypeSigListDataFieldVM typeArray;

		public bool ObjectArrayIsNull {
			get { return objectArrayIsNull; }
			set {
				if (objectArrayIsNull != value) {
					objectArrayIsNull = value;
					OnPropertyChanged("ObjectArrayIsNull");
					OnPropertyChanged("ObjectArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool objectArrayIsNull;

		public bool BooleanArrayIsNull {
			get { return booleanArrayIsNull; }
			set {
				if (booleanArrayIsNull != value) {
					booleanArrayIsNull = value;
					OnPropertyChanged("BooleanArrayIsNull");
					OnPropertyChanged("BooleanArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool booleanArrayIsNull;

		public bool CharArrayIsNull {
			get { return charArrayIsNull; }
			set {
				if (charArrayIsNull != value) {
					charArrayIsNull = value;
					OnPropertyChanged("CharArrayIsNull");
					OnPropertyChanged("CharArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool charArrayIsNull;

		public bool SByteArrayIsNull {
			get { return sbyteArrayIsNull; }
			set {
				if (sbyteArrayIsNull != value) {
					sbyteArrayIsNull = value;
					OnPropertyChanged("SByteArrayIsNull");
					OnPropertyChanged("SByteArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool sbyteArrayIsNull;

		public bool Int16ArrayIsNull {
			get { return int16ArrayIsNull; }
			set {
				if (int16ArrayIsNull != value) {
					int16ArrayIsNull = value;
					OnPropertyChanged("Int16ArrayIsNull");
					OnPropertyChanged("Int16ArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool int16ArrayIsNull;

		public bool Int32ArrayIsNull {
			get { return int32ArrayIsNull; }
			set {
				if (int32ArrayIsNull != value) {
					int32ArrayIsNull = value;
					OnPropertyChanged("Int32ArrayIsNull");
					OnPropertyChanged("Int32ArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool int32ArrayIsNull;

		public bool Int64ArrayIsNull {
			get { return int64ArrayIsNull; }
			set {
				if (int64ArrayIsNull != value) {
					int64ArrayIsNull = value;
					OnPropertyChanged("Int64ArrayIsNull");
					OnPropertyChanged("Int64ArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool int64ArrayIsNull;

		public bool ByteArrayIsNull {
			get { return byteArrayIsNull; }
			set {
				if (byteArrayIsNull != value) {
					byteArrayIsNull = value;
					OnPropertyChanged("ByteArrayIsNull");
					OnPropertyChanged("ByteArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool byteArrayIsNull;

		public bool UInt16ArrayIsNull {
			get { return uint16ArrayIsNull; }
			set {
				if (uint16ArrayIsNull != value) {
					uint16ArrayIsNull = value;
					OnPropertyChanged("UInt16ArrayIsNull");
					OnPropertyChanged("UInt16ArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool uint16ArrayIsNull;

		public bool UInt32ArrayIsNull {
			get { return uint32ArrayIsNull; }
			set {
				if (uint32ArrayIsNull != value) {
					uint32ArrayIsNull = value;
					OnPropertyChanged("UInt32ArrayIsNull");
					OnPropertyChanged("UInt32ArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool uint32ArrayIsNull;

		public bool UInt64ArrayIsNull {
			get { return uint64ArrayIsNull; }
			set {
				if (uint64ArrayIsNull != value) {
					uint64ArrayIsNull = value;
					OnPropertyChanged("UInt64ArrayIsNull");
					OnPropertyChanged("UInt64ArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool uint64ArrayIsNull;

		public bool SingleArrayIsNull {
			get { return singleArrayIsNull; }
			set {
				if (singleArrayIsNull != value) {
					singleArrayIsNull = value;
					OnPropertyChanged("SingleArrayIsNull");
					OnPropertyChanged("SingleArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool singleArrayIsNull;

		public bool DoubleArrayIsNull {
			get { return doubleArrayIsNull; }
			set {
				if (doubleArrayIsNull != value) {
					doubleArrayIsNull = value;
					OnPropertyChanged("DoubleArrayIsNull");
					OnPropertyChanged("DoubleArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool doubleArrayIsNull;

		public bool StringArrayIsNull {
			get { return stringArrayIsNull; }
			set {
				if (stringArrayIsNull != value) {
					stringArrayIsNull = value;
					OnPropertyChanged("StringArrayIsNull");
					OnPropertyChanged("StringArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool stringArrayIsNull;

		public bool EnumArrayIsNull {
			get { return enumArrayIsNull; }
			set {
				if (enumArrayIsNull != value) {
					enumArrayIsNull = value;
					OnPropertyChanged("EnumArrayIsNull");
					OnPropertyChanged("EnumArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool enumArrayIsNull;

		public bool TypeArrayIsNull {
			get { return typeArrayIsNull; }
			set {
				if (typeArrayIsNull != value) {
					typeArrayIsNull = value;
					OnPropertyChanged("TypeArrayIsNull");
					OnPropertyChanged("TypeArrayIsSelectedAndNotNull");
					HasErrorUpdated();
				}
			}
		}
		bool typeArrayIsNull;

		public bool ArraysCanBeNull {
			get { return arraysCanBeNull; }
		}
		readonly bool arraysCanBeNull;

		public ConstantTypeVM(ModuleDef ownerModule, object value, ConstantType[] validConstants, bool allowNullString, bool arraysCanBeNull, TypeSigCreatorOptions options = null)
		{
			if (options == null) {
				IList<ConstantType> clist = validConstants;
				if (clist.IndexOf(ConstantType.Type) >= 0 ||
					clist.IndexOf(ConstantType.TypeArray) >= 0 ||
					clist.IndexOf(ConstantType.ObjectArray) >= 0) {
					throw new ArgumentNullException();
				}
			}
			this.arraysCanBeNull = arraysCanBeNull;
			var list = validConstants.Select(a => typeToEnumVM[a]);
			this.constantTypeEnumListVM = new EnumListVM(list, (a, b) => OnConstantChanged());
			this.boolean = new BooleanVM(a => FieldUpdated());
			this.@char = new CharVM(a => FieldUpdated());
			this.@sbyte = new SByteVM(a => FieldUpdated());
			this.@byte = new ByteVM(a => FieldUpdated());
			this.int16 = new Int16VM(a => FieldUpdated());
			this.uint16 = new UInt16VM(a => FieldUpdated());
			this.int32 = new Int32VM(a => FieldUpdated());
			this.uint32 = new UInt32VM(a => FieldUpdated());
			this.int64 = new Int64VM(a => FieldUpdated());
			this.uint64 = new UInt64VM(a => FieldUpdated());
			this.single = new SingleVM(a => FieldUpdated());
			this.@double = new DoubleVM(a => FieldUpdated());
			this.@string = new StringVM(a => FieldUpdated(), allowNullString);
			this.@enum = new EnumDataFieldVM(ownerModule, a => FieldUpdated());
			this.type = new TypeSigVM(a => FieldUpdated(), options);
			this.objectArray = new ObjectListDataFieldVM(ownerModule, a => FieldUpdated(), options);
			this.booleanArray = new BooleanListDataFieldVM(a => FieldUpdated());
			this.charArray = new CharListDataFieldVM(a => FieldUpdated());
			this.sbyteArray = new SByteListDataFieldVM(a => FieldUpdated());
			this.byteArray = new ByteListDataFieldVM(a => FieldUpdated());
			this.int16Array = new Int16ListDataFieldVM(a => FieldUpdated());
			this.uint16Array = new UInt16ListDataFieldVM(a => FieldUpdated());
			this.int32Array = new Int32ListDataFieldVM(a => FieldUpdated());
			this.uint32Array = new UInt32ListDataFieldVM(a => FieldUpdated());
			this.int64Array = new Int64ListDataFieldVM(a => FieldUpdated());
			this.uint64Array = new UInt64ListDataFieldVM(a => FieldUpdated());
			this.singleArray = new SingleListDataFieldVM(a => FieldUpdated());
			this.doubleArray = new DoubleListDataFieldVM(a => FieldUpdated());
			this.stringArray = new StringListDataFieldVM(a => FieldUpdated());
			this.enumArray = new EnumListDataFieldVM(ownerModule, a => FieldUpdated());
			this.typeArray = new TypeSigListDataFieldVM(a => FieldUpdated(), options);
			this.Value = value;
		}

		void FieldUpdated()
		{
			OnPropertyChanged("Modified");
			HasErrorUpdated();
		}

		void OnConstantChanged()
		{
			OnPropertyChanged("Modified");
			OnPropertyChanged("NullIsSelected");
			OnPropertyChanged("BooleanIsSelected");
			OnPropertyChanged("CharIsSelected");
			OnPropertyChanged("SByteIsSelected");
			OnPropertyChanged("Int16IsSelected");
			OnPropertyChanged("Int32IsSelected");
			OnPropertyChanged("Int64IsSelected");
			OnPropertyChanged("ByteIsSelected");
			OnPropertyChanged("UInt16IsSelected");
			OnPropertyChanged("UInt32IsSelected");
			OnPropertyChanged("UInt64IsSelected");
			OnPropertyChanged("SingleIsSelected");
			OnPropertyChanged("DoubleIsSelected");
			OnPropertyChanged("StringIsSelected");
			OnPropertyChanged("EnumIsSelected");
			OnPropertyChanged("TypeIsSelected");
			OnPropertyChanged("ObjectArrayIsSelected");
			OnPropertyChanged("ObjectArrayIsSelectedAndNotNull");
			OnPropertyChanged("BooleanArrayIsSelected");
			OnPropertyChanged("BooleanArrayIsSelectedAndNotNull");
			OnPropertyChanged("CharArrayIsSelected");
			OnPropertyChanged("CharArrayIsSelectedAndNotNull");
			OnPropertyChanged("SByteArrayIsSelected");
			OnPropertyChanged("SByteArrayIsSelectedAndNotNull");
			OnPropertyChanged("Int16ArrayIsSelected");
			OnPropertyChanged("Int16ArrayIsSelectedAndNotNull");
			OnPropertyChanged("Int32ArrayIsSelected");
			OnPropertyChanged("Int32ArrayIsSelectedAndNotNull");
			OnPropertyChanged("Int64ArrayIsSelected");
			OnPropertyChanged("Int64ArrayIsSelectedAndNotNull");
			OnPropertyChanged("ByteArrayIsSelected");
			OnPropertyChanged("ByteArrayIsSelectedAndNotNull");
			OnPropertyChanged("UInt16ArrayIsSelected");
			OnPropertyChanged("UInt16ArrayIsSelectedAndNotNull");
			OnPropertyChanged("UInt32ArrayIsSelected");
			OnPropertyChanged("UInt32ArrayIsSelectedAndNotNull");
			OnPropertyChanged("UInt64ArrayIsSelected");
			OnPropertyChanged("UInt64ArrayIsSelectedAndNotNull");
			OnPropertyChanged("SingleArrayIsSelected");
			OnPropertyChanged("SingleArrayIsSelectedAndNotNull");
			OnPropertyChanged("DoubleArrayIsSelected");
			OnPropertyChanged("DoubleArrayIsSelectedAndNotNull");
			OnPropertyChanged("StringArrayIsSelected");
			OnPropertyChanged("StringArrayIsSelectedAndNotNull");
			OnPropertyChanged("EnumArrayIsSelected");
			OnPropertyChanged("EnumArrayIsSelectedAndNotNull");
			OnPropertyChanged("TypeArrayIsSelected");
			OnPropertyChanged("TypeArrayIsSelectedAndNotNull");
			HasErrorUpdated();
		}

		protected override string Verify(string columnName)
		{
			return string.Empty;
		}

		public override bool HasError {
			get {
				if (!IsEnabled)
					return false;

				switch ((ConstantType)ConstantTypeEnumList.SelectedItem) {
				case ConstantType.Null:			break;
				case ConstantType.Boolean:		if (boolean.HasError) return true; break;
				case ConstantType.Char:			if (@char.HasError) return true; break;
				case ConstantType.SByte:		if (@sbyte.HasError) return true; break;
				case ConstantType.Int16:		if (int16.HasError) return true; break;
				case ConstantType.Int32:		if (int32.HasError) return true; break;
				case ConstantType.Int64:		if (int64.HasError) return true; break;
				case ConstantType.Byte:			if (@byte.HasError) return true; break;
				case ConstantType.UInt16:		if (uint16.HasError) return true; break;
				case ConstantType.UInt32:		if (uint32.HasError) return true; break;
				case ConstantType.UInt64:		if (uint64.HasError) return true; break;
				case ConstantType.Single:		if (single.HasError) return true; break;
				case ConstantType.Double:		if (@double.HasError) return true; break;
				case ConstantType.String:		if (@string.HasError) return true; break;
				case ConstantType.Enum:			if (@enum.HasError) return true; break;
				case ConstantType.Type:			if (type.HasError) return true; break;
				case ConstantType.ObjectArray:	if (!ObjectArrayIsNull && objectArray.HasError) return true; break;
				case ConstantType.BooleanArray:	if (!BooleanArrayIsNull && booleanArray.HasError) return true; break;
				case ConstantType.CharArray:	if (!CharArrayIsNull && charArray.HasError) return true; break;
				case ConstantType.SByteArray:	if (!SByteArrayIsNull && sbyteArray.HasError) return true; break;
				case ConstantType.Int16Array:	if (!Int16ArrayIsNull && int16Array.HasError) return true; break;
				case ConstantType.Int32Array:	if (!Int32ArrayIsNull && int32Array.HasError) return true; break;
				case ConstantType.Int64Array:	if (!Int64ArrayIsNull && int64Array.HasError) return true; break;
				case ConstantType.ByteArray:	if (!ByteArrayIsNull && byteArray.HasError) return true; break;
				case ConstantType.UInt16Array:	if (!UInt16ArrayIsNull && uint16Array.HasError) return true; break;
				case ConstantType.UInt32Array:	if (!UInt32ArrayIsNull && uint32Array.HasError) return true; break;
				case ConstantType.UInt64Array:	if (!UInt64ArrayIsNull && uint64Array.HasError) return true; break;
				case ConstantType.SingleArray:	if (!SingleArrayIsNull && singleArray.HasError) return true; break;
				case ConstantType.DoubleArray:	if (!DoubleArrayIsNull && doubleArray.HasError) return true; break;
				case ConstantType.StringArray:	if (!StringArrayIsNull && stringArray.HasError) return true; break;
				case ConstantType.EnumArray:	if (!EnumArrayIsNull && enumArray.HasError) return true; break;
				case ConstantType.TypeArray:	if (!TypeArrayIsNull && typeArray.HasError) return true; break;
				default: throw new InvalidOperationException();
				}

				return false;
			}
		}
	}
}
