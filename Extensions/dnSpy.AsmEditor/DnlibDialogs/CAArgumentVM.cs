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
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class CAArgumentVM : ViewModelBase {
		public ConstantTypeVM ConstantTypeVM { get; }

		public bool IsEnabled {
			get => isEnabled;
			set {
				if (isEnabled != value) {
					isEnabled = value;
					OnPropertyChanged(nameof(IsEnabled));
					HasErrorUpdated();
				}
			}
		}
		bool isEnabled = true;

		static readonly ConstantType[] ConstantTypes = new ConstantType[] {
			ConstantType.Null,
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
		readonly CAArgument originalArg;
		readonly ModuleDef module;

		public TypeSig StorageType {
			get => storageType;
			set {
				if (storageType != value) {
					storageType = value;
					OnPropertyChanged(nameof(StorageType));
				}
			}
		}
		TypeSig storageType;

		public CAArgumentVM(ModuleDef ownerModule, CAArgument arg, TypeSigCreatorOptions options, TypeSig storageType) {
			module = options.OwnerModule;
			originalArg = arg.Clone();
			ConstantTypeVM = new DnlibDialogs.ConstantTypeVM(ownerModule, null, ConstantTypes, true, true, options);
			ConstantTypeVM.PropertyChanged += ConstantTypeVM_PropertyChanged;
			InitializeFrom(arg, storageType);
			modified = false;
		}

		void ConstantTypeVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "Modified") {
				OnPropertyChanged("Modified");
				modified = true;
			}
			HasErrorUpdated();
		}

		void InitializeFrom(CAArgument arg, TypeSig storageType) {
			StorageType = storageType;
			ConstantTypeVM.Value = ConvertFromModel(arg.Type, arg.Value);
		}

		object ConvertFromModel(TypeSig valueType, object value) {
			var type = valueType.RemovePinnedAndModifiers();
			var et = type.GetElementType();
			ITypeDefOrRef tdr;
			TypeDef td;
			switch (et) {
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
				if (ModelUtils.GetElementType(value?.GetType()) == et)
					return value;
				break;

			case ElementType.String:
				if (value == null)
					return Null<string>.Instance;
				else if (value is string)
					return value;
				else if (value is UTF8String)
					return ((UTF8String)value).String;
				break;

			case ElementType.ValueType:
			case ElementType.Class:
				tdr = ((ClassOrValueTypeSig)type).TypeDefOrRef;
				if (tdr.IsSystemType()) {
					if (value == null)
						return Null<TypeSig>.Instance;
					return value;
				}
				td = tdr.ResolveTypeDef();
				if (td != null && !td.IsEnum)
					break;
				return new EnumInfo() {
					EnumType = tdr,
					Value = value,
					IsArray = false,
				};

			case ElementType.SZArray:
				var elemType = type.Next.RemovePinnedAndModifiers();
				if (value == null) {
					switch (elemType.GetElementType()) {
					case ElementType.Boolean:	return Null<bool[]>.Instance;
					case ElementType.Char:		return Null<char[]>.Instance;
					case ElementType.I1:		return Null<sbyte[]>.Instance;
					case ElementType.U1:		return Null<byte[]>.Instance;
					case ElementType.I2:		return Null<short[]>.Instance;
					case ElementType.U2:		return Null<ushort[]>.Instance;
					case ElementType.I4:		return Null<int[]>.Instance;
					case ElementType.U4:		return Null<uint[]>.Instance;
					case ElementType.I8:		return Null<long[]>.Instance;
					case ElementType.U8:		return Null<ulong[]>.Instance;
					case ElementType.R4:		return Null<float[]>.Instance;
					case ElementType.R8:		return Null<double[]>.Instance;
					case ElementType.String:	return Null<string[]>.Instance;
					case ElementType.Object:	return Null<object[]>.Instance;
					case ElementType.ValueType:
					case ElementType.Class:
						tdr = ((ClassOrValueTypeSig)elemType).TypeDefOrRef;
						if (tdr.IsSystemType())
							return Null<Type[]>.Instance;
						td = tdr.ResolveTypeDef();
						if (td != null && !td.IsEnum)
							break;
						return EnumInfo.CreateNullArray(tdr);
					}
					break;
				}
				var oldList = value as IList<CAArgument>;
				if (oldList == null)
					break;

				switch (elemType.GetElementType()) {
				case ElementType.Boolean:	return ConvertArray<bool>(elemType, oldList);
				case ElementType.Char:		return ConvertArray<char>(elemType, oldList);
				case ElementType.I1:		return ConvertArray<sbyte>(elemType, oldList);
				case ElementType.U1:		return ConvertArray<byte>(elemType, oldList);
				case ElementType.I2:		return ConvertArray<short>(elemType, oldList);
				case ElementType.U2:		return ConvertArray<ushort>(elemType, oldList);
				case ElementType.I4:		return ConvertArray<int>(elemType, oldList);
				case ElementType.U4:		return ConvertArray<uint>(elemType, oldList);
				case ElementType.I8:		return ConvertArray<long>(elemType, oldList);
				case ElementType.U8:		return ConvertArray<ulong>(elemType, oldList);
				case ElementType.R4:		return ConvertArray<float>(elemType, oldList);
				case ElementType.R8:		return ConvertArray<double>(elemType, oldList);
				case ElementType.String:	return ConvertArray<string>(elemType, oldList);
				case ElementType.Object:	return ConvertArray<object>(elemType, oldList);
				case ElementType.ValueType:
				case ElementType.Class:
					tdr = ((ClassOrValueTypeSig)elemType).TypeDefOrRef;
					if (tdr.IsSystemType())
						return ConvertArray<TypeSig>(elemType, oldList);
					td = tdr.ResolveTypeDef();
					if (td != null && !td.IsEnum)
						break;
					return ConvertEnum(elemType, oldList);
				}
				break;

			default:
				break;
			}
			return value;
		}

		object ConvertEnum(TypeSig elemType, IList<CAArgument> oldList) {
			var td = elemType.ScopeType.ResolveTypeDef();
			ElementType underlyingElemType = ElementType.End;
			if (td != null && td.IsEnum)
				underlyingElemType = td.GetEnumUnderlyingType().RemovePinnedAndModifiers().GetElementType();
			if (!(ElementType.Boolean <= underlyingElemType && underlyingElemType <= ElementType.R8)) {
				if (oldList.Count > 0 && oldList[0].Value != null)
					underlyingElemType = ModelUtils.GetElementType(oldList[0].Value.GetType());
			}

			switch (underlyingElemType) {
			case ElementType.Boolean:	return ConvertEnum<bool>(elemType, oldList);
			case ElementType.Char:		return ConvertEnum<char>(elemType, oldList);
			case ElementType.I1:		return ConvertEnum<sbyte>(elemType, oldList);
			case ElementType.U1:		return ConvertEnum<byte>(elemType, oldList);
			case ElementType.I2:		return ConvertEnum<short>(elemType, oldList);
			case ElementType.U2:		return ConvertEnum<ushort>(elemType, oldList);
			case ElementType.I4:		return ConvertEnum<int>(elemType, oldList);
			case ElementType.U4:		return ConvertEnum<uint>(elemType, oldList);
			case ElementType.I8:		return ConvertEnum<long>(elemType, oldList);
			case ElementType.U8:		return ConvertEnum<ulong>(elemType, oldList);
			case ElementType.R4:		return ConvertEnum<float>(elemType, oldList);
			case ElementType.R8:		return ConvertEnum<double>(elemType, oldList);
			}
			return Array.Empty<int>();
		}

		object ConvertEnum<T>(TypeSig elemType, IList<CAArgument> oldList) {
			var ary = ConvertArray<EnumInfo>(elemType, oldList);
			var list = new T[ary.Length];

			var sigComparer = new SigComparer(SigComparerOptions.CompareAssemblyPublicKeyToken);
			for (int i = 0; i < list.Length; i++) {
				if (ary[i].Value is T && sigComparer.Equals(elemType, ary[i].EnumType))
					list[i] = (T)ary[i].Value;
			}

			return new EnumInfo {
				EnumType = elemType.ToTypeDefOrRef(),
				Value = list,
				IsArray = true,
			};
		}

		T[] ConvertArray<T>(TypeSig elemType, IList<CAArgument> oldList) {
			var list = new T[oldList.Count];

			bool tIsValueType = typeof(T).IsValueType;
			bool tIsSystemObject = typeof(T) == typeof(object);

			var sigComparer = new SigComparer(SigComparerOptions.CompareAssemblyPublicKeyToken);
			for (int i = 0; i < list.Length; i++) {
				var arg = oldList[i];
				if (!tIsSystemObject && !sigComparer.Equals(elemType, arg.Type))
					return Array.Empty<T>();
				var res = ConvertFromModel(arg.Type, arg.Value);
				if (res is T)
					list[i] = (T)res;
				else if (!tIsValueType && (res == null || res == Null<T>.Instance)) {
					object n = null;
					list[i] = (T)n;
				}
				else
					return Array.Empty<T>();
			}

			return list;
		}

		public CAArgument CreateCAArgument(TypeSig ownerType) {
			if (!modified)
				return originalArg.Clone();
			return CreateCAArgument(ownerType, ConstantTypeVM.Value);
		}

		CAArgument CreateCAArgument(TypeSig ownerType, object value) {
			if (value == null || value is Null) {
				var t = ownerType.RemovePinnedAndModifiers();
				t = t is SZArraySig ? t.Next : t;
				if (t.RemovePinnedAndModifiers().GetElementType() == ElementType.Object)
					return new CAArgument(module.CorLibTypes.String, null);
				return new CAArgument(ownerType, null);
			}

			switch (ModelUtils.GetElementType(value.GetType())) {
			case ElementType.Boolean:return new CAArgument(module.CorLibTypes.Boolean, value);
			case ElementType.Char:	return new CAArgument(module.CorLibTypes.Char, value);
			case ElementType.I1:	return new CAArgument(module.CorLibTypes.SByte, value);
			case ElementType.U1:	return new CAArgument(module.CorLibTypes.Byte, value);
			case ElementType.I2:	return new CAArgument(module.CorLibTypes.Int16, value);
			case ElementType.U2:	return new CAArgument(module.CorLibTypes.UInt16, value);
			case ElementType.I4:	return new CAArgument(module.CorLibTypes.Int32, value);
			case ElementType.U4:	return new CAArgument(module.CorLibTypes.UInt32, value);
			case ElementType.I8:	return new CAArgument(module.CorLibTypes.Int64, value);
			case ElementType.U8:	return new CAArgument(module.CorLibTypes.UInt64, value);
			case ElementType.R4:	return new CAArgument(module.CorLibTypes.Single, value);
			case ElementType.R8:	return new CAArgument(module.CorLibTypes.Double, value);
			case ElementType.String:return new CAArgument(module.CorLibTypes.String, new UTF8String((string)value));
			}
			if (value is TypeSig)
				return new CAArgument(new ClassSig(module.CorLibTypes.GetTypeRef("System", "Type")), value);

			if (value is EnumInfo enumInfo) {
				var enumSig = enumInfo.EnumType.ToTypeSig();
				if (!enumInfo.IsArray)
					return new CAArgument(enumSig, enumInfo.Value);
				var res = CreateArray(enumSig, enumInfo.Value);
				var list = (IList<CAArgument>)res.Value;
				if (list != null) {
					for (int i = 0; i < list.Count; i++)
						list[i] = new CAArgument(enumSig, list[i].Value);
				}
				return res;
			}

			var valueType = value.GetType();
			if (value is IList<bool>)
				return CreateArray(module.CorLibTypes.Boolean, value);
			if (value is IList<char>)
				return CreateArray(module.CorLibTypes.Char, value);
			if (value is IList<sbyte> && valueType != typeof(byte[]))
				return CreateArray(module.CorLibTypes.SByte, value);
			if (value is IList<short> && valueType != typeof(ushort[]))
				return CreateArray(module.CorLibTypes.Int16, value);
			if (value is IList<int> && valueType != typeof(uint[]))
				return CreateArray(module.CorLibTypes.Int32, value);
			if (value is IList<long> && valueType != typeof(ulong[]))
				return CreateArray(module.CorLibTypes.Int64, value);
			if (value is IList<byte> && valueType != typeof(sbyte[]))
				return CreateArray(module.CorLibTypes.Byte, value);
			if (value is IList<ushort> && valueType != typeof(short[]))
				return CreateArray(module.CorLibTypes.UInt16, value);
			if (value is IList<uint> && valueType != typeof(int[]))
				return CreateArray(module.CorLibTypes.UInt32, value);
			if (value is IList<ulong> && valueType != typeof(long[]))
				return CreateArray(module.CorLibTypes.UInt64, value);
			if (value is IList<float>)
				return CreateArray(module.CorLibTypes.Single, value);
			if (value is IList<double>)
				return CreateArray(module.CorLibTypes.Double, value);
			if (value is IList<string>)
				return CreateArray(module.CorLibTypes.String, value);
			if (value is IList<TypeSig>)
				return CreateArray(new ClassSig(module.CorLibTypes.GetTypeRef("System", "Type")), value);
			if (value is IList<object>)
				return CreateArray(module.CorLibTypes.Object, value);

			Debug.Fail($"Unknown CA arg: {value}, ownerType: {ownerType}");
			return new CAArgument();
		}

		CAArgument CreateArray(TypeSig elemType, object value) {
			var aryType = new SZArraySig(elemType);
			var list = value as System.Collections.IList;
			Debug.Assert(list != null || value == null);
			if (list == null)
				return new CAArgument(aryType, null);
			var ary = new List<CAArgument>(list.Count);

			for (int i = 0; i < list.Count; i++)
				ary.Add(CreateCAArgument(elemType, list[i]));

			return new CAArgument(aryType, ary);
		}

		public override string ToString() {
			if (ConstantTypeVM.HasError)
				return dnSpy_AsmEditor_Resources.Error;
			return DlgUtils.ValueToString(ConstantTypeVM.Value, StorageType);
		}

		public override bool HasError => IsEnabled && ConstantTypeVM.HasError;
	}
}
