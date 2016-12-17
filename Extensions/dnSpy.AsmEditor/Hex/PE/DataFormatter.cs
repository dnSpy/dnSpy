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
using dnlib.DotNet;
using dnSpy.Contracts.Text;

namespace dnSpy.AsmEditor.Hex.PE {
	struct DataFormatter {
		readonly ITextColorWriter output;

		public DataFormatter(ITextColorWriter output) {
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			this.output = output;
		}

		public void Write(object color, string text) => output.Write(color, text);
		public void Write(TextColor color, string text) => output.Write(color, text);
		public void WriteSpace() => output.WriteSpace();

		public void WriteToken(uint token) {
			output.Write(BoxedTextColor.Number, "0x" + token.ToString("X8"));
			output.WriteSpace();
			output.Write(BoxedTextColor.Punctuation, "(");
			var mdToken = new MDToken(token);
			output.Write(BoxedTextColor.Enum, mdToken.Table.ToString());
			output.Write(BoxedTextColor.Punctuation, ",");
			output.WriteSpace();
			output.Write(BoxedTextColor.Number, mdToken.Rid.ToString());
			output.Write(BoxedTextColor.Punctuation, ")");
		}

		void WriteShortNumber(uint value) {
			if (value <= 9)
				output.Write(BoxedTextColor.Number, value.ToString());
			else
				output.Write(BoxedTextColor.Number, "0x" + value.ToString("X"));
		}

		public void WriteByte(byte value) {
			output.Write(BoxedTextColor.Number, "0x" + value.ToString("X2"));
			output.WriteSpace();
			output.Write(BoxedTextColor.Punctuation, "(");
			output.Write(BoxedTextColor.Number, value.ToString());
			output.Write(BoxedTextColor.Punctuation, ")");
		}

		public void WriteUInt16(ushort value) {
			output.Write(BoxedTextColor.Number, "0x" + value.ToString("X4"));
			output.WriteSpace();
			output.Write(BoxedTextColor.Punctuation, "(");
			output.Write(BoxedTextColor.Number, value.ToString());
			output.Write(BoxedTextColor.Punctuation, ")");
		}

		public void WriteUInt24(uint value) {
			output.Write(BoxedTextColor.Number, "0x" + value.ToString("X6"));
			output.WriteSpace();
			output.Write(BoxedTextColor.Punctuation, "(");
			output.Write(BoxedTextColor.Number, value.ToString());
			output.Write(BoxedTextColor.Punctuation, ")");
		}

		public void WriteUInt32(uint value) {
			output.Write(BoxedTextColor.Number, "0x" + value.ToString("X8"));
			output.WriteSpace();
			output.Write(BoxedTextColor.Punctuation, "(");
			output.Write(BoxedTextColor.Number, value.ToString());
			output.Write(BoxedTextColor.Punctuation, ")");
		}

		public void WriteUInt64(ulong value) {
			output.Write(BoxedTextColor.Number, "0x" + value.ToString("X16"));
			output.WriteSpace();
			output.Write(BoxedTextColor.Punctuation, "(");
			output.Write(BoxedTextColor.Number, value.ToString());
			output.Write(BoxedTextColor.Punctuation, ")");
		}

		public void WriteEquals() {
			output.WriteSpace();
			output.Write(BoxedTextColor.Operator, "=");
			output.WriteSpace();
		}

		public void WriteStructAndErrorField(string structName) {
			output.Write(BoxedTextColor.ValueType, structName);
			output.Write(BoxedTextColor.Operator, ".");
			output.Write(BoxedTextColor.Error, "???");
		}

		public void WriteStructAndField(string structName, string fieldName) {
			output.Write(BoxedTextColor.ValueType, structName);
			output.Write(BoxedTextColor.Operator, ".");
			output.Write(BoxedTextColor.InstanceField, fieldName);
		}

		public void WriteFlags(uint value, FlagInfo[] flagInfos) {
			uint checkedBits = 0;
			foreach (var info in flagInfos) {
				if ((value & info.Mask) == info.Value && (info.Mask & checkedBits) == 0) {
					if (checkedBits != 0) {
						output.WriteSpace();
						output.Write(BoxedTextColor.Operator, "|");
						output.WriteSpace();
					}
					output.Write(BoxedTextColor.EnumField, info.Name);

					output.WriteSpace();
					output.Write(BoxedTextColor.Punctuation, "(");
					WriteShortNumber(info.Value);
					output.Write(BoxedTextColor.Punctuation, ")");

					checkedBits |= info.Mask;
				}
			}
			if ((value & ~checkedBits) != 0 || checkedBits == 0) {
				if (checkedBits != 0) {
					output.WriteSpace();
					output.Write(BoxedTextColor.Operator, "|");
					output.WriteSpace();
				}
				WriteShortNumber(value & ~checkedBits);
			}
		}
	}

	struct FlagInfo {
		public string Name { get; }
		public uint Mask { get; }
		public uint Value { get; }

		public FlagInfo(string name, uint bitMask)
			: this(name, bitMask, bitMask) {
		}

		public FlagInfo(string name, uint mask, uint value) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			if (mask == 0)
				throw new ArgumentOutOfRangeException(nameof(mask));
			Name = name;
			Mask = mask;
			Value = value;
		}
	}
}
