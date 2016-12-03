/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Linq;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class NullableCompressedUInt32 : NullableUInt32VM {
		public NullableCompressedUInt32(Action<DataFieldVM> onUpdated)
			: this(null, onUpdated) {
		}

		public NullableCompressedUInt32(uint? value, Action<DataFieldVM> onUpdated)
			: base(value, onUpdated) {
			Min = ModelUtils.COMPRESSED_UINT32_MIN;
			Max = ModelUtils.COMPRESSED_UINT32_MAX;
		}
	}

	sealed class MarshalTypeVM : ViewModelBase {
		static readonly EnumVM[] nativeTypeList = EnumVM.Create(typeof(dnlib.DotNet.NativeType));
		public EnumListVM NativeType { get; }
		public bool NativeType_IsEnabled => IsEnabled;

		public bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					isEnabled = value;
					OnPropertyChanged(nameof(IsEnabled));
					OnPropertyChanged(nameof(NativeType_IsEnabled));
					OnPropertyChanged(nameof(RawMarshalType_Data_IsEnabled));
					OnPropertyChanged(nameof(FixedSysStringMarshalType_Size_IsEnabled));
					OnSafeArrayMarshalTypeIsEnabledChanged();
					OnFixedArrayMarshalTypeIsEnabledChanged();
					OnArrayMarshalTypeIsEnabledChanged();
					OnPropertyChanged(nameof(CustomMarshalType_GUID_IsEnabled));
					OnPropertyChanged(nameof(CustomMarshalType_NativeTypeName_IsEnabled));
					OnPropertyChanged(nameof(CustomMarshalType_CustMarshaler_TypeSigCreator_IsEnabled));
					OnPropertyChanged(nameof(CustomMarshalType_Cookie_IsEnabled));
					OnPropertyChanged(nameof(InterfaceMarshalType_IidParamIndex_IsEnabled));
					TypeStringUpdated();
					if (!isEnabled)
						NativeType.SelectedItem = dnlib.DotNet.NativeType.End;
					HasErrorUpdated();
				}
			}
		}
		bool isEnabled = true;

		public bool IsMarshalType {
			get {
				return !IsRawMarshalType && !IsFixedSysStringMarshalType &&
						!IsSafeArrayMarshalType && !IsFixedArrayMarshalType &&
						!IsArrayMarshalType && !IsCustomMarshalType &&
						!IsInterfaceMarshalType;
			}
		}

		public bool IsRawMarshalType => (dnlib.DotNet.NativeType)NativeType.SelectedItem == dnlib.DotNet.NativeType.RawBlob;
		public bool IsFixedSysStringMarshalType => (dnlib.DotNet.NativeType)NativeType.SelectedItem == dnlib.DotNet.NativeType.FixedSysString;
		public bool IsSafeArrayMarshalType => (dnlib.DotNet.NativeType)NativeType.SelectedItem == dnlib.DotNet.NativeType.SafeArray;
		public bool IsFixedArrayMarshalType => (dnlib.DotNet.NativeType)NativeType.SelectedItem == dnlib.DotNet.NativeType.FixedArray;
		public bool IsArrayMarshalType => (dnlib.DotNet.NativeType)NativeType.SelectedItem == dnlib.DotNet.NativeType.Array;
		public bool IsCustomMarshalType => (dnlib.DotNet.NativeType)NativeType.SelectedItem == dnlib.DotNet.NativeType.CustomMarshaler;

		public bool IsInterfaceMarshalType {
			get {
				return (dnlib.DotNet.NativeType)NativeType.SelectedItem == dnlib.DotNet.NativeType.IUnknown ||
					(dnlib.DotNet.NativeType)NativeType.SelectedItem == dnlib.DotNet.NativeType.IDispatch ||
					(dnlib.DotNet.NativeType)NativeType.SelectedItem == dnlib.DotNet.NativeType.IntF;
			}
		}

		public HexStringVM RawMarshalType_Data { get; }
		public bool RawMarshalType_Data_IsEnabled => IsEnabled;
		public NullableCompressedUInt32 FixedSysStringMarshalType_Size { get; }
		public bool FixedSysStringMarshalType_Size_IsEnabled => IsEnabled;

		static readonly EnumVM[] variantTypeList = new EnumVM[] {
			new EnumVM(VariantType.NotInitialized, dnSpy_AsmEditor_Resources.MarshalType_EnumNotInitialized),
			new EnumVM(VariantType.None, "None"),
			new EnumVM(VariantType.Null, "Null"),
			new EnumVM(VariantType.I2, "I2"),
			new EnumVM(VariantType.I4, "I4"),
			new EnumVM(VariantType.R4, "R4"),
			new EnumVM(VariantType.R8, "R8"),
			new EnumVM(VariantType.CY, "CY"),
			new EnumVM(VariantType.Date, "Date"),
			new EnumVM(VariantType.BStr, "BStr"),
			new EnumVM(VariantType.Dispatch, "Dispatch"),
			new EnumVM(VariantType.Error, "Error"),
			new EnumVM(VariantType.Bool, "Bool"),
			new EnumVM(VariantType.Variant, "Variant"),
			new EnumVM(VariantType.Unknown, dnSpy_AsmEditor_Resources.Unknown),
			new EnumVM(VariantType.Decimal, "Decimal"),
			new EnumVM(VariantType.I1, "I1"),
			new EnumVM(VariantType.UI1, "UI1"),
			new EnumVM(VariantType.UI2, "UI2"),
			new EnumVM(VariantType.UI4, "UI4"),
			new EnumVM(VariantType.I8, "I8"),
			new EnumVM(VariantType.UI8, "UI8"),
			new EnumVM(VariantType.Int, "Int"),
			new EnumVM(VariantType.UInt, "UInt"),
			new EnumVM(VariantType.Void, "Void"),
			new EnumVM(VariantType.HResult, "HResult"),
			new EnumVM(VariantType.Ptr, "Ptr"),
			new EnumVM(VariantType.SafeArray, "SafeArray"),
			new EnumVM(VariantType.CArray, "CArray"),
			new EnumVM(VariantType.UserDefined, "UserDefined"),
			new EnumVM(VariantType.LPStr, "LPStr"),
			new EnumVM(VariantType.LPWStr, "LPWStr"),
			new EnumVM(VariantType.Record, "Record"),
			new EnumVM(VariantType.IntPtr, "IntPtr"),
			new EnumVM(VariantType.UIntPtr, "UIntPtr"),
			new EnumVM(VariantType.FileTime, "FileTime"),
			new EnumVM(VariantType.Blob, "Blob"),
			new EnumVM(VariantType.Stream, "Stream"),
			new EnumVM(VariantType.Storage, "Storage"),
			new EnumVM(VariantType.StreamedObject, "StreamedObject"),
			new EnumVM(VariantType.StoredObject, "StoredObject"),
			new EnumVM(VariantType.BlobObject, "BlobObject"),
			new EnumVM(VariantType.CF, "CF"),
			new EnumVM(VariantType.CLSID, "CLSID"),
			new EnumVM(VariantType.VersionedStream, "VersionedStream"),
			new EnumVM(VariantType.BStrBlob, "BStrBlob"),
		};
		public EnumListVM SafeArrayMarshalType_VariantType { get; }

		public VariantType SafeArrayMarshalType_VT {
			get {
				return (safeArrayMarshalType_vt & ~VariantType.TypeMask) |
					((VariantType)SafeArrayMarshalType_VariantType.SelectedItem & VariantType.TypeMask);
			}
			set {
				if (safeArrayMarshalType_vt != value) {
					safeArrayMarshalType_vt = value;
					OnPropertyChanged(nameof(SafeArrayMarshalType_VT));
					OnPropertyChanged(nameof(SafeArrayMarshalType_VT_Vector));
					OnPropertyChanged(nameof(SafeArrayMarshalType_VT_Array));
					OnPropertyChanged(nameof(SafeArrayMarshalType_VT_ByRef));
					OnPropertyChanged(nameof(SafeArrayMarshalType_VT_Reserved));
					TypeStringUpdated();
				}
			}
		}
		VariantType safeArrayMarshalType_vt;

		public bool SafeArrayMarshalType_VT_IsEnabled => IsEnabled;

		public bool SafeArrayMarshalType_VT_Flags_IsEnabled {
			get {
				return SafeArrayMarshalType_VT_IsEnabled &&
					(VariantType)SafeArrayMarshalType_VariantType.SelectedItem != VariantType.NotInitialized;
			}
		}

		public bool SafeArrayMarshalType_VT_Vector {
			get { return GetFlagValue(VariantType.Vector); }
			set { SetFlagValue(VariantType.Vector, value); }
		}

		public bool SafeArrayMarshalType_VT_Array {
			get { return GetFlagValue(VariantType.Array); }
			set { SetFlagValue(VariantType.Array, value); }
		}

		public bool SafeArrayMarshalType_VT_ByRef {
			get { return GetFlagValue(VariantType.ByRef); }
			set { SetFlagValue(VariantType.ByRef, value); }
		}

		public bool SafeArrayMarshalType_VT_Reserved {
			get { return GetFlagValue(VariantType.Reserved); }
			set { SetFlagValue(VariantType.Reserved, value); }
		}

		bool GetFlagValue(VariantType flag) => (SafeArrayMarshalType_VT & flag) != 0;

		void SetFlagValue(VariantType flag, bool value) {
			if (value)
				SafeArrayMarshalType_VT |= flag;
			else
				SafeArrayMarshalType_VT &= ~flag;
		}

		public TypeSigCreatorVM SafeArrayMarshalType_UserDefinedSubType_TypeSigCreator { get; }
		public bool SafeArrayMarshalType_UserDefinedSubType_TypeSigCreator_IsEnabled => SafeArrayMarshalType_VT_Flags_IsEnabled;
		public NullableCompressedUInt32 FixedArrayMarshalType_Size { get; }
		public bool FixedArrayMarshalType_Size_IsEnabled => IsEnabled;
		public EnumListVM FixedArrayMarshalType_NativeType { get; }
		public bool FixedArrayMarshalType_NativeType_IsEnabled => FixedArrayMarshalType_Size_IsEnabled && !FixedArrayMarshalType_Size.IsNull;
		public EnumListVM ArrayMarshalType_NativeType { get; }
		public bool ArrayMarshalType_NativeType_IsEnabled => IsEnabled;
		public NullableCompressedUInt32 ArrayMarshalType_ParamNum { get; }
		public bool ArrayMarshalType_ParamNum_IsEnabled =>
			ArrayMarshalType_NativeType_IsEnabled &&
			(NativeType)ArrayMarshalType_NativeType.SelectedItem != dnlib.DotNet.NativeType.NotInitialized;
		public NullableCompressedUInt32 ArrayMarshalType_NumElems { get; }
		public bool ArrayMarshalType_NumElems_IsEnabled => ArrayMarshalType_ParamNum_IsEnabled && !ArrayMarshalType_ParamNum.IsNull;
		public NullableCompressedUInt32 ArrayMarshalType_Flags { get; }
		public bool ArrayMarshalType_Flags_IsEnabled => ArrayMarshalType_NumElems_IsEnabled && !ArrayMarshalType_NumElems.IsNull;

		public string CustomMarshalType_GUID {
			get { return customMarshalType_guid; }
			set {
				if (customMarshalType_guid != value) {
					customMarshalType_guid = value;
					OnPropertyChanged(nameof(CustomMarshalType_GUID));
					TypeStringUpdated();
				}
			}
		}
		string customMarshalType_guid;

		public bool CustomMarshalType_GUID_IsEnabled => IsEnabled;

		public string CustomMarshalType_NativeTypeName {
			get { return customMarshalType_nativeTypeName; }
			set {
				if (customMarshalType_nativeTypeName != value) {
					customMarshalType_nativeTypeName = value;
					OnPropertyChanged(nameof(CustomMarshalType_NativeTypeName));
					TypeStringUpdated();
				}
			}
		}
		string customMarshalType_nativeTypeName;

		public bool CustomMarshalType_NativeTypeName_IsEnabled => IsEnabled;
		public TypeSigCreatorVM CustomMarshalType_CustMarshaler_TypeSigCreator { get; }
		public bool CustomMarshalType_CustMarshaler_TypeSigCreator_IsEnabled => IsEnabled;

		public string CustomMarshalType_Cookie {
			get { return customMarshalType_cookie; }
			set {
				if (customMarshalType_cookie != value) {
					customMarshalType_cookie = value;
					OnPropertyChanged(nameof(CustomMarshalType_Cookie));
					TypeStringUpdated();
				}
			}
		}
		string customMarshalType_cookie;

		public bool CustomMarshalType_Cookie_IsEnabled => IsEnabled;
		public NullableCompressedUInt32 InterfaceMarshalType_IidParamIndex { get; }
		public bool InterfaceMarshalType_IidParamIndex_IsEnabled => IsEnabled;
		public string TypeString => NativeType.SelectedItem.ToString();
		void TypeStringUpdated() => OnPropertyChanged(nameof(TypeString));

		public MarshalTypeVM(ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef ownerType, MethodDef ownerMethod) {
			NativeType = new EnumListVM(nativeTypeList, (a, b) => { OnNativeTypeChanged(); TypeStringUpdated(); });
			FixNativeTypeEnum(NativeType, false);
			RawMarshalType_Data = new HexStringVM(a => { HasErrorUpdated(); TypeStringUpdated(); });
			FixedSysStringMarshalType_Size = new NullableCompressedUInt32(a => { HasErrorUpdated(); TypeStringUpdated(); });
			SafeArrayMarshalType_VariantType = new EnumListVM(variantTypeList, (a, b) => { OnSafeArrayMarshalTypeIsEnabledChanged(); TypeStringUpdated(); });
			SafeArrayMarshalType_UserDefinedSubType_TypeSigCreator = CreateTypeSigCreatorVM(ownerModule, decompilerService, ownerType, ownerMethod, true, safeArrayMarshalType_userDefinedSubType_typeSigCreator_PropertyChanged);
			FixedArrayMarshalType_Size = new NullableCompressedUInt32(a => { OnFixedArrayMarshalTypeIsEnabledChanged(); TypeStringUpdated(); });
			FixedArrayMarshalType_NativeType = new EnumListVM(nativeTypeList, (a, b) => { OnFixedArrayMarshalTypeIsEnabledChanged(); TypeStringUpdated(); });
			FixNativeTypeEnum(FixedArrayMarshalType_NativeType, true);
			ArrayMarshalType_NativeType = new EnumListVM(nativeTypeList, (a, b) => { OnArrayMarshalTypeIsEnabledChanged(); TypeStringUpdated(); });
			FixNativeTypeEnum(ArrayMarshalType_NativeType, true);
			ArrayMarshalType_ParamNum = new NullableCompressedUInt32(a => { OnArrayMarshalTypeIsEnabledChanged(); TypeStringUpdated(); });
			ArrayMarshalType_NumElems = new NullableCompressedUInt32(a => { OnArrayMarshalTypeIsEnabledChanged(); TypeStringUpdated(); });
			ArrayMarshalType_Flags = new NullableCompressedUInt32(a => { OnArrayMarshalTypeIsEnabledChanged(); TypeStringUpdated(); });
			CustomMarshalType_CustMarshaler_TypeSigCreator = CreateTypeSigCreatorVM(ownerModule, decompilerService, ownerType, ownerMethod, true, customMarshalType_custMarshaler_typeSigCreator_PropertyChanged);
			InterfaceMarshalType_IidParamIndex = new NullableCompressedUInt32(a => { HasErrorUpdated(); TypeStringUpdated(); });
		}

		void OnSafeArrayMarshalTypeIsEnabledChanged() {
			OnPropertyChanged(nameof(SafeArrayMarshalType_VT_IsEnabled));
			OnPropertyChanged(nameof(SafeArrayMarshalType_VT_Flags_IsEnabled));
			OnPropertyChanged(nameof(SafeArrayMarshalType_UserDefinedSubType_TypeSigCreator_IsEnabled));
			HasErrorUpdated();
			if (!SafeArrayMarshalType_UserDefinedSubType_TypeSigCreator_IsEnabled)
				SafeArrayMarshalType_UserDefinedSubType_TypeSigCreator.TypeSig = null;
		}

		void OnFixedArrayMarshalTypeIsEnabledChanged() {
			OnPropertyChanged(nameof(FixedArrayMarshalType_Size_IsEnabled));
			OnPropertyChanged(nameof(FixedArrayMarshalType_NativeType_IsEnabled));
			HasErrorUpdated();
			if (!FixedArrayMarshalType_NativeType_IsEnabled)
				FixedArrayMarshalType_NativeType.SelectedItem = dnlib.DotNet.NativeType.NotInitialized;
		}

		void OnArrayMarshalTypeIsEnabledChanged() {
			OnPropertyChanged(nameof(ArrayMarshalType_NativeType_IsEnabled));
			OnPropertyChanged(nameof(ArrayMarshalType_ParamNum_IsEnabled));
			OnPropertyChanged(nameof(ArrayMarshalType_NumElems_IsEnabled));
			OnPropertyChanged(nameof(ArrayMarshalType_Flags_IsEnabled));
			HasErrorUpdated();
			if (!ArrayMarshalType_ParamNum_IsEnabled)
				ArrayMarshalType_ParamNum.Value = null;
			if (!ArrayMarshalType_NumElems_IsEnabled)
				ArrayMarshalType_NumElems.Value = null;
			if (!ArrayMarshalType_Flags_IsEnabled)
				ArrayMarshalType_Flags.Value = null;
		}

		public string SafeArrayMarshalType_UserDefinedSubType_TypeHeader =>
			string.Format(dnSpy_AsmEditor_Resources.Type, SafeArrayMarshalType_UserDefinedSubType_TypeSigCreator.TypeSigDnlibFullName);
		public string CustomMarshalType_CustMarshaler_TypeHeader =>
			string.Format(dnSpy_AsmEditor_Resources.Type, CustomMarshalType_CustMarshaler_TypeSigCreator.TypeSigDnlibFullName);

		void safeArrayMarshalType_userDefinedSubType_typeSigCreator_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(SafeArrayMarshalType_UserDefinedSubType_TypeSigCreator.TypeSigDnlibFullName))
				OnPropertyChanged(nameof(SafeArrayMarshalType_UserDefinedSubType_TypeHeader));
			OnSafeArrayMarshalTypeIsEnabledChanged();
			TypeStringUpdated();
			HasErrorUpdated();
		}

		void customMarshalType_custMarshaler_typeSigCreator_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(CustomMarshalType_CustMarshaler_TypeSigCreator.TypeSigDnlibFullName))
				OnPropertyChanged(nameof(CustomMarshalType_CustMarshaler_TypeHeader));
			TypeStringUpdated();
			HasErrorUpdated();
		}

		static TypeSigCreatorVM CreateTypeSigCreatorVM(ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef ownerType, MethodDef ownerMethod, bool allowNullTypeSig, PropertyChangedEventHandler handler) {
			var typeSigCreatorOptions = new TypeSigCreatorOptions(ownerModule, decompilerService) {
				IsLocal = false,
				CanAddGenericTypeVar = true,
				CanAddGenericMethodVar = false,
				OwnerType = ownerType,
				OwnerMethod = ownerMethod,
				NullTypeSigAllowed = allowNullTypeSig,
			};
			if (ownerType != null && ownerType.GenericParameters.Count == 0)
				typeSigCreatorOptions.CanAddGenericTypeVar = false;
			if (ownerMethod != null && ownerMethod.GenericParameters.Count > 0)
				typeSigCreatorOptions.CanAddGenericMethodVar = true;
			var typeSigCreator = new TypeSigCreatorVM(typeSigCreatorOptions);
			typeSigCreator.PropertyChanged += handler;
			return typeSigCreator;
		}

		static void FixNativeTypeEnum(EnumListVM e, bool canHaveNotInitialized) {
			e.Items.RemoveAt(e.GetIndex(dnlib.DotNet.NativeType.NotInitialized));
			if (canHaveNotInitialized)
				e.Items.Insert(0, new EnumVM(dnlib.DotNet.NativeType.NotInitialized, dnSpy_AsmEditor_Resources.MarshalType_EnumNotInitialized));
		}

		void OnNativeTypeChanged() {
			OnPropertyChanged(nameof(IsMarshalType));
			OnPropertyChanged(nameof(IsRawMarshalType));
			OnPropertyChanged(nameof(IsFixedSysStringMarshalType));
			OnPropertyChanged(nameof(IsSafeArrayMarshalType));
			OnPropertyChanged(nameof(IsFixedArrayMarshalType));
			OnPropertyChanged(nameof(IsArrayMarshalType));
			OnPropertyChanged(nameof(IsCustomMarshalType));
			OnPropertyChanged(nameof(IsInterfaceMarshalType));
			HasErrorUpdated();
		}

		public override bool HasError {
			get {
				if (!IsEnabled)
					return false;

				if (IsRawMarshalType) {
					if (RawMarshalType_Data_IsEnabled && RawMarshalType_Data.HasError) return true;
				}
				else if (IsFixedSysStringMarshalType) {
					if (FixedSysStringMarshalType_Size_IsEnabled && FixedSysStringMarshalType_Size.HasError) return true;
				}
				else if (IsSafeArrayMarshalType) {
					if (SafeArrayMarshalType_UserDefinedSubType_TypeSigCreator_IsEnabled && SafeArrayMarshalType_UserDefinedSubType_TypeSigCreator.HasError) return true;
				}
				else if (IsFixedArrayMarshalType) {
					if (FixedArrayMarshalType_Size_IsEnabled && FixedArrayMarshalType_Size.HasError) return true;
				}
				else if (IsArrayMarshalType) {
					if (ArrayMarshalType_ParamNum_IsEnabled && ArrayMarshalType_ParamNum.HasError) return true;
					if (ArrayMarshalType_NumElems_IsEnabled && ArrayMarshalType_NumElems.HasError) return true;
					if (ArrayMarshalType_Flags_IsEnabled && ArrayMarshalType_Flags.HasError) return true;
				}
				else if (IsCustomMarshalType) {
					if (CustomMarshalType_CustMarshaler_TypeSigCreator_IsEnabled && CustomMarshalType_CustMarshaler_TypeSigCreator.HasError) return true;
				}
				else if (IsInterfaceMarshalType) {
					if (InterfaceMarshalType_IidParamIndex_IsEnabled && InterfaceMarshalType_IidParamIndex.HasError) return true;
				}
				else if (IsMarshalType) {
				}
				else
					throw new InvalidOperationException();

				return false;
			}
		}

		public MarshalType Type {
			get {
				if (!IsEnabled)
					return null;

				if (IsRawMarshalType) {
					return new RawMarshalType(RawMarshalType_Data.Value.ToArray());
				}
				else if (IsFixedSysStringMarshalType) {
					int size = FixedSysStringMarshalType_Size_IsEnabled && !FixedSysStringMarshalType_Size.IsNull ?
						(int)FixedSysStringMarshalType_Size.Value : -1;
					return new FixedSysStringMarshalType(size);
				}
				else if (IsSafeArrayMarshalType) {
					var vt = (dnlib.DotNet.VariantType)SafeArrayMarshalType_VariantType.SelectedItem == VariantType.NotInitialized ?
						VariantType.NotInitialized : SafeArrayMarshalType_VT;
					var userType = SafeArrayMarshalType_UserDefinedSubType_TypeSigCreator.TypeSig.ToTypeDefOrRef();
					return new SafeArrayMarshalType(vt, userType);
				}
				else if (IsFixedArrayMarshalType) {
					int size = FixedArrayMarshalType_Size_IsEnabled && !FixedArrayMarshalType_Size.IsNull ?
						(int)FixedArrayMarshalType_Size.Value : -1;
					var nt = (dnlib.DotNet.NativeType)FixedArrayMarshalType_NativeType.SelectedItem;
					return new FixedArrayMarshalType(size, nt);
				}
				else if (IsArrayMarshalType) {
					var nt = (dnlib.DotNet.NativeType)ArrayMarshalType_NativeType.SelectedItem;
					int paramNum = ArrayMarshalType_ParamNum_IsEnabled && !ArrayMarshalType_ParamNum.IsNull ?
						(int)ArrayMarshalType_ParamNum.Value : -1;
					int numElems = ArrayMarshalType_NumElems_IsEnabled && !ArrayMarshalType_NumElems.IsNull ?
						(int)ArrayMarshalType_NumElems.Value : -1;
					int flags = ArrayMarshalType_Flags_IsEnabled && !ArrayMarshalType_Flags.IsNull ?
						(int)ArrayMarshalType_Flags.Value : -1;
					return new ArrayMarshalType(nt, paramNum, numElems, flags);
				}
				else if (IsCustomMarshalType) {
					return new CustomMarshalType(
						CustomMarshalType_GUID,
						CustomMarshalType_NativeTypeName,
						CustomMarshalType_CustMarshaler_TypeSigCreator.TypeSig.ToTypeDefOrRef(),
						CustomMarshalType_Cookie);
				}
				else if (IsInterfaceMarshalType) {
					int iidParamIndex = InterfaceMarshalType_IidParamIndex_IsEnabled && !InterfaceMarshalType_IidParamIndex.IsNull ?
						(int)InterfaceMarshalType_IidParamIndex.Value : -1;
					return new InterfaceMarshalType((dnlib.DotNet.NativeType)NativeType.SelectedItem, iidParamIndex);
				}
				else if (IsMarshalType) {
					return new MarshalType((dnlib.DotNet.NativeType)NativeType.SelectedItem);
				}
				else
					throw new InvalidOperationException();
			}
			set {
				IsEnabled = value != null;
				if (value == null)
					return;

				switch (value.NativeType) {
				case dnlib.DotNet.NativeType.RawBlob:
					RawMarshalType_Data.Value = ((RawMarshalType)value).Data;
					break;

				case dnlib.DotNet.NativeType.FixedSysString:
					var fixedStr = (FixedSysStringMarshalType)value;
					FixedSysStringMarshalType_Size.Value = !fixedStr.IsSizeValid ? (uint?)null : (uint)fixedStr.Size;
					break;

				case dnlib.DotNet.NativeType.SafeArray:
					var safeAry = (SafeArrayMarshalType)value;
					if (safeAry.IsVariantTypeValid) {
						SafeArrayMarshalType_VariantType.SelectedItem = safeAry.VariantType & VariantType.TypeMask;
						SafeArrayMarshalType_VT = safeAry.VariantType;
					}
					else {
						SafeArrayMarshalType_VariantType.SelectedItem = safeAry.VariantType;
						SafeArrayMarshalType_VT = 0;
					}
					SafeArrayMarshalType_UserDefinedSubType_TypeSigCreator.TypeSig = safeAry.UserDefinedSubType.ToTypeSig();
					break;

				case dnlib.DotNet.NativeType.FixedArray:
					var fixedAry = (FixedArrayMarshalType)value;
					FixedArrayMarshalType_Size.Value = !fixedAry.IsSizeValid ? (uint?)null : (uint)fixedAry.Size;
					FixedArrayMarshalType_NativeType.SelectedItem = fixedAry.ElementType;
					break;

				case dnlib.DotNet.NativeType.Array:
					var ary = (ArrayMarshalType)value;
					ArrayMarshalType_NativeType.SelectedItem = ary.ElementType;
					ArrayMarshalType_ParamNum.Value = !ary.IsParamNumberValid ? (uint?)null : (uint)ary.ParamNumber;
					ArrayMarshalType_NumElems.Value = !ary.IsSizeValid ? (uint?)null : (uint)ary.Size;
					ArrayMarshalType_Flags.Value = !ary.IsFlagsValid ? (uint?)null : (uint)ary.Flags;
					break;

				case dnlib.DotNet.NativeType.CustomMarshaler:
					var cust = (CustomMarshalType)value;
					CustomMarshalType_GUID = cust.Guid;
					CustomMarshalType_NativeTypeName = cust.NativeTypeName;
					CustomMarshalType_CustMarshaler_TypeSigCreator.TypeSig = cust.CustomMarshaler.ToTypeSig();
					CustomMarshalType_Cookie = cust.Cookie;
					break;

				case dnlib.DotNet.NativeType.IUnknown:
				case dnlib.DotNet.NativeType.IDispatch:
				case dnlib.DotNet.NativeType.IntF:
					var iface = (InterfaceMarshalType)value;
					InterfaceMarshalType_IidParamIndex.Value = !iface.IsIidParamIndexValid ? (uint?)null : (uint)iface.IidParamIndex;
					break;

				default:
					break;
				}

				NativeType.SelectedItem = value.NativeType;
			}
		}
	}
}
