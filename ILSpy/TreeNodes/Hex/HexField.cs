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
using System.Text;
using dnSpy.AsmEditor;
using dnSpy.AsmEditor.Hex;
using dnSpy.HexEditor;

namespace dnSpy.TreeNodes.Hex {
	[DebuggerDisplay("{StartOffset} {EndOffset} {Name} {DataFieldVM.StringValue}")]
	abstract class HexField {
		protected readonly HexDocument doc;
		readonly string parentName;

		public string NameUI {
			get { return UIUtils.EscapeMenuItemHeader(name); }
		}

		public string Name {
			get { return name; }
		}
		readonly string name;

		public string OffsetString {
			get { return string.Format("0x{0:X8}", startOffset); }
		}

		public ulong StartOffset {
			get { return startOffset; }
		}
		readonly ulong startOffset;

		public ulong EndOffset {
			get { return endOffset; }
		}
		readonly ulong endOffset;

		public bool IsVisible {
			get { return isVisible; }
			set { isVisible = value; }
		}
		bool isVisible = true;

		public int Size {
			get { return (int)(EndOffset - StartOffset + 1); }
		}

		public abstract string FormattedValue { get; }

		protected HexField(HexDocument doc, string parentName, string name, ulong start, int size) {
			this.doc = doc;
			this.parentName = parentName;
			this.name = name;
			this.startOffset = start;
			this.endOffset = start + (ulong)size - 1;
		}

		public abstract DataFieldVM DataFieldVM { get; }

		public void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			if (!HexUtils.IsModified(startOffset, endOffset, modifiedStart, modifiedEnd))
				return;

			var newValue = ReadDataFrom(doc);
			if (!DataFieldVM.HasError && newValue.Equals(DataFieldVM.ObjectValue))
				return;

			var old = disable_UpdateValue;
			try {
				disable_UpdateValue = true;
				DataFieldVM.ObjectValue = newValue;
			}
			finally {
				disable_UpdateValue = old;
			}
        }

		protected void UpdateValue() {
			if (disable_UpdateValue)
				return;
			if (DataFieldVM.HasError)
				return;
			var newData = GetDataAsByteArray();
			Debug.Assert(newData != null && (ulong)newData.Length == endOffset - startOffset + 1);

			var origData = doc.ReadBytes(startOffset, newData.Length);
			if (Equals(newData, origData))
				return;

			WriteHexUndoCommand.AddAndExecute(doc, startOffset, newData, string.Format("Write {0}.{1}", parentName, Name));
			doc.Write(startOffset, newData);
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
		protected abstract object ReadDataFrom(IHexStream stream);
	}

	sealed class ByteHexField : HexField {
		public override DataFieldVM DataFieldVM {
			get { return data; }
		}
		readonly ByteVM data;

		public override string FormattedValue {
			get { return string.Format("{0:X2}", ReadDataFrom(doc)); }
		}

		public ByteHexField(HexDocument doc, string parentName, string name, ulong start)
			: base(doc, parentName, name, start, 1) {
			this.data = new ByteVM((byte)doc.ReadByte(start), a => UpdateValue());
		}

		protected override byte[] GetDataAsByteArray() {
			return new byte[1] { data.Value };
		}

		protected override object ReadDataFrom(IHexStream stream) {
			return (byte)stream.ReadByte(StartOffset);
		}
	}

	sealed class Int16HexField : HexField {
		public override DataFieldVM DataFieldVM {
			get { return data; }
		}
		readonly Int16VM data;

		public override string FormattedValue {
			get { return string.Format("{0:X4}", ReadDataFrom(doc)); }
		}

		public Int16HexField(HexDocument doc, string parentName, string name, ulong start)
			: base(doc, parentName, name, start, 2) {
			this.data = new Int16VM(doc.ReadInt16(start), a => UpdateValue());
		}

		protected override byte[] GetDataAsByteArray() {
			return BitConverter.GetBytes(data.Value);
		}

		protected override object ReadDataFrom(IHexStream stream) {
			return stream.ReadInt16(StartOffset);
		}
	}

	sealed class UInt16HexField : HexField {
		public override DataFieldVM DataFieldVM {
			get { return data; }
		}
		readonly UInt16VM data;

		public override string FormattedValue {
			get { return string.Format("{0:X4}", ReadDataFrom(doc)); }
		}

		public UInt16HexField(HexDocument doc, string parentName, string name, ulong start)
			: base(doc, parentName, name, start, 2) {
			this.data = new UInt16VM(doc.ReadUInt16(start), a => UpdateValue());
		}

