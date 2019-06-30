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
using System.Text;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Search;

namespace dnSpy.AsmEditor.DnlibDialogs {
	struct EnumInfo {
		public ITypeDefOrRef? EnumType;
		public object? Value;
		public bool IsArray;

		public static EnumInfo CreateNullArray(ITypeDefOrRef? type) => new EnumInfo() {
			EnumType = type,
			IsArray = true,
		};

		public override string ToString() {
			var td = EnumType.ResolveTypeDef();
			if (!(td is null)) {
				var s = ModelUtils.GetEnumFieldName(td, Value);
				if (!(s is null))
					return $"{EnumType}.{s}";
			}
			if (!IsArray)
				return $"({(EnumType ?? (object)dnSpy_AsmEditor_Resources.UnknownEnum)}){Value}";

			var list = Value as System.Collections.IList;
			if (list is null)
				return $"({(EnumType ?? (object)dnSpy_AsmEditor_Resources.UnknownEnum)}[])null";

			var sb = new StringBuilder();
			sb.Append($"new {(EnumType ?? (object)dnSpy_AsmEditor_Resources.UnknownEnum)}[] {{");
			for (int i = 0; i < list.Count; i++) {
				if (i > 0)
					sb.Append(',');
				sb.Append(' ');
				var s = ModelUtils.GetEnumFieldName(td, list[i]);
				sb.Append(s ?? (Value is null ? "null" : Value.ToString()));
			}
			sb.Append(" }");
			return sb.ToString();
		}
	}

	abstract class EnumDataFieldVMBase : DataFieldVM<EnumInfo> {
		EnumInfo enumInfo;
		DataFieldVM? enumUnderlyingTypeField;

		public IDnlibTypePicker DnlibTypePicker {
			set => dnlibTypePicker = value;
		}
		IDnlibTypePicker? dnlibTypePicker;

		public ICommand PickEnumTypeCommand => new RelayCommand(a => PickEnumType());

		public ITypeDefOrRef? EnumType {
			get => enumInfo.EnumType;
			set {
				enumInfo.EnumType = value;
				var td = value.ResolveTypeDef();
				if (td is null || !td.IsEnum)
					enumUnderlyingTypeField = null;
				else {
					enumUnderlyingTypeField = CreateEnumUnderlyingTypeField(td.GetEnumUnderlyingType().RemovePinnedAndModifiers().GetElementType());
					if (!(enumUnderlyingTypeField is null)) {
						enumUnderlyingTypeField.StringValue = StringValue;
						ForceWriteStringValue(enumUnderlyingTypeField.StringValue);
					}
				}
				OnPropertyChanged(nameof(PickEnumToolTip));
			}
		}

		public string PickEnumToolTip {
			get {
				if (enumInfo.EnumType is null)
					return dnSpy_AsmEditor_Resources.Pick_EnumType;
				return string.Format(dnSpy_AsmEditor_Resources.EnumType, enumInfo.EnumType.FullName);
			}
		}

		public EnumInfo NullValue => EnumInfo.CreateNullArray(enumInfo.EnumType);

		readonly ModuleDef ownerModule;

		protected EnumDataFieldVMBase(ModuleDef ownerModule, EnumInfo value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) {
			this.ownerModule = ownerModule;
			SetValueFromConstructor(value);
		}

		protected override void OnStringValueChanged() {
			if (!(enumUnderlyingTypeField is null))
				enumUnderlyingTypeField.StringValue = StringValue;
		}

		protected override string OnNewValue(EnumInfo value) {
			InitializeEnumUnderlyingTypeField(value);

			if (enumUnderlyingTypeField is null)
				return string.Empty;
			else {
				enumUnderlyingTypeField.ObjectValue = value.Value;
				return enumUnderlyingTypeField.StringValue;
			}
		}

		protected override string? ConvertToValue(out EnumInfo value) {
			string? error = null;
			value = enumInfo;
			if (!(enumUnderlyingTypeField is null))
				error = enumUnderlyingTypeField.ConvertToObjectValue(out value.Value);

			return error;
		}

		void InitializeEnumUnderlyingTypeField(EnumInfo enumInfo) {
			this.enumInfo = enumInfo;
			enumUnderlyingTypeField = null;

			if (!(enumInfo.Value is null))
				enumUnderlyingTypeField = CreateEnumUnderlyingTypeFieldFromValue(enumInfo.Value);
			else {
				var td = enumInfo.EnumType.ResolveTypeDef();
				if (!(td is null) && td.IsEnum)
					enumUnderlyingTypeField = CreateEnumUnderlyingTypeField(td.GetEnumUnderlyingType().RemovePinnedAndModifiers().GetElementType());
			}
		}

		void PickEnumType() {
			if (dnlibTypePicker is null)
				throw new InvalidOperationException();
			var type = dnlibTypePicker.GetDnlibType(dnSpy_AsmEditor_Resources.Pick_EnumType, new FlagsDocumentTreeNodeFilter(VisibleMembersFlags.EnumTypeDef), EnumType, ownerModule);
			if (!(type is null))
				EnumType = type;
		}

		protected abstract DataFieldVM? CreateEnumUnderlyingTypeFieldFromValue(object value);
		protected abstract DataFieldVM? CreateEnumUnderlyingTypeField(ElementType elementType);
	}

