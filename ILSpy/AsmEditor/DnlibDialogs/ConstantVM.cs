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

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	enum ConstantType
	{
		Null,
		Boolean,
		Char,
		SByte,
		Int16,
		Int32,
		Int64,
		Byte,
		UInt16,
		UInt32,
		UInt64,
		Single,
		Double,
		String,
	}

	sealed class ConstantVM : ViewModelBase
	{
		static readonly EnumVM[] constantTypeList = EnumVM.Create(false, typeof(DnlibDialogs.ConstantType));
		public EnumListVM ConstantType {
			get { return constantTypeVM; }
		}
		readonly EnumListVM constantTypeVM;

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
		bool isEnabled;

		public string ConstantCheckBoxToolTip {
			get { return constantCheckBoxToolTip; }
			set {
				if (constantCheckBoxToolTip != value) {
					constantCheckBoxToolTip = value;
					OnPropertyChanged("ConstantCheckBoxToolTip");
				}
			}
		}
		string constantCheckBoxToolTip;

		public object Value {
			get {
				switch ((DnlibDialogs.ConstantType)ConstantType.SelectedItem) {
				case DnlibDialogs.ConstantType.Null:	return null;
				case DnlibDialogs.ConstantType.Boolean:	return boolean.Value;
				case DnlibDialogs.ConstantType.Char:	return @char.Value;
				case DnlibDialogs.ConstantType.SByte:	return @sbyte.Value;
				case DnlibDialogs.ConstantType.Int16:	return int16.Value;
				case DnlibDialogs.ConstantType.Int32:	return int32.Value;
				case DnlibDialogs.ConstantType.Int64:	return int64.Value;
				case DnlibDialogs.ConstantType.Byte:	return @byte.Value;
				case DnlibDialogs.ConstantType.UInt16:	return uint16.Value;
				case DnlibDialogs.ConstantType.UInt32:	return uint32.Value;
				case DnlibDialogs.ConstantType.UInt64:	return uint64.Value;
				case DnlibDialogs.ConstantType.Single:	return single.Value;
				case DnlibDialogs.ConstantType.Double:	return @double.Value;
				case DnlibDialogs.ConstantType.String:	return @string;
				default: throw new InvalidOperationException();
				}
			}
			set {
				if (value is bool) {
					constantTypeVM.SelectedItem = DnlibDialogs.ConstantType.Boolean;
					boolean.Value = (bool)value;
				}
				else if (value is char) {
					constantTypeVM.SelectedItem = DnlibDialogs.ConstantType.Char;
					@char.Value = (char)value;
				}
				else if (value is sbyte) {
					constantTypeVM.SelectedItem = DnlibDialogs.ConstantType.SByte;
					@sbyte.Value = (sbyte)value;
				}
				else if (value is short) {
					constantTypeVM.SelectedItem = DnlibDialogs.ConstantType.Int16;
					int16.Value = (short)value;
				}
				else if (value is int) {
					constantTypeVM.SelectedItem = DnlibDialogs.ConstantType.Int32;
					int32.Value = (int)value;
				}
				else if (value is long) {
					constantTypeVM.SelectedItem = DnlibDialogs.ConstantType.Int64;
					int64.Value = (long)value;
				}
				else if (value is byte) {
					constantTypeVM.SelectedItem = DnlibDialogs.ConstantType.Byte;
					@byte.Value = (byte)value;
				}
				else if (value is ushort) {
					constantTypeVM.SelectedItem = DnlibDialogs.ConstantType.UInt16;
					uint16.Value = (ushort)value;
				}
				else if (value is uint) {
					constantTypeVM.SelectedItem = DnlibDialogs.ConstantType.UInt32;
					uint32.Value = (uint)value;
				}
				else if (value is ulong) {
					constantTypeVM.SelectedItem = DnlibDialogs.ConstantType.UInt64;
					uint64.Value = (ulong)value;
				}
				else if (value is float) {
					constantTypeVM.SelectedItem = DnlibDialogs.ConstantType.Single;
					single.Value = (float)value;
				}
				else if (value is double) {
					constantTypeVM.SelectedItem = DnlibDialogs.ConstantType.Double;
					@double.Value = (double)value;
				}
				else if (value is string) {
					constantTypeVM.SelectedItem = DnlibDialogs.ConstantType.String;
					@string = (string)value;
				}
				else {
					constantTypeVM.SelectedItem = DnlibDialogs.ConstantType.Null;
				}
			}
		}

		public bool NullIsSelected {
			get { return (DnlibDialogs.ConstantType)ConstantType.SelectedItem == DnlibDialogs.ConstantType.Null; }
		}

		public bool BooleanIsSelected {
			get { return (DnlibDialogs.ConstantType)ConstantType.SelectedItem == DnlibDialogs.ConstantType.Boolean; }
		}

		public bool CharIsSelected {
			get { return (DnlibDialogs.ConstantType)ConstantType.SelectedItem == DnlibDialogs.ConstantType.Char; }
		}

		public bool SByteIsSelected {
			get { return (DnlibDialogs.ConstantType)ConstantType.SelectedItem == DnlibDialogs.ConstantType.SByte; }
		}

		public bool Int16IsSelected {
			get { return (DnlibDialogs.ConstantType)ConstantType.SelectedItem == DnlibDialogs.ConstantType.Int16; }
		}

		public bool Int32IsSelected {
			get { return (DnlibDialogs.ConstantType)ConstantType.SelectedItem == DnlibDialogs.ConstantType.Int32; }
		}

		public bool Int64IsSelected {
			get { return (DnlibDialogs.ConstantType)ConstantType.SelectedItem == DnlibDialogs.ConstantType.Int64; }
		}

		public bool ByteIsSelected {
			get { return (DnlibDialogs.ConstantType)ConstantType.SelectedItem == DnlibDialogs.ConstantType.Byte; }
		}

		public bool UInt16IsSelected {
			get { return (DnlibDialogs.ConstantType)ConstantType.SelectedItem == DnlibDialogs.ConstantType.UInt16; }
		}

		public bool UInt32IsSelected {
			get { return (DnlibDialogs.ConstantType)ConstantType.SelectedItem == DnlibDialogs.ConstantType.UInt32; }
		}

		public bool UInt64IsSelected {
			get { return (DnlibDialogs.ConstantType)ConstantType.SelectedItem == DnlibDialogs.ConstantType.UInt64; }
		}

		public bool SingleIsSelected {
			get { return (DnlibDialogs.ConstantType)ConstantType.SelectedItem == DnlibDialogs.ConstantType.Single; }
		}

		public bool DoubleIsSelected {
			get { return (DnlibDialogs.ConstantType)ConstantType.SelectedItem == DnlibDialogs.ConstantType.Double; }
		}

		public bool StringIsSelected {
			get { return (DnlibDialogs.ConstantType)ConstantType.SelectedItem == DnlibDialogs.ConstantType.String; }
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

		public string String {
			get { return @string; }
			set {
				if (@string != value) {
					@string = value ?? string.Empty;
					OnPropertyChanged("String");
				}
			}
		}
		string @string = string.Empty;

		public ConstantVM(object value, string constantCheckBoxToolTip)
		{
			this.ConstantCheckBoxToolTip = constantCheckBoxToolTip;
			this.constantTypeVM = new EnumListVM(constantTypeList, OnConstantChanged);
			this.boolean = new BooleanVM(a => HasErrorUpdated());
			this.@char = new CharVM(a => HasErrorUpdated());
			this.@sbyte = new SByteVM(a => HasErrorUpdated());
			this.@byte = new ByteVM(a => HasErrorUpdated());
			this.int16 = new Int16VM(a => HasErrorUpdated());
			this.uint16 = new UInt16VM(a => HasErrorUpdated());
			this.int32 = new Int32VM(a => HasErrorUpdated());
			this.uint32 = new UInt32VM(a => HasErrorUpdated());
			this.int64 = new Int64VM(a => HasErrorUpdated());
			this.uint64 = new UInt64VM(a => HasErrorUpdated());
			this.single = new SingleVM(a => HasErrorUpdated());
			this.@double = new DoubleVM(a => HasErrorUpdated());
			this.Value = value;
		}

		void OnConstantChanged()
		{
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

				switch ((DnlibDialogs.ConstantType)ConstantType.SelectedItem) {
				case DnlibDialogs.ConstantType.Null:	break;
				case DnlibDialogs.ConstantType.Boolean:	if (boolean.HasError) return true; break;
				case DnlibDialogs.ConstantType.Char:	if (@char.HasError) return true; break;
				case DnlibDialogs.ConstantType.SByte:	if (@sbyte.HasError) return true; break;
				case DnlibDialogs.ConstantType.Int16:	if (int16.HasError) return true; break;
				case DnlibDialogs.ConstantType.Int32:	if (int32.HasError) return true; break;
				case DnlibDialogs.ConstantType.Int64:	if (int64.HasError) return true; break;
				case DnlibDialogs.ConstantType.Byte:	if (@byte.HasError) return true; break;
				case DnlibDialogs.ConstantType.UInt16:	if (uint16.HasError) return true; break;
				case DnlibDialogs.ConstantType.UInt32:	if (uint32.HasError) return true; break;
				case DnlibDialogs.ConstantType.UInt64:	if (uint64.HasError) return true; break;
				case DnlibDialogs.ConstantType.Single:	if (single.HasError) return true; break;
				case DnlibDialogs.ConstantType.Double:	if (@double.HasError) return true; break;
				case DnlibDialogs.ConstantType.String:	break;
				default: throw new InvalidOperationException();
				}

				return false;
			}
		}
	}
}
