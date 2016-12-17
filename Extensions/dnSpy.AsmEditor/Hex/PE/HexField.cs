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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.Hex.PE {
	[DebuggerDisplay("{Span} {Name} {DataFieldVM.StringValue}")]
	abstract class HexField {
		protected readonly HexBuffer buffer;
		readonly string parentName;

		public string NameUI => UIUtilities.EscapeMenuItemHeader(Name);
		public string Name { get; }
		public string OffsetString => string.Format("0x{0:X8}", Span.Start.ToUInt64());
		public HexSpan Span { get; }
		public bool IsVisible { get; set; }
		public HexPosition Size => Span.Length;
		public abstract string FormattedValue { get; }

		protected HexField(HexBuffer buffer, string parentName, string name, HexPosition start, int size) {
			this.buffer = buffer;
			this.parentName = parentName;
			IsVisible = true;
			Name = name;
			Span = new HexSpan(start, (ulong)size);
		}

		public abstract DataFieldVM DataFieldVM { get; }

		public void OnBufferChanged(NormalizedHexChangeCollection changes) {
			if (!changes.OverlapsWith(Span))
				return;

			var newValue = ReadData();
			if (!DataFieldVM.HasError && newValue.Equals(DataFieldVM.ObjectValue))
				return;

			var old = disable_UpdateValue;
			try {
				disable_UpdateValue = true;
				DataFieldVM.ObjectValue = newValue;
				OnBufferChanged(newValue);
			}
			finally {
				disable_UpdateValue = old;
			}
		}

		protected virtual void OnBufferChanged(object newValue) {
		}

		protected void UpdateValue() {
			if (disable_UpdateValue)
				return;
			if (DataFieldVM.HasError)
				return;
			var newData = GetDataAsByteArray();
			Debug.Assert(newData != null && newData.LongLength == Span.Length);

			var origData = buffer.ReadBytes(Span.Start, newData.LongLength);
			if (Equals(newData, origData))
				return;

			buffer.Replace(Span.Start, newData);
			OnUpdateValue();
		}
		bool disable_UpdateValue = false;

		static bool Equals(byte[] a, byte[] b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		protected abstract byte[] GetDataAsByteArray();
		protected abstract object ReadData();
		protected virtual void OnUpdateValue() { }
	}

	sealed class ByteHexField : HexField {
		public override DataFieldVM DataFieldVM => data;
		readonly ByteVM data;

		public override string FormattedValue => string.Format("{0:X2}", ReadData());

		public ByteHexField(HexBuffer buffer, string parentName, string name, HexPosition start, bool useDecimal = false)
			: base(buffer, parentName, name, start, 1) {
			data = new ByteVM(buffer.ReadByte(start), a => UpdateValue(), useDecimal);
		}

		protected override byte[] GetDataAsByteArray() => new byte[1] { data.Value };
		protected override object ReadData() => buffer.ReadByte(Span.Start);
	}

	sealed class Int16HexField : HexField {
		public override DataFieldVM DataFieldVM => data;
		readonly Int16VM data;

		public override string FormattedValue => string.Format("{0:X4}", ReadData());

		public Int16HexField(HexBuffer buffer, string parentName, string name, HexPosition start, bool useDecimal = false)
			: base(buffer, parentName, name, start, 2) {
			data = new Int16VM(buffer.ReadInt16(start), a => UpdateValue(), useDecimal);
		}

		protected override byte[] GetDataAsByteArray() => BitConverter.GetBytes(data.Value);
		protected override object ReadData() => buffer.ReadInt16(Span.Start);
	}

	sealed class UInt16HexField : HexField {
		public override DataFieldVM DataFieldVM => data;
		readonly UInt16VM data;

		public override string FormattedValue => string.Format("{0:X4}", ReadData());

		public UInt16HexField(HexBuffer buffer, string parentName, string name, HexPosition start, bool useDecimal = false)
			: base(buffer, parentName, name, start, 2) {
			data = new UInt16VM(buffer.ReadUInt16(start), a => UpdateValue(), useDecimal);
		}

		protected override byte[] GetDataAsByteArray() => BitConverter.GetBytes(data.Value);
		protected override object ReadData() => buffer.ReadUInt16(Span.Start);
	}

	sealed class Int32HexField : HexField {
		public override DataFieldVM DataFieldVM => data;
		readonly Int32VM data;

		public override string FormattedValue => string.Format("{0:X8}", ReadData());

		public Int32HexField(HexBuffer buffer, string parentName, string name, HexPosition start, bool useDecimal = false)
			: base(buffer, parentName, name, start, 4) {
			data = new Int32VM(buffer.ReadInt32(start), a => UpdateValue(), useDecimal);
		}

		protected override byte[] GetDataAsByteArray() => BitConverter.GetBytes(data.Value);
		protected override object ReadData() => buffer.ReadInt32(Span.Start);
	}

	sealed class UInt32HexField : HexField {
		public override DataFieldVM DataFieldVM => data;
		readonly UInt32VM data;

		public override string FormattedValue => string.Format("{0:X8}", ReadData());

		public UInt32HexField(HexBuffer buffer, string parentName, string name, HexPosition start, bool useDecimal = false)
			: base(buffer, parentName, name, start, 4) {
			data = new UInt32VM(buffer.ReadUInt32(start), a => UpdateValue(), useDecimal);
		}

		protected override byte[] GetDataAsByteArray() => BitConverter.GetBytes(data.Value);
		protected override object ReadData() => buffer.ReadUInt32(Span.Start);
	}

	sealed class UInt64HexField : HexField {
		public override DataFieldVM DataFieldVM => data;
		readonly UInt64VM data;

		public override string FormattedValue => string.Format("{0:X16}", ReadData());

		public UInt64HexField(HexBuffer buffer, string parentName, string name, HexPosition start, bool useDecimal = false)
			: base(buffer, parentName, name, start, 8) {
			data = new UInt64VM(buffer.ReadUInt64(start), a => UpdateValue(), useDecimal);
		}

		protected override byte[] GetDataAsByteArray() => BitConverter.GetBytes(data.Value);
		protected override object ReadData() => buffer.ReadUInt64(Span.Start);
	}

	sealed class StringHexField : HexField {
		public override DataFieldVM DataFieldVM => data;
		readonly StringVM data;

		public override string FormattedValue => String;

		public string String {
			get {
				if (DataFieldVM.HasError)
					return "???";
				var data = GetDataAsByteArray();
				int count;
				for (count = data.Length - 1; count >= 0; count--) {
					if (data[count] != 0)
						break;
				}
				return Filter(encoding.GetString(data, 0, count + 1));
			}
		}

		public string StringZ {
			get {
				if (DataFieldVM.HasError)
					return "???";
				var data = GetDataAsByteArray();
				int count;
				for (count = 0; count < data.Length; count++) {
					if (data[count] == 0)
						break;
				}
				return Filter(encoding.GetString(data, 0, count));
			}
		}

		static string Filter(string s) {
			var sb = new StringBuilder(s.Length);
			foreach (var c in s) {
				if (c == 0)
					sb.Append(@"\0");
				else if (c < 0x20)
					sb.Append(string.Format("\\u{0:X4}", (ushort)c));
				else
					sb.Append(c);
			}
			return sb.ToString();
		}

		readonly Encoding encoding;

		public StringHexField(HexBuffer buffer, string parentName, string name, HexPosition start, Encoding encoding, int dataLen)
			: base(buffer, parentName, name, start, dataLen) {
			this.encoding = encoding;
			data = new StringVM((string)ReadData(), a => UpdateValue());
		}

		protected override byte[] GetDataAsByteArray() {
			var sd = encoding.GetBytes(data.Value);
			var d = new byte[Span.Length.ToUInt64()];
			Array.Copy(sd, d, Math.Min(d.Length, sd.Length));
			return d;
		}

		protected override object ReadData() => encoding.GetString(buffer.ReadBytes(Span.Start, Span.Length.ToUInt64()));
	}

	abstract class FlagsHexField : HexField {
		Dictionary<int, HexBitField> bitFields;

		public HexBitField Bit0 => GetBitField(0);
		public HexBitField Bit1 => GetBitField(1);
		public HexBitField Bit2 => GetBitField(2);
		public HexBitField Bit3 => GetBitField(3);
		public HexBitField Bit4 => GetBitField(4);
		public HexBitField Bit5 => GetBitField(5);
		public HexBitField Bit6 => GetBitField(6);
		public HexBitField Bit7 => GetBitField(7);
		public HexBitField Bit8 => GetBitField(8);
		public HexBitField Bit9 => GetBitField(9);
		public HexBitField Bit10 => GetBitField(10);
		public HexBitField Bit11 => GetBitField(11);
		public HexBitField Bit12 => GetBitField(12);
		public HexBitField Bit13 => GetBitField(13);
		public HexBitField Bit14 => GetBitField(14);
		public HexBitField Bit15 => GetBitField(15);
		public HexBitField Bit16 => GetBitField(16);
		public HexBitField Bit17 => GetBitField(17);
		public HexBitField Bit18 => GetBitField(18);
		public HexBitField Bit19 => GetBitField(19);
		public HexBitField Bit20 => GetBitField(20);
		public HexBitField Bit21 => GetBitField(21);
		public HexBitField Bit22 => GetBitField(22);
		public HexBitField Bit23 => GetBitField(23);
		public HexBitField Bit24 => GetBitField(24);
		public HexBitField Bit25 => GetBitField(25);
		public HexBitField Bit26 => GetBitField(26);
		public HexBitField Bit27 => GetBitField(27);
		public HexBitField Bit28 => GetBitField(28);
		public HexBitField Bit29 => GetBitField(29);
		public HexBitField Bit30 => GetBitField(30);
		public HexBitField Bit31 => GetBitField(31);
		public HexBitField Bit32 => GetBitField(32);
		public HexBitField Bit33 => GetBitField(33);
		public HexBitField Bit34 => GetBitField(34);
		public HexBitField Bit35 => GetBitField(35);
		public HexBitField Bit36 => GetBitField(36);
		public HexBitField Bit37 => GetBitField(37);
		public HexBitField Bit38 => GetBitField(38);
		public HexBitField Bit39 => GetBitField(39);
		public HexBitField Bit40 => GetBitField(40);
		public HexBitField Bit41 => GetBitField(41);
		public HexBitField Bit42 => GetBitField(42);
		public HexBitField Bit43 => GetBitField(43);
		public HexBitField Bit44 => GetBitField(44);
		public HexBitField Bit45 => GetBitField(45);
		public HexBitField Bit46 => GetBitField(46);
		public HexBitField Bit47 => GetBitField(47);
		public HexBitField Bit48 => GetBitField(48);
		public HexBitField Bit49 => GetBitField(49);
		public HexBitField Bit50 => GetBitField(50);
		public HexBitField Bit51 => GetBitField(51);
		public HexBitField Bit52 => GetBitField(52);
		public HexBitField Bit53 => GetBitField(53);
		public HexBitField Bit54 => GetBitField(54);
		public HexBitField Bit55 => GetBitField(55);
		public HexBitField Bit56 => GetBitField(56);
		public HexBitField Bit57 => GetBitField(57);
		public HexBitField Bit58 => GetBitField(58);
		public HexBitField Bit59 => GetBitField(59);
		public HexBitField Bit60 => GetBitField(60);
		public HexBitField Bit61 => GetBitField(61);
		public HexBitField Bit62 => GetBitField(62);
		public HexBitField Bit63 => GetBitField(63);

		HexBitField GetBitField(int bit) {
			HexBitField bitField;
			bool b = bitFields.TryGetValue(bit, out bitField);
			Debug.Assert(b);
			return bitField;
		}

		protected FlagsHexField(HexBuffer buffer, string parentName, string name, HexPosition start, int size)
			: base(buffer, parentName, name, start, size) {
			bitFields = new Dictionary<int, HexBitField>();
		}

		static ulong ToUInt64(object o) {
			if (o is byte)
				return (byte)o;
			if (o is ushort)
				return (ushort)o;
			if (o is short)
				return (ushort)(short)o;
			if (o is uint)
				return (uint)o;
			if (o is int)
				return (uint)(int)o;
			if (o is ulong)
				return (ulong)o;
			throw new InvalidOperationException();
		}

		public void Add(HexBitField bitField) {
			Debug.Assert(bitField.Owner == null);
			bitField.Owner = this;
			bitFields.Add(bitField.Bit, bitField);
			Debug.Assert(!DataFieldVM.HasError);	// Should only be called at init and it's then always valid
			ulong val = ToUInt64(DataFieldVM.ObjectValue);
			SetValue(bitField, val);
		}

		protected override void OnBufferChanged(object newValue) => UpdateFields(newValue);
		protected override void OnUpdateValue() => UpdateFields(DataFieldVM.ObjectValue);

		void UpdateFields(object newValue) {
			ulong val = ToUInt64(newValue);

			foreach (var bitField in bitFields.Values)
				SetValue(bitField, val);
		}

		void SetValue(HexBitField bitField, ulong val) {
			ulong bitVal = (val >> bitField.Bit) & bitField.Mask;
			bitField.SetValue(bitVal);
		}

		internal void Updated(HexBitField bitField) {
			ulong val = ToUInt64(ReadData());
			ulong origVal = val;
			val &= ~(bitField.Mask << bitField.Bit);
			val |= bitField.GetValue() << bitField.Bit;
			if (origVal != val)
				WriteNewValue(val);
		}

		protected abstract void WriteNewValue(ulong newValue);
	}

	sealed class ByteFlagsHexField : FlagsHexField {
		public override DataFieldVM DataFieldVM => data;
		readonly ByteVM data;

		public override string FormattedValue => string.Format("{0:X2}", ReadData());

		public ByteFlagsHexField(HexBuffer buffer, string parentName, string name, HexPosition start)
			: base(buffer, parentName, name, start, 1) {
			data = new ByteVM(buffer.ReadByte(start), a => UpdateValue(), false);
		}

		protected override byte[] GetDataAsByteArray() => new byte[1] { data.Value };
		protected override object ReadData() => buffer.ReadByte(Span.Start);
		protected override void WriteNewValue(ulong newValue) => data.Value = (byte)newValue;
	}

	sealed class Int16FlagsHexField : FlagsHexField {
		public override DataFieldVM DataFieldVM => data;
		readonly Int16VM data;

		public override string FormattedValue => string.Format("{0:X4}", ReadData());

		public Int16FlagsHexField(HexBuffer buffer, string parentName, string name, HexPosition start)
			: base(buffer, parentName, name, start, 2) {
			data = new Int16VM(buffer.ReadInt16(start), a => UpdateValue(), false);
		}

		protected override byte[] GetDataAsByteArray() => BitConverter.GetBytes(data.Value);
		protected override object ReadData() => buffer.ReadInt16(Span.Start);
		protected override void WriteNewValue(ulong newValue) => data.Value = (short)newValue;
	}

	sealed class UInt16FlagsHexField : FlagsHexField {
		public override DataFieldVM DataFieldVM => data;
		readonly UInt16VM data;

		public override string FormattedValue => string.Format("{0:X4}", ReadData());

		public UInt16FlagsHexField(HexBuffer buffer, string parentName, string name, HexPosition start)
			: base(buffer, parentName, name, start, 2) {
			data = new UInt16VM(buffer.ReadUInt16(start), a => UpdateValue(), false);
		}

		protected override byte[] GetDataAsByteArray() => BitConverter.GetBytes(data.Value);
		protected override object ReadData() => buffer.ReadUInt16(Span.Start);
		protected override void WriteNewValue(ulong newValue) => data.Value = (ushort)newValue;
	}

	sealed class UInt32FlagsHexField : FlagsHexField {
		public override DataFieldVM DataFieldVM => data;
		readonly UInt32VM data;

		public override string FormattedValue => string.Format("{0:X8}", ReadData());

		public UInt32FlagsHexField(HexBuffer buffer, string parentName, string name, HexPosition start)
			: base(buffer, parentName, name, start, 4) {
			data = new UInt32VM(buffer.ReadUInt32(start), a => UpdateValue(), false);
		}

		protected override byte[] GetDataAsByteArray() => BitConverter.GetBytes(data.Value);
		protected override object ReadData() => buffer.ReadUInt32(Span.Start);
		protected override void WriteNewValue(ulong newValue) => data.Value = (uint)newValue;
	}

	sealed class UInt64FlagsHexField : FlagsHexField {
		public override DataFieldVM DataFieldVM => data;
		readonly UInt64VM data;

		public override string FormattedValue => string.Format("{0:X16}", ReadData());

		public UInt64FlagsHexField(HexBuffer buffer, string parentName, string name, HexPosition start)
			: base(buffer, parentName, name, start, 8) {
			data = new UInt64VM(buffer.ReadUInt64(start), a => UpdateValue(), false);
		}

		protected override byte[] GetDataAsByteArray() => BitConverter.GetBytes(data.Value);
		protected override object ReadData() => buffer.ReadUInt64(Span.Start);
		protected override void WriteNewValue(ulong newValue) => data.Value = newValue;
	}

	abstract class HexBitField : ViewModelBase {
		public string NameUI => UIUtilities.EscapeMenuItemHeader(Name);
		public string Name { get; }
		public int Bit { get; }
		public ulong Mask => Count == 64 ? ulong.MaxValue : (1UL << Count) - 1;
		public int Count { get; }
		internal FlagsHexField Owner { get; set; }

		public HexBitField(string name, int bit, int count) {
			Debug.Assert(0 <= bit && bit <= 63 && 1 <= count && count <= 64 && bit + count <= 64);
			Name = name;
			Bit = bit;
			Count = count;
		}

		public abstract void SetValue(ulong value);
		public abstract ulong GetValue();
	}

	sealed class BooleanHexBitField : HexBitField {
		public bool BitValue {
			get { return boolean; }
			set {
				if (SetBitValue(value))
					Owner.Updated(this);
			}
		}
		bool SetBitValue(bool value) {
			if (boolean == value)
				return false;
			boolean = value;
			OnPropertyChanged(nameof(BitValue));
			return true;
		}
		bool boolean;

		public BooleanHexBitField(string name, int bit)
			: base(name, bit, 1) {
		}

		public override void SetValue(ulong value) {
			Debug.Assert(value <= 1);
			SetBitValue(value != 0);
		}

		public override ulong GetValue() => boolean ? 1UL : 0;
	}

	enum IntegerHexBitFieldEnum : ulong {
	}

	struct IntegerHexBitFieldEnumInfo {
		public readonly ulong Value;
		public readonly string Name;

		public IntegerHexBitFieldEnumInfo(int v, string name) {
			Value = (ulong)v;
			Name = name;
		}
	}

	sealed class IntegerHexBitField : HexBitField {
		public EnumListVM ListVM {
			get {
				var res = listOrEnumInfos as EnumListVM;
				if (res != null)
					return res;

				var list = ((IntegerHexBitFieldEnumInfo[])listOrEnumInfos).Select(a => new EnumVM((IntegerHexBitFieldEnum)a.Value, a.Name));
				listOrEnumInfos = res = new EnumListVM(list, ListUpdated);
				return res;
			}
		}
		object listOrEnumInfos;

		public IntegerHexBitField(string name, int bit, int count, IntegerHexBitFieldEnumInfo[] fields)
			: base(name, bit, count) {
			listOrEnumInfos = fields;
		}

		void ListUpdated(int a, int b) {
			if (ListUpdated_ignore)
				return;

			Owner.Updated(this);
		}
		bool ListUpdated_ignore = false;

		public override void SetValue(ulong value) {
			var old = ListUpdated_ignore;
			try {
				ListUpdated_ignore = true;
				ListVM.SelectedItem = (IntegerHexBitFieldEnum)value;
			}
			finally {
				ListUpdated_ignore = old;
			}
		}

		public override ulong GetValue() => (ulong)(IntegerHexBitFieldEnum)ListVM.SelectedItem;
	}

	sealed class DataDirVM : ViewModelBase {
		public string Name { get; }
		public UInt32HexField RVAVM { get; }
		public UInt32HexField SizeVM { get; }

		public bool IsVisible {
			get { return isVisible; }
			set {
				if (isVisible != value) {
					isVisible = value;
					OnPropertyChanged(nameof(IsVisible));
				}
			}
		}
		bool isVisible = true;

		public DataDirVM(HexBuffer buffer, string parentName, string name, HexPosition start) {
			Name = name;
			RVAVM = new UInt32HexField(buffer, parentName, string.Format(dnSpy_AsmEditor_Resources.HexField_RelativeVirtualAddress, name), start);
			SizeVM = new UInt32HexField(buffer, parentName, string.Format(dnSpy_AsmEditor_Resources.HexField_Size, name), start + 4);
		}
	}
}