	sealed class EnumDataFieldVM : EnumDataFieldVMBase {
		public EnumDataFieldVM(ModuleDef ownerModule, Action<DataFieldVM> onUpdated)
			: this(ownerModule, new EnumInfo(), onUpdated) {
		}

		public EnumDataFieldVM(ModuleDef ownerModule, EnumInfo value, Action<DataFieldVM> onUpdated)
			: base(ownerModule, value, onUpdated) {
		}

		protected override DataFieldVM? CreateEnumUnderlyingTypeFieldFromValue(object value) {
			if (value is bool)		return new BooleanVM((bool)value, a => { });
			if (value is char)		return new CharVM((char)value, a => { });
			if (value is sbyte)		return new SByteVM((sbyte)value, a => { });
			if (value is byte)		return new ByteVM((byte)value, a => { });
			if (value is short)		return new Int16VM((short)value, a => { });
			if (value is ushort)	return new UInt16VM((ushort)value, a => { });
			if (value is int)		return new Int32VM((int)value, a => { });
			if (value is uint)		return new UInt32VM((uint)value, a => { });
			if (value is long)		return new Int64VM((long)value, a => { });
			if (value is ulong)		return new UInt64VM((ulong)value, a => { });
			if (value is float)		return new SingleVM((float)value, a => { });
			if (value is double)	return new DoubleVM((double)value, a => { });
			return null;
		}

		protected override DataFieldVM? CreateEnumUnderlyingTypeField(ElementType elementType) {
			switch (elementType) {
			case ElementType.Boolean:	return new BooleanVM(a => { });
			case ElementType.Char:		return new CharVM(a => { });
			case ElementType.I1:		return new SByteVM(a => { });
			case ElementType.U1:		return new ByteVM(a => { });
			case ElementType.I2:		return new Int16VM(a => { });
			case ElementType.U2:		return new UInt16VM(a => { });
			case ElementType.I4:		return new Int32VM(a => { });
			case ElementType.U4:		return new UInt32VM(a => { });
			case ElementType.I8:		return new Int64VM(a => { });
			case ElementType.U8:		return new UInt64VM(a => { });
			case ElementType.R4:		return new SingleVM(a => { });
			case ElementType.R8:		return new DoubleVM(a => { });
			}
			return null;
		}
	}

	sealed class EnumListDataFieldVM : EnumDataFieldVMBase {
		public EnumListDataFieldVM(ModuleDef ownerModule, Action<DataFieldVM> onUpdated)
			: this(ownerModule, EnumInfo.CreateNullArray(null), onUpdated) {
		}

		public EnumListDataFieldVM(ModuleDef ownerModule, EnumInfo value, Action<DataFieldVM> onUpdated)
			: base(ownerModule, value, onUpdated)
		{
		}

		protected override DataFieldVM? CreateEnumUnderlyingTypeFieldFromValue(object value) {
			if (value is IList<bool>)	return new BooleanListDataFieldVM((IList<bool>)value, a => { });
			if (value is IList<char>)	return new CharListDataFieldVM((IList<char>)value, a => { });
			if (value is IList<sbyte>)	return new SByteListDataFieldVM((IList<sbyte>)value, a => { });
			if (value is IList<byte>)	return new ByteListDataFieldVM((IList<byte>)value, a => { });
			if (value is IList<short>)	return new Int16ListDataFieldVM((IList<short>)value, a => { });
			if (value is IList<ushort>)	return new UInt16ListDataFieldVM((IList<ushort>)value, a => { });
			if (value is IList<int>)	return new Int32ListDataFieldVM((IList<int>)value, a => { });
			if (value is IList<uint>)	return new UInt32ListDataFieldVM((IList<uint>)value, a => { });
			if (value is IList<long>)	return new Int64ListDataFieldVM((IList<long>)value, a => { });
			if (value is IList<ulong>)	return new UInt64ListDataFieldVM((IList<ulong>)value, a => { });
			if (value is IList<float>)	return new SingleListDataFieldVM((IList<float>)value, a => { });
			if (value is IList<double>)	return new DoubleListDataFieldVM((IList<double>)value, a => { });
			return null;
		}

		protected override DataFieldVM? CreateEnumUnderlyingTypeField(ElementType elementType) {
			switch (elementType) {
			case ElementType.Boolean:	return new BooleanListDataFieldVM(a => { });
			case ElementType.Char:		return new CharListDataFieldVM(a => { });
			case ElementType.I1:		return new SByteListDataFieldVM(a => { });
			case ElementType.U1:		return new ByteListDataFieldVM(a => { });
			case ElementType.I2:		return new Int16ListDataFieldVM(a => { });
			case ElementType.U2:		return new UInt16ListDataFieldVM(a => { });
			case ElementType.I4:		return new Int32ListDataFieldVM(a => { });
			case ElementType.U4:		return new UInt32ListDataFieldVM(a => { });
			case ElementType.I8:		return new Int64ListDataFieldVM(a => { });
			case ElementType.U8:		return new UInt64ListDataFieldVM(a => { });
			case ElementType.R4:		return new SingleListDataFieldVM(a => { });
			case ElementType.R8:		return new DoubleListDataFieldVM(a => { });
			}
			return null;
		}
	}
}
