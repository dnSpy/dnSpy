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
using System.Diagnostics;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Shared.UI.MVVM;
using dnSpy.Shared.UI.Search;

namespace dnSpy.AsmEditor.DnlibDialogs {
	enum NamedArgType {
		Property,
		Field,
	}

	sealed class CANamedArgumentVM : ViewModelBase {
		public IDnlibTypePicker DnlibTypePicker {
			set { dnlibTypePicker = value; }
		}
		IDnlibTypePicker dnlibTypePicker;

		public ICommand PickEnumTypeCommand {
			get { return new RelayCommand(a => PickEnumType(), a => PickEnumTypeCanExecute()); }
		}

		public bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					isEnabled = value;
					CAArgumentVM.IsEnabled = value;
					OnPropertyChanged("IsEnabled");
					HasErrorUpdated();
				}
			}
		}
		bool isEnabled = true;

		public ITypeDefOrRef EnumType {
			get { return enumType; }
			set {
				if (enumType != value) {
					enumType = value;
					modified = true;
					OnPropertyChanged("EnumType");
					OnPropertyChanged("PickEnumToolTip");
				}
			}
		}
		ITypeDefOrRef enumType;

		public string PickEnumToolTip {
			get {
				if (EnumType == null)
					return "Pick an Enum Type";
				return string.Format("Enum: {0}", EnumType.FullName);
			}
		}

		public bool EnumIsSelected {
			get {
				return (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.Enum ||
					  (ConstantType)ConstantTypeEnumList.SelectedItem == ConstantType.EnumArray;
			}
		}

		public bool IsField {
			get { return (NamedArgType)NamedArgTypeEnumList.SelectedItem == NamedArgType.Field; }
			set {
				if (value)
					NamedArgTypeEnumList.SelectedItem = NamedArgType.Field;
				else
					NamedArgTypeEnumList.SelectedItem = NamedArgType.Property;
			}
		}

		public string Name {
			get { return name; }
			set {
				if (name != value) {
					name = value;
					modified = true;
					OnPropertyChanged("Name");
				}
			}
		}
		UTF8String name;

		public EnumListVM ConstantTypeEnumList {
			get { return constantTypeEnumListVM; }
		}
		readonly EnumListVM constantTypeEnumListVM;

		public EnumListVM NamedArgTypeEnumList {
			get { return namedArgTypeEnumListVM; }
		}
		readonly EnumListVM namedArgTypeEnumListVM;

		public CAArgumentVM CAArgumentVM {
			get { return caArgumentVM; }
		}
		CAArgumentVM caArgumentVM;

		static readonly ConstantType[] validTypes = new ConstantType[] {
			ConstantType.Object,
			ConstantType.Boolean,
			ConstantType.Char,
			ConstantType.SByte,
			ConstantType.Byte,
			ConstantType.Int16,
			ConstantType.UInt16,
			ConstantType.Int32,
			ConstantType.UInt32,
			ConstantType.Int64,
			ConstantType.UInt64,
			ConstantType.Single,
			ConstantType.Double,
			ConstantType.String,
			ConstantType.Enum,
			ConstantType.Type,
			ConstantType.ObjectArray,
			ConstantType.BooleanArray,
			ConstantType.CharArray,
			ConstantType.SByteArray,
			ConstantType.ByteArray,
			ConstantType.Int16Array,
			ConstantType.UInt16Array,
			ConstantType.Int32Array,
			ConstantType.UInt32Array,
			ConstantType.Int64Array,
			ConstantType.UInt64Array,
			ConstantType.SingleArray,
			ConstantType.DoubleArray,
			ConstantType.StringArray,
			ConstantType.EnumArray,
			ConstantType.TypeArray,
		};

		bool modified;
		readonly CANamedArgument originalNamedArg;
		readonly ModuleDef ownerModule;

		public CANamedArgumentVM(ModuleDef ownerModule, CANamedArgument namedArg, TypeSigCreatorOptions options) {
			this.ownerModule = ownerModule;
			this.originalNamedArg = namedArg.Clone();
			this.constantTypeEnumListVM = new EnumListVM(ConstantTypeVM.CreateEnumArray(validTypes), (a, b) => OnConstantTypeChanged());
			this.namedArgTypeEnumListVM = new EnumListVM(EnumVM.Create(typeof(NamedArgType)), (a, b) => OnNamedArgTypeChanged());
			InitializeFrom(namedArg, options);
			this.modified = false;
		}

		void OnConstantTypeChanged() {
			modified = true;
			OnPropertyChanged("EnumIsSelected");
			UpdateArgumentType();
			HasErrorUpdated();
		}

		void UpdateArgumentType() {
			if (CAArgumentVM != null) {
				var ct = (ConstantType)ConstantTypeEnumList.SelectedItem;
				if (ct != ConstantType.Object && ct != ConstantType.ObjectArray)
					CAArgumentVM.ConstantTypeVM.ConstantTypeEnumList.SelectedItem = ct;
				CAArgumentVM.StorageType = GetType(ct);
			}
		}

		void OnNamedArgTypeChanged() {
			modified = true;
			OnPropertyChanged("IsField");
			HasErrorUpdated();
		}

		void InitializeFrom(CANamedArgument namedArg, TypeSigCreatorOptions options) {
			if (CAArgumentVM != null)
				CAArgumentVM.PropertyChanged -= caArgumentVM_PropertyChanged;
			caArgumentVM = new CAArgumentVM(ownerModule, namedArg.Argument, options, null);
			OnPropertyChanged("CAArgumentVM");
			CAArgumentVM.PropertyChanged += caArgumentVM_PropertyChanged;

			Name = namedArg.Name;
			IsField = namedArg.IsField;
			ITypeDefOrRef newEnumType;
			ConstantTypeEnumList.SelectedItem = GetConstantType(namedArg.Type, out newEnumType);
			EnumType = newEnumType;
			CAArgumentVM.StorageType = GetType((ConstantType)ConstantTypeEnumList.SelectedItem);
		}

		void caArgumentVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
			if (e.PropertyName == "Modified")
				modified = true;
			if (e.PropertyName == "IsEnabled")
				IsEnabled = CAArgumentVM.IsEnabled;
			HasErrorUpdated();
		}

		static ConstantType GetConstantType(TypeSig type, out ITypeDefOrRef enumType) {
			enumType = null;
			var t = type.RemovePinnedAndModifiers();
			switch (t.GetElementType())
			{
			case ElementType.Boolean:	return ConstantType.Boolean;
			case ElementType.Char:		return ConstantType.Char;
			case ElementType.I1:		return ConstantType.SByte;
			case ElementType.U1:		return ConstantType.Byte;
			case ElementType.I2:		return ConstantType.Int16;
			case ElementType.U2:		return ConstantType.UInt16;
			case ElementType.I4:		return ConstantType.Int32;
			case ElementType.U4:		return ConstantType.UInt32;
			case ElementType.I8:		return ConstantType.Int64;
			case ElementType.U8:		return ConstantType.UInt64;
			case ElementType.R4:		return ConstantType.Single;
			case ElementType.R8:		return ConstantType.Double;
			case ElementType.String:	return ConstantType.String;
			case ElementType.Object:	return ConstantType.Object;

			case ElementType.ValueType:
			case ElementType.Class:
				var tdr = ((ClassOrValueTypeSig)t).TypeDefOrRef;
				if (tdr.IsSystemType())
					return ConstantType.Type;
				enumType = tdr;
				return ConstantType.Enum;

			case ElementType.SZArray:
				var elemType = t.Next.RemovePinnedAndModifiers();
				switch (elemType.GetElementType()) {
				case ElementType.Boolean:	return ConstantType.BooleanArray;
				case ElementType.Char:		return ConstantType.CharArray;
				case ElementType.I1:		return ConstantType.SByteArray;
				case ElementType.U1:		return ConstantType.ByteArray;
				case ElementType.I2:		return ConstantType.Int16Array;
				case ElementType.U2:		return ConstantType.UInt16Array;
				case ElementType.I4:		return ConstantType.Int32Array;
				case ElementType.U4:		return ConstantType.UInt32Array;
				case ElementType.I8:		return ConstantType.Int64Array;
				case ElementType.U8:		return ConstantType.UInt64Array;
				case ElementType.R4:		return ConstantType.SingleArray;
				case ElementType.R8:		return ConstantType.DoubleArray;
				case ElementType.String:	return ConstantType.StringArray;
				case ElementType.Object:	return ConstantType.ObjectArray;
				case ElementType.ValueType:
				case ElementType.Class:
					tdr = ((ClassOrValueTypeSig)elemType).TypeDefOrRef;
					if (tdr.IsSystemType())
						return ConstantType.TypeArray;
					enumType = tdr;
					return ConstantType.EnumArray;
				}
				break;
			}

			Debug.Fail(string.Format("Unsupported CA named type: {0}", type));
			return ConstantType.Object;
		}

		public CANamedArgument CreateCANamedArgument() {
			if (!modified)
				return originalNamedArg.Clone();
			var type = GetType((ConstantType)ConstantTypeEnumList.SelectedItem);
			return new CANamedArgument(IsField, type, Name, CAArgumentVM.CreateCAArgument(type));
		}

		TypeSig GetType(ConstantType ct) {
			switch (ct) {
			case ConstantType.Object:	return ownerModule.CorLibTypes.Object;
			case ConstantType.Boolean:	return ownerModule.CorLibTypes.Boolean;
			case ConstantType.Char:		return ownerModule.CorLibTypes.Char;
			case ConstantType.SByte:	return ownerModule.CorLibTypes.SByte;
			case ConstantType.Byte:		return ownerModule.CorLibTypes.Byte;
			case ConstantType.Int16:	return ownerModule.CorLibTypes.Int16;
			case ConstantType.UInt16:	return ownerModule.CorLibTypes.UInt16;
			case ConstantType.Int32:	return ownerModule.CorLibTypes.Int32;
			case ConstantType.UInt32:	return ownerModule.CorLibTypes.UInt32;
			case ConstantType.Int64:	return ownerModule.CorLibTypes.Int64;
			case ConstantType.UInt64:	return ownerModule.CorLibTypes.UInt64;
			case ConstantType.Single:	return ownerModule.CorLibTypes.Single;
			case ConstantType.Double:	return ownerModule.CorLibTypes.Double;
			case ConstantType.String:	return ownerModule.CorLibTypes.String;
			case ConstantType.Enum:		return new ValueTypeSig(EnumType);
			case ConstantType.Type:		return new ClassSig(ownerModule.CorLibTypes.GetTypeRef("System", "Type"));

			case ConstantType.ObjectArray:	return new SZArraySig(ownerModule.CorLibTypes.Object);
			case ConstantType.BooleanArray:	return new SZArraySig(ownerModule.CorLibTypes.Boolean);
			case ConstantType.CharArray:	return new SZArraySig(ownerModule.CorLibTypes.Char);
			case ConstantType.SByteArray:	return new SZArraySig(ownerModule.CorLibTypes.SByte);
			case ConstantType.ByteArray:	return new SZArraySig(ownerModule.CorLibTypes.Byte);
			case ConstantType.Int16Array:	return new SZArraySig(ownerModule.CorLibTypes.Int16);
			case ConstantType.UInt16Array:	return new SZArraySig(ownerModule.CorLibTypes.UInt16);
			case ConstantType.Int32Array:	return new SZArraySig(ownerModule.CorLibTypes.Int32);
			case ConstantType.UInt32Array:	return new SZArraySig(ownerModule.CorLibTypes.UInt32);
			case ConstantType.Int64Array:	return new SZArraySig(ownerModule.CorLibTypes.Int64);
			case ConstantType.UInt64Array:	return new SZArraySig(ownerModule.CorLibTypes.UInt64);
			case ConstantType.SingleArray:	return new SZArraySig(ownerModule.CorLibTypes.Single);
			case ConstantType.DoubleArray:	return new SZArraySig(ownerModule.CorLibTypes.Double);
			case ConstantType.StringArray:	return new SZArraySig(ownerModule.CorLibTypes.String);
			case ConstantType.EnumArray:	return new SZArraySig(new ValueTypeSig(EnumType));
			case ConstantType.TypeArray:	return new SZArraySig(new ClassSig(ownerModule.CorLibTypes.GetTypeRef("System", "Type")));
			}

			Debug.Fail(string.Format("Unknown constant type: {0}", ct));
			return ownerModule.CorLibTypes.Object;
		}

		void PickEnumType() {
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			var type = dnlibTypePicker.GetDnlibType(new FlagsFileTreeNodeFilter(VisibleMembersFlags.EnumTypeDef), EnumType, ownerModule);
			if (type != null)
				EnumType = type;
		}

		bool PickEnumTypeCanExecute() {
			return IsEnabled;
		}

		public override bool HasError {
			get { return IsEnabled && CAArgumentVM.HasError; }
		}

		public override string ToString() {
			return string.Format("{0} = {1}", Name, CAArgumentVM.ToString());
		}
	}
}