		protected override byte[] GetDataAsByteArray() {
			return BitConverter.GetBytes(data.Value);
		}

		protected override object ReadDataFrom(IHexStream stream) {
			return stream.ReadUInt16(StartOffset);
		}
	}

	sealed class Int32HexField : HexField {
		public override DataFieldVM DataFieldVM {
			get { return data; }
		}
		readonly Int32VM data;

		public override string FormattedValue {
			get { return string.Format("{0:X8}", ReadDataFrom(doc)); }
		}

		public Int32HexField(HexDocument doc, string parentName, string name, ulong start)
			: base(doc, parentName, name, start, 4) {
			this.data = new Int32VM(doc.ReadInt32(start), a => UpdateValue());
		}

		protected override byte[] GetDataAsByteArray() {
			return BitConverter.GetBytes(data.Value);
		}

		protected override object ReadDataFrom(IHexStream stream) {
			return stream.ReadInt32(StartOffset);
		}
	}

	sealed class UInt32HexField : HexField {
		public override DataFieldVM DataFieldVM {
			get { return data; }
		}
		readonly UInt32VM data;

		public override string FormattedValue {
			get { return string.Format("{0:X8}", ReadDataFrom(doc)); }
		}

		public UInt32HexField(HexDocument doc, string parentName, string name, ulong start)
			: base(doc, parentName, name, start, 4) {
			this.data = new UInt32VM(doc.ReadUInt32(start), a => UpdateValue());
		}

		protected override byte[] GetDataAsByteArray() {
			return BitConverter.GetBytes(data.Value);
		}

		protected override object ReadDataFrom(IHexStream stream) {
			return stream.ReadUInt32(StartOffset);
		}
	}

	sealed class UInt64HexField : HexField {
		public override DataFieldVM DataFieldVM {
			get { return data; }
		}
		readonly UInt64VM data;

		public override string FormattedValue {
			get { return string.Format("{0:X16}", ReadDataFrom(doc)); }
		}

		public UInt64HexField(HexDocument doc, string parentName, string name, ulong start)
			: base(doc, parentName, name, start, 8) {
			this.data = new UInt64VM(doc.ReadUInt64(start), a => UpdateValue());
		}

		protected override byte[] GetDataAsByteArray() {
			return BitConverter.GetBytes(data.Value);
		}

		protected override object ReadDataFrom(IHexStream stream) {
			return stream.ReadUInt64(StartOffset);
		}
	}

	sealed class StringHexField : HexField {
		public override DataFieldVM DataFieldVM {
			get { return data; }
		}
		readonly StringVM data;

		public override string FormattedValue {
			get { return String; }
		}

		public string String {
			get {
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

		public StringHexField(HexDocument doc, string parentName, string name, ulong start, Encoding encoding, int dataLen)
			: base(doc, parentName, name, start, dataLen) {
			this.encoding = encoding;
            this.data = new StringVM((string)ReadDataFrom(doc), a => UpdateValue());
		}

		protected override byte[] GetDataAsByteArray() {
			var sd = encoding.GetBytes(data.Value);
			var d = new byte[EndOffset - StartOffset + 1];
			Array.Copy(sd, d, Math.Min(d.Length, sd.Length));
			return d;
		}

		protected override object ReadDataFrom(IHexStream stream) {
			return encoding.GetString(stream.ReadBytes(StartOffset, (int)(EndOffset - StartOffset + 1)));
		}
	}

	sealed class DataDirVM : ViewModelBase {
		public string Name {
			get { return name; }
		}
		readonly string name;

		public UInt32HexField RVAVM {
			get { return rvaVM; }
		}
		readonly UInt32HexField rvaVM;

		public UInt32HexField SizeVM {
			get { return sizeVM; }
		}
		readonly UInt32HexField sizeVM;

		public bool IsVisible {
			get { return isVisible; }
			set {
				if (isVisible != value) {
					isVisible = value;
					OnPropertyChanged("IsVisible");
				}
			}
		}
		bool isVisible = true;

		public DataDirVM(HexDocument doc, string parentName, string name, ulong start) {
			this.name = name;
			this.rvaVM = new UInt32HexField(doc, parentName, string.Format("{0} RVA", name), start);
			this.sizeVM = new UInt32HexField(doc, parentName, string.Format("{0} Size", name), start + 4);
		}
	}
}
