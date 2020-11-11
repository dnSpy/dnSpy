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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Hex.Text;

namespace dnSpy.Hex.Files {
	[Export(typeof(HexFieldFormatterFactory))]
	sealed class HexFieldFormatterFactoryImpl : HexFieldFormatterFactory {
		public override HexFieldFormatter Create(HexTextWriter writer, HexFieldFormatterOptions options, HexNumberOptions arrayIndexOptions, HexNumberOptions valueNumberOptions) {
			if (writer is null)
				throw new ArgumentNullException(nameof(writer));
			return new HexFieldFormatterImpl(writer, options, new NumberFormatter(arrayIndexOptions), new NumberFormatter(valueNumberOptions));
		}
	}

	sealed class HexFieldFormatterImpl : HexFieldFormatter {
		readonly HexTextWriter writer;
		readonly HexFieldFormatterOptions options;
		readonly NumberFormatter arrayIndexFormatter;
		readonly NumberFormatter numberFormatter;
		readonly NumberFormatter numberFormatterShort;
		readonly NumberFormatter tokenFormatter;

		public HexFieldFormatterImpl(HexTextWriter writer, HexFieldFormatterOptions options, NumberFormatter arrayIndexFormatter, NumberFormatter numberFormatter) {
			this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
			this.options = options;
			this.arrayIndexFormatter = arrayIndexFormatter;
			this.numberFormatter = numberFormatter;
			numberFormatterShort = new NumberFormatter(numberFormatter.Options | HexNumberOptions.MinimumDigits);

			var tokenOptions = numberFormatter.Options & ~HexNumberOptions.MinimumDigits;
			if ((tokenOptions & HexNumberOptions.NumberBaseMask) == HexNumberOptions.Decimal)
				tokenOptions |= HexNumberOptions.HexCSharp;
			tokenFormatter = new NumberFormatter(tokenOptions);
		}

		public override void Write(string text, string tag) => writer.Write(text, tag);

		public override void WriteEquals() {
			writer.WriteSpace();
			writer.Write("=", PredefinedClassifiedTextTags.Operator);
			writer.WriteSpace();
		}

		public override void WriteField(string name) {
			writer.Write(".", PredefinedClassifiedTextTags.Operator);
			Write(name, PredefinedClassifiedTextTags.Field);
		}

		public override void WriteArrayField(uint index) {
			writer.Write("[", PredefinedClassifiedTextTags.Punctuation);
			writer.Write(arrayIndexFormatter.ToString(index), PredefinedClassifiedTextTags.Number);
			writer.Write("]", PredefinedClassifiedTextTags.Punctuation);
		}

		public override void WriteField(ComplexData structure, HexPosition position) {
			if (structure is null)
				throw new ArgumentNullException(nameof(structure));
			if (!structure.Span.Span.Contains(position))
				throw new ArgumentOutOfRangeException(nameof(position));
			structure.WriteName(this);
			ComplexData? str = structure;
			for (;;) {
				var field = str.GetFieldByPosition(position);
				if (field is null)
					break;
				field.WriteName(this);
				str = field.Data as ComplexData;
				if (str is null)
					break;
			}
		}

		public override void WriteValue(ComplexData structure, HexPosition position) {
			if (structure is null)
				throw new ArgumentNullException(nameof(structure));
			if (!structure.Span.Span.Contains(position))
				throw new ArgumentOutOfRangeException(nameof(position));
			var field = structure.GetSimpleField(position);
			Debug2.Assert(field is not null);
			if (field is null)
				return;
			var data = field.Data as SimpleData;
			Debug2.Assert(data is not null);
			if (data is null)
				return;
			try {
				data.WriteValue(this);
				return;
			}
			catch (ArithmeticException) {
			}
			catch (OutOfMemoryException) {
			}
			WriteUnknownValue();
		}

		public override void WriteToken(uint token) {
			writer.Write(tokenFormatter.ToString(token), PredefinedClassifiedTextTags.Number);
			writer.WriteSpace();
			writer.Write("(", PredefinedClassifiedTextTags.Punctuation);
			var mdToken = new MDToken(token);
			writer.Write(mdToken.Table.ToString(), PredefinedClassifiedTextTags.Enum);
			writer.Write(",", PredefinedClassifiedTextTags.Punctuation);
			writer.WriteSpace();
			writer.Write(mdToken.Rid.ToString(), PredefinedClassifiedTextTags.Number);
			writer.Write(")", PredefinedClassifiedTextTags.Punctuation);
		}

		void WriteShortNumber(ulong value) => writer.Write(numberFormatterShort.ToString(value), PredefinedClassifiedTextTags.Number);

		void WriteValue(string text, ulong value) {
			writer.Write(text, PredefinedClassifiedTextTags.Number);

			if ((options & HexFieldFormatterOptions.DontPrintDecimalValueInParens) == 0 && (numberFormatter.Options & HexNumberOptions.NumberBaseMask) != HexNumberOptions.Decimal) {
				writer.WriteSpace();
				writer.Write("(", PredefinedClassifiedTextTags.Punctuation);
				writer.Write(value.ToString(), PredefinedClassifiedTextTags.Number);
				writer.Write(")", PredefinedClassifiedTextTags.Punctuation);
			}
		}

