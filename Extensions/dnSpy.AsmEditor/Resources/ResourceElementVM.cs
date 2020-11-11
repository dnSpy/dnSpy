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
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.Resources {
	enum ResourceElementType {
		Null			= ResourceTypeCode.Null,
		String			= ResourceTypeCode.String,
		Boolean			= ResourceTypeCode.Boolean,
		Char			= ResourceTypeCode.Char,
		Byte			= ResourceTypeCode.Byte,
		SByte			= ResourceTypeCode.SByte,
		Int16			= ResourceTypeCode.Int16,
		UInt16			= ResourceTypeCode.UInt16,
		Int32			= ResourceTypeCode.Int32,
		UInt32			= ResourceTypeCode.UInt32,
		Int64			= ResourceTypeCode.Int64,
		UInt64			= ResourceTypeCode.UInt64,
		Single			= ResourceTypeCode.Single,
		Double			= ResourceTypeCode.Double,
		Decimal			= ResourceTypeCode.Decimal,
		DateTime		= ResourceTypeCode.DateTime,
		TimeSpan		= ResourceTypeCode.TimeSpan,
		ByteArray		= ResourceTypeCode.ByteArray,
		Stream			= ResourceTypeCode.Stream,
		SerializedType	= ResourceTypeCode.UserTypes,
	}

	sealed class ResourceElementVM : ViewModelBase {
		readonly ResourceElementOptions origOptions;

		public IOpenFile OpenFile {
			set => openFile = value;
		}
		IOpenFile? openFile;

		public IDnlibTypePicker DnlibTypePicker {
			set => UserTypeVM.DnlibTypePicker = value;
		}

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public ICommand PickRawBytesCommand => new RelayCommand(a => PickRawBytes(), a => IsRawBytes);

		public bool CanChangeType {
			get => canChangeType;
			set {
				if (canChangeType != value) {
					canChangeType = value;
					OnPropertyChanged(nameof(CanChangeType));
				}
			}
		}
		bool canChangeType = true;

		internal static readonly EnumVM[] resourceElementTypeList = EnumVM.Create(false, typeof(ResourceElementType));
		public EnumListVM ResourceElementTypeVM { get; }

		public string? Name {
			get => name;
			set {
				if (name != value) {
					name = value;
					OnPropertyChanged(nameof(Name));
				}
			}
		}
		UTF8String? name;

		public string? String {
			get => @string;
			set {
				if (@string != value) {
					@string = value;
					OnPropertyChanged(nameof(String));
				}
			}
		}
		string? @string;

		public BooleanVM BooleanVM { get; }
		public CharVM CharVM { get; }
		public ByteVM ByteVM { get; }
		public SByteVM SByteVM { get; }
		public Int16VM Int16VM { get; }
		public UInt16VM UInt16VM { get; }
		public Int32VM Int32VM { get; }
		public UInt32VM UInt32VM { get; }
		public Int64VM Int64VM { get; }
		public UInt64VM UInt64VM { get; }
		public SingleVM SingleVM { get; }
		public DoubleVM DoubleVM { get; }
		public DecimalVM DecimalVM { get; }
		public DateTimeVM DateTimeVM { get; }
		public TimeSpanVM TimeSpanVM { get; }

		public byte[]? Data {
			get => data;
			set {
				if (data != value) {
					data = value;
					OnPropertyChanged(nameof(Data));
					OnPropertyChanged(nameof(DataString));
				}
			}
		}
		byte[]? data;

		public string DataString => string.Format(dnSpy_AsmEditor_Resources.XBytes, Data is null ? 0 : Data.Length);
		public UserTypeVM UserTypeVM { get; }

		public object? ValueVM {
			get {
				switch ((ResourceElementType)ResourceElementTypeVM.SelectedItem!) {
				case ResourceElementType.Null:		return null;
				case ResourceElementType.String:	return null;
				case ResourceElementType.Boolean:	return BooleanVM;
				case ResourceElementType.Char:		return CharVM;
				case ResourceElementType.Byte:		return ByteVM;
				case ResourceElementType.SByte:		return SByteVM;
				case ResourceElementType.Int16:		return Int16VM;
				case ResourceElementType.UInt16:	return UInt16VM;
				case ResourceElementType.Int32:		return Int32VM;
				case ResourceElementType.UInt32:	return UInt32VM;
				case ResourceElementType.Int64:		return Int64VM;
				case ResourceElementType.UInt64:	return UInt64VM;
				case ResourceElementType.Single:	return SingleVM;
				case ResourceElementType.Double:	return DoubleVM;
				case ResourceElementType.Decimal:	return DecimalVM;
				case ResourceElementType.DateTime:	return DateTimeVM;
				case ResourceElementType.TimeSpan:	return TimeSpanVM;
				case ResourceElementType.ByteArray:	return null;
				case ResourceElementType.Stream:	return null;
				case ResourceElementType.SerializedType: return UserTypeVM;
				default: throw new InvalidOperationException();
				}
			}
		}

		public bool IsSerializedType => (ResourceElementType)ResourceElementTypeVM.SelectedItem! == ResourceElementType.SerializedType;
		public bool IsSingleLineValue => !IsMultiLineValue && !IsRawBytes && (ResourceElementType)ResourceElementTypeVM.SelectedItem! != ResourceElementType.Null;
		public bool IsMultiLineValue => (ResourceElementType)ResourceElementTypeVM.SelectedItem! == ResourceElementType.String;

		public bool IsRawBytes {
			get {
				var code = (ResourceElementType)ResourceElementTypeVM.SelectedItem!;
				return code == ResourceElementType.ByteArray || code == ResourceElementType.Stream;
			}
		}

		readonly bool canDeserialize;

		public ResourceElementVM(ResourceElementOptions options, ModuleDef ownerModule, bool canDeserialize) {
			origOptions = options;
			this.canDeserialize = canDeserialize;

			BooleanVM = new BooleanVM(a => HasErrorUpdated());
			CharVM = new CharVM(a => HasErrorUpdated());
			ByteVM = new ByteVM(a => HasErrorUpdated());
			SByteVM = new SByteVM(a => HasErrorUpdated());
			Int16VM = new Int16VM(a => HasErrorUpdated());
			UInt16VM = new UInt16VM(a => HasErrorUpdated());
			Int32VM = new Int32VM(a => HasErrorUpdated());
			UInt32VM = new UInt32VM(a => HasErrorUpdated());
			Int64VM = new Int64VM(a => HasErrorUpdated());
			UInt64VM = new UInt64VM(a => HasErrorUpdated());
			SingleVM = new SingleVM(a => HasErrorUpdated());
			DoubleVM = new DoubleVM(a => HasErrorUpdated());
			DecimalVM = new DecimalVM(a => HasErrorUpdated());
			DateTimeVM = new DateTimeVM(a => HasErrorUpdated());
			TimeSpanVM = new TimeSpanVM(a => HasErrorUpdated());
			UserTypeVM = new UserTypeVM(ownerModule, canDeserialize);
			ResourceElementTypeVM = new EnumListVM(resourceElementTypeList, (a, b) => OnResourceElementTypeChanged());

			UserTypeVM.PropertyChanged += (s, e) => {
				if (e.PropertyName == nameof(UserTypeVM.HasError))
					HasErrorUpdated();
			};

			Reinitialize();
		}

		void PickRawBytes() {
			if (openFile is null)
				throw new InvalidOperationException();
			var newBytes = openFile.Open();
			if (newBytes is not null)
				Data = newBytes;
		}

		void OnResourceElementTypeChanged() {
			OnPropertyChanged(nameof(ValueVM));
			OnPropertyChanged(nameof(IsSerializedType));
			OnPropertyChanged(nameof(IsSingleLineValue));
			OnPropertyChanged(nameof(IsMultiLineValue));
			OnPropertyChanged(nameof(IsRawBytes));
			HasErrorUpdated();
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public ResourceElementOptions CreateResourceElementOptions() => CopyTo(new ResourceElementOptions());

		void InitializeFrom(ResourceElementOptions options) {
			Name = options.Name;
			var code = Convert(options.ResourceData!.Code);
			var builtin = options.ResourceData as BuiltInResourceData;
			ResourceElementTypeVM.SelectedItem = code;
			switch (code) {
			case ResourceElementType.Null:		break;
			case ResourceElementType.String:	String = (string)builtin!.Data; break;
			case ResourceElementType.Boolean:	BooleanVM.Value = (bool)builtin!.Data; break;
			case ResourceElementType.Char:		CharVM.Value = (char)builtin!.Data; break;
			case ResourceElementType.Byte:		ByteVM.Value = (byte)builtin!.Data; break;
			case ResourceElementType.SByte:		SByteVM.Value = (sbyte)builtin!.Data; break;
			case ResourceElementType.Int16:		Int16VM.Value = (short)builtin!.Data; break;
			case ResourceElementType.UInt16:	UInt16VM.Value = (ushort)builtin!.Data; break;
			case ResourceElementType.Int32:		Int32VM.Value = (int)builtin!.Data; break;
			case ResourceElementType.UInt32:	UInt32VM.Value = (uint)builtin!.Data; break;
			case ResourceElementType.Int64:		Int64VM.Value = (long)builtin!.Data; break;
			case ResourceElementType.UInt64:	UInt64VM.Value = (ulong)builtin!.Data; break;
			case ResourceElementType.Single:	SingleVM.Value = (float)builtin!.Data; break;
			case ResourceElementType.Double:	DoubleVM.Value = (double)builtin!.Data; break;
			case ResourceElementType.Decimal:	DecimalVM.Value = (decimal)builtin!.Data; break;
			case ResourceElementType.DateTime:	DateTimeVM.Value = (DateTime)builtin!.Data; break;
			case ResourceElementType.TimeSpan:	TimeSpanVM.Value = (TimeSpan)builtin!.Data; break;
			case ResourceElementType.ByteArray:	Data = (byte[])builtin!.Data; break;
			case ResourceElementType.Stream:	Data = (byte[])builtin!.Data; break;

			case ResourceElementType.SerializedType:
				var binRes = (BinaryResourceData)options.ResourceData;
				UserTypeVM.TypeFullName = binRes.TypeName;
				UserTypeVM.SetData(binRes.Data);
				break;

			default: throw new InvalidOperationException();
			}
		}

		static ResourceElementType Convert(ResourceTypeCode code) {
			if (code >= ResourceTypeCode.UserTypes)
				return ResourceElementType.SerializedType;
			return (ResourceElementType)code;
		}

		ResourceElementOptions CopyTo(ResourceElementOptions options) {
			options.Name = Name;
			options.ResourceData = CreateResourceData();
			return options;
		}

		IResourceData CreateResourceData() {
			var code = (ResourceElementType)ResourceElementTypeVM.SelectedItem!;
			switch (code) {
			case ResourceElementType.Null:		return new BuiltInResourceData((ResourceTypeCode)code, null);
			case ResourceElementType.String:	return new BuiltInResourceData((ResourceTypeCode)code, String);
			case ResourceElementType.Boolean:	return new BuiltInResourceData((ResourceTypeCode)code, BooleanVM.Value);
			case ResourceElementType.Char:		return new BuiltInResourceData((ResourceTypeCode)code, CharVM.Value);
			case ResourceElementType.Byte:		return new BuiltInResourceData((ResourceTypeCode)code, ByteVM.Value);
			case ResourceElementType.SByte:		return new BuiltInResourceData((ResourceTypeCode)code, SByteVM.Value);
			case ResourceElementType.Int16:		return new BuiltInResourceData((ResourceTypeCode)code, Int16VM.Value);
			case ResourceElementType.UInt16:	return new BuiltInResourceData((ResourceTypeCode)code, UInt16VM.Value);
			case ResourceElementType.Int32:		return new BuiltInResourceData((ResourceTypeCode)code, Int32VM.Value);
			case ResourceElementType.UInt32:	return new BuiltInResourceData((ResourceTypeCode)code, UInt32VM.Value);
			case ResourceElementType.Int64:		return new BuiltInResourceData((ResourceTypeCode)code, Int64VM.Value);
			case ResourceElementType.UInt64:	return new BuiltInResourceData((ResourceTypeCode)code, UInt64VM.Value);
			case ResourceElementType.Single:	return new BuiltInResourceData((ResourceTypeCode)code, SingleVM.Value);
			case ResourceElementType.Double:	return new BuiltInResourceData((ResourceTypeCode)code, DoubleVM.Value);
			case ResourceElementType.Decimal:	return new BuiltInResourceData((ResourceTypeCode)code, DecimalVM.Value);
			case ResourceElementType.DateTime:	return new BuiltInResourceData((ResourceTypeCode)code, DateTimeVM.Value);
			case ResourceElementType.TimeSpan:	return new BuiltInResourceData((ResourceTypeCode)code, TimeSpanVM.Value);
			case ResourceElementType.ByteArray: return new BuiltInResourceData((ResourceTypeCode)code, Data ?? Array.Empty<byte>());
			case ResourceElementType.Stream:	return new BuiltInResourceData((ResourceTypeCode)code, Data ?? Array.Empty<byte>());
			case ResourceElementType.SerializedType: return new BinaryResourceData(new UserResourceType(UserTypeVM.TypeFullName, ResourceTypeCode.UserTypes), UserTypeVM.GetSerializedData());
			default: throw new InvalidOperationException();
			}
		}

		public override bool HasError {
			get {
				switch ((ResourceElementType)ResourceElementTypeVM.SelectedItem!) {
				case ResourceElementType.Null:		break;
				case ResourceElementType.String:	break;
				case ResourceElementType.Boolean:	if (BooleanVM.HasError) return true; break;
				case ResourceElementType.Char:		if (CharVM.HasError) return true; break;
				case ResourceElementType.Byte:		if (ByteVM.HasError) return true; break;
				case ResourceElementType.SByte:		if (SByteVM.HasError) return true; break;
				case ResourceElementType.Int16:		if (Int16VM.HasError) return true; break;
				case ResourceElementType.UInt16:	if (UInt16VM.HasError) return true; break;
				case ResourceElementType.Int32:		if (Int32VM.HasError) return true; break;
				case ResourceElementType.UInt32:	if (UInt32VM.HasError) return true; break;
				case ResourceElementType.Int64:		if (Int64VM.HasError) return true; break;
				case ResourceElementType.UInt64:	if (UInt64VM.HasError) return true; break;
				case ResourceElementType.Single:	if (SingleVM.HasError) return true; break;
				case ResourceElementType.Double:	if (DoubleVM.HasError) return true; break;
				case ResourceElementType.Decimal:	if (DecimalVM.HasError) return true; break;
				case ResourceElementType.DateTime:	if (DateTimeVM.HasError) return true; break;
				case ResourceElementType.TimeSpan:	if (TimeSpanVM.HasError) return true; break;
				case ResourceElementType.ByteArray:	break;
				case ResourceElementType.Stream:	break;
				case ResourceElementType.SerializedType: if (UserTypeVM.HasError) return true; break;
				default: throw new InvalidOperationException();
				}

				return false;
			}
		}
	}
}
