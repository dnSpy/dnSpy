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
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.MVVM;
using dnSpy.Shared.UI.MVVM;

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
			set { openFile = value; }
		}
		IOpenFile openFile;

		public IDnlibTypePicker DnlibTypePicker {
			set { UserTypeVM.DnlibTypePicker = value; }
		}

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public ICommand PickRawBytesCommand {
			get { return new RelayCommand(a => PickRawBytes(), a => IsRawBytes); }
		}

		public bool CanChangeType {
			get { return canChangeType; }
			set {
				if (canChangeType != value) {
					canChangeType = value;
					OnPropertyChanged("CanChangeType");
				}
			}
		}
		bool canChangeType = true;

		internal static readonly EnumVM[] resourceElementTypeList = EnumVM.Create(false, typeof(ResourceElementType));
		public EnumListVM ResourceElementTypeVM {
			get { return resourceElementTypeVM; }
		}
		readonly EnumListVM resourceElementTypeVM;

		public string Name {
			get { return name; }
			set {
				if (name != value) {
					name = value;
					OnPropertyChanged("Name");
				}
			}
		}
		UTF8String name;

		public string String {
			get { return @string; }
			set {
				if (@string != value) {
					@string = value;
					OnPropertyChanged("String");
				}
			}
		}
		string @string;

		public BooleanVM BooleanVM {
			get { return booleanVM; }
		}
		readonly BooleanVM booleanVM;

		public CharVM CharVM {
			get { return charVM; }
		}
		readonly CharVM charVM;

		public ByteVM ByteVM {
			get { return byteVM; }
		}
		readonly ByteVM byteVM;

		public SByteVM SByteVM {
			get { return sbyteVM; }
		}
		readonly SByteVM sbyteVM;

		public Int16VM Int16VM {
			get { return int16VM; }
		}
		readonly Int16VM int16VM;

		public UInt16VM UInt16VM {
			get { return uint16VM; }
		}
		readonly UInt16VM uint16VM;

		public Int32VM Int32VM {
			get { return int32VM; }
		}
		readonly Int32VM int32VM;

		public UInt32VM UInt32VM {
			get { return uint32VM; }
		}
		readonly UInt32VM uint32VM;

		public Int64VM Int64VM {
			get { return int64VM; }
		}
		readonly Int64VM int64VM;

		public UInt64VM UInt64VM {
			get { return uint64VM; }
		}
		readonly UInt64VM uint64VM;

		public SingleVM SingleVM {
			get { return singleVM; }
		}
		readonly SingleVM singleVM;

		public DoubleVM DoubleVM {
			get { return doubleVM; }
		}
		readonly DoubleVM doubleVM;

		public DecimalVM DecimalVM {
			get { return decimalVM; }
		}
		readonly DecimalVM decimalVM;

		public DateTimeVM DateTimeVM {
			get { return dateTimeVM; }
		}
		readonly DateTimeVM dateTimeVM;

		public TimeSpanVM TimeSpanVM {
			get { return timeSpanVM; }
		}
		readonly TimeSpanVM timeSpanVM;

		public byte[] Data {
			get { return data; }
			set {
				if (data != value) {
					data = value;
					OnPropertyChanged("Data");
					OnPropertyChanged("DataString");
				}
			}
		}
		byte[] data;

		public string DataString {
			get { return string.Format("{0} bytes", Data == null ? 0 : Data.Length); }
		}

		public UserTypeVM UserTypeVM {
			get { return userTypeVM; }
		}
		UserTypeVM userTypeVM;

		public object ValueVM {
			get {
				switch ((ResourceElementType)this.ResourceElementTypeVM.SelectedItem) {
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

		public bool IsSerializedType {
			get { return (ResourceElementType)this.ResourceElementTypeVM.SelectedItem == ResourceElementType.SerializedType; }
		}

		public bool IsSingleLineValue {
			get { return !IsMultiLineValue && !IsRawBytes && (ResourceElementType)this.ResourceElementTypeVM.SelectedItem != ResourceElementType.Null; }
		}

		public bool IsMultiLineValue {
			get { return (ResourceElementType)this.ResourceElementTypeVM.SelectedItem == ResourceElementType.String; }
		}

		public bool IsRawBytes {
			get {
				var code = (ResourceElementType)this.ResourceElementTypeVM.SelectedItem;
				return code == ResourceElementType.ByteArray || code == ResourceElementType.Stream;
			}
		}

		readonly bool canDeserialize;

		public ResourceElementVM(ResourceElementOptions options, ModuleDef ownerModule, bool canDeserialize) {
			this.origOptions = options;
			this.canDeserialize = canDeserialize;

			this.booleanVM = new BooleanVM(a => HasErrorUpdated());
			this.charVM = new CharVM(a => HasErrorUpdated());
			this.byteVM = new ByteVM(a => HasErrorUpdated());
			this.sbyteVM = new SByteVM(a => HasErrorUpdated());
			this.int16VM = new Int16VM(a => HasErrorUpdated());
			this.uint16VM = new UInt16VM(a => HasErrorUpdated());
			this.int32VM = new Int32VM(a => HasErrorUpdated());
			this.uint32VM = new UInt32VM(a => HasErrorUpdated());
			this.int64VM = new Int64VM(a => HasErrorUpdated());
			this.uint64VM = new UInt64VM(a => HasErrorUpdated());
			this.singleVM = new SingleVM(a => HasErrorUpdated());
			this.doubleVM = new DoubleVM(a => HasErrorUpdated());
			this.decimalVM = new DecimalVM(a => HasErrorUpdated());
			this.dateTimeVM = new DateTimeVM(a => HasErrorUpdated());
			this.timeSpanVM = new TimeSpanVM(a => HasErrorUpdated());
			this.userTypeVM = new UserTypeVM(ownerModule, canDeserialize);
			this.resourceElementTypeVM = new EnumListVM(resourceElementTypeList, (a, b) => OnResourceElementTypeChanged());

			this.UserTypeVM.PropertyChanged += (s, e) => {
				if (e.PropertyName == "HasError")
					HasErrorUpdated();
			};

			Reinitialize();
		}

		void PickRawBytes() {
			if (openFile == null)
				throw new InvalidOperationException();
			var newBytes = openFile.Open();
			if (newBytes != null)
				Data = newBytes;
		}

		void OnResourceElementTypeChanged() {
			OnPropertyChanged("ValueVM");
			OnPropertyChanged("IsSerializedType");
			OnPropertyChanged("IsSingleLineValue");
			OnPropertyChanged("IsMultiLineValue");
			OnPropertyChanged("IsRawBytes");
			HasErrorUpdated();
		}

		void Reinitialize() {
			InitializeFrom(origOptions);
		}

		public ResourceElementOptions CreateResourceElementOptions() {
			return CopyTo(new ResourceElementOptions());
		}

		void InitializeFrom(ResourceElementOptions options) {
			this.Name = options.Name;
			var code = Convert(options.ResourceData.Code);
			var builtin = options.ResourceData as BuiltInResourceData;
			this.ResourceElementTypeVM.SelectedItem = code;
			switch (code) {
			case ResourceElementType.Null:		break;
			case ResourceElementType.String:	String = (string)builtin.Data; break;
			case ResourceElementType.Boolean:	BooleanVM.Value = (bool)builtin.Data; break;
			case ResourceElementType.Char:		CharVM.Value = (char)builtin.Data; break;
			case ResourceElementType.Byte:		ByteVM.Value = (byte)builtin.Data; break;
			case ResourceElementType.SByte:		SByteVM.Value = (sbyte)builtin.Data; break;
			case ResourceElementType.Int16:		Int16VM.Value = (short)builtin.Data; break;
			case ResourceElementType.UInt16:	UInt16VM.Value = (ushort)builtin.Data; break;
			case ResourceElementType.Int32:		Int32VM.Value = (int)builtin.Data; break;
			case ResourceElementType.UInt32:	UInt32VM.Value = (uint)builtin.Data; break;
			case ResourceElementType.Int64:		Int64VM.Value = (long)builtin.Data; break;
			case ResourceElementType.UInt64:	UInt64VM.Value = (ulong)builtin.Data; break;
			case ResourceElementType.Single:	SingleVM.Value = (float)builtin.Data; break;
			case ResourceElementType.Double:	DoubleVM.Value = (double)builtin.Data; break;
			case ResourceElementType.Decimal:	DecimalVM.Value = (decimal)builtin.Data; break;
			case ResourceElementType.DateTime:	DateTimeVM.Value = (DateTime)builtin.Data; break;
			case ResourceElementType.TimeSpan:	TimeSpanVM.Value = (TimeSpan)builtin.Data; break;
			case ResourceElementType.ByteArray:	Data = (byte[])builtin.Data; break;
			case ResourceElementType.Stream:	Data = (byte[])builtin.Data; break;

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
			options.Name = this.Name;
			options.ResourceData = CreateResourceData();
			return options;
		}

		IResourceData CreateResourceData() {
			var code = (ResourceElementType)this.ResourceElementTypeVM.SelectedItem;
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
			case ResourceElementType.ByteArray: return new BuiltInResourceData((ResourceTypeCode)code, Data ?? new byte[0]);
			case ResourceElementType.Stream:	return new BuiltInResourceData((ResourceTypeCode)code, Data ?? new byte[0]);
			case ResourceElementType.SerializedType: return new BinaryResourceData(new UserResourceType(UserTypeVM.TypeFullName, ResourceTypeCode.UserTypes), UserTypeVM.GetSerializedData());
			default: throw new InvalidOperationException();
			}
		}

		public override bool HasError {
			get {
				switch ((ResourceElementType)this.ResourceElementTypeVM.SelectedItem) {
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