		void WriteValue(string text, long value) {
			writer.Write(text, PredefinedClassifiedTextTags.Number);

			if ((options & HexFieldFormatterOptions.DontPrintDecimalValueInParens) == 0 && (numberFormatter.Options & HexNumberOptions.NumberBaseMask) != HexNumberOptions.Decimal) {
				writer.WriteSpace();
				writer.Write("(", PredefinedClassifiedTextTags.Punctuation);
				writer.Write(value.ToString(), PredefinedClassifiedTextTags.Number);
				writer.Write(")", PredefinedClassifiedTextTags.Punctuation);
			}
		}

		public override void WriteBoolean(bool value) => writer.Write(value ? "true" : "false", PredefinedClassifiedTextTags.Keyword);
		public override void WriteChar(char value) => writer.Write(numberFormatter.ToString(value), PredefinedClassifiedTextTags.Char);
		public override void WriteByte(byte value) => WriteValue(numberFormatter.ToString(value), (ulong)value);
		public override void WriteUInt16(ushort value) => WriteValue(numberFormatter.ToString(value), (ulong)value);
		public override void WriteUInt24(uint value) => WriteValue(numberFormatter.ToString24(value), (ulong)value);
		public override void WriteUInt32(uint value) => WriteValue(numberFormatter.ToString(value), (ulong)value);
		public override void WriteUInt64(ulong value) => WriteValue(numberFormatter.ToString(value), value);
		public override void WriteSByte(sbyte value) => WriteValue(numberFormatter.ToString(value), value);
		public override void WriteInt16(short value) => WriteValue(numberFormatter.ToString(value), value);
		public override void WriteInt32(int value) => WriteValue(numberFormatter.ToString(value), value);
		public override void WriteInt64(long value) => WriteValue(numberFormatter.ToString(value), value);
		public override void WriteSingle(float value) => writer.Write(numberFormatter.ToString(value), PredefinedClassifiedTextTags.Number);
		public override void WriteDouble(double value) => writer.Write(numberFormatter.ToString(value), PredefinedClassifiedTextTags.Number);
		public override void WriteString(string value) => writer.Write(numberFormatter.ToString(FilterStringLength(value)), PredefinedClassifiedTextTags.String);
		public override void WriteDecimal(decimal value) => writer.Write(numberFormatter.ToString(value), PredefinedClassifiedTextTags.Number);

		public override void WriteFlags(ulong value, ReadOnlyCollection<FlagInfo> infos) {
			ulong checkedBits = 0;
			int count = infos.Count;
			for (int i = 0; i < count; i++) {
				var info = infos[i];
				if (info.IsEnumName)
					continue;
				if ((value & info.Mask) == info.Value && (info.Mask & checkedBits) == 0) {
					if (checkedBits != 0) {
						writer.WriteSpace();
						writer.Write("|", PredefinedClassifiedTextTags.Operator);
						writer.WriteSpace();
					}
					writer.Write(info.Name, PredefinedClassifiedTextTags.EnumField);

					if ((options & HexFieldFormatterOptions.DontPrintFlagValueInParens) == 0) {
						writer.WriteSpace();
						writer.Write("(", PredefinedClassifiedTextTags.Punctuation);
						WriteShortNumber(info.Value);
						writer.Write(")", PredefinedClassifiedTextTags.Punctuation);
					}

					checkedBits |= info.Mask;
				}
			}
			if ((value & ~checkedBits) != 0 || checkedBits == 0) {
				if (checkedBits != 0) {
					writer.WriteSpace();
					writer.Write("|", PredefinedClassifiedTextTags.Operator);
					writer.WriteSpace();
				}
				WriteShortNumber(value & ~checkedBits);
			}
		}

		public override void WriteEnum(ulong value, ReadOnlyCollection<EnumFieldInfo> infos) {
			int count = infos.Count;
			for (int i = 0; i < count; i++) {
				var info = infos[i];
				if (info.Value == value) {
					writer.Write(info.Name, PredefinedClassifiedTextTags.EnumField);

					if ((options & HexFieldFormatterOptions.DontPrintEnumValueInParens) == 0) {
						writer.WriteSpace();
						writer.Write("(", PredefinedClassifiedTextTags.Punctuation);
						WriteShortNumber(info.Value);
						writer.Write(")", PredefinedClassifiedTextTags.Punctuation);
					}
					return;
				}
			}
			WriteShortNumber(value);
		}

		public override void WriteFilename(string filename) {
			if (filename is null)
				throw new ArgumentNullException(nameof(filename));
			filename = FilterStringLength(filename);
			var parts = filename.Split(pathSeparators);
			int index = 0;
			for (int i = 0; i < parts.Length - 1; i++) {
				var part = parts[i];
				writer.Write(part, PredefinedClassifiedTextTags.PathName);
				index += part.Length + 1;
				writer.Write(filename.Substring(index - 1, 1), PredefinedClassifiedTextTags.PathSeparator);
			}

			var name = parts[parts.Length - 1];
			int dot = name.LastIndexOf('.');
			if (dot < 0)
				writer.Write(name, PredefinedClassifiedTextTags.Filename);
			else {
				writer.Write(name.Substring(0, dot), PredefinedClassifiedTextTags.Filename);
				writer.Write(name.Substring(dot, 1), PredefinedClassifiedTextTags.FileDot);
				writer.Write(name.Substring(dot + 1), PredefinedClassifiedTextTags.FileExtension);
			}
		}
		static readonly char[] pathSeparators = new[] { '/', '\\' };

		string FilterStringLength(string s) {
			const int MAX = 1024;
			if (s.Length <= MAX)
				return s;
			return s.Substring(0, MAX) + "[...]";
		}

		public override void WriteUnknownValue() => writer.Write("???", PredefinedClassifiedTextTags.Error);
	}
}
