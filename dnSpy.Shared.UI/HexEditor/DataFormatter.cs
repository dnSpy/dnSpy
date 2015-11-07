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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace dnSpy.Shared.UI.HexEditor {
	abstract class DataFormatter {
		protected readonly HexBox hexBox;
		protected readonly ulong start;
		protected readonly ulong end;

		const ulong MAX_BYTES = 50 * 1024 * 1024;

		protected DataFormatter(HexBox hexBox, ulong start, ulong end) {
			if (start < hexBox.StartOffset)
				start = hexBox.StartOffset;
			else if (start > hexBox.EndOffset)
				start = hexBox.EndOffset;
			if (end < hexBox.StartOffset)
				end = hexBox.StartOffset;
			else if (end > hexBox.EndOffset)
				end = hexBox.EndOffset;
			if (end < start)
				end = start;

			if (end - start >= MAX_BYTES - 1)
				end = start + MAX_BYTES - 1;

			this.hexBox = hexBox;
			this.start = start;
			this.end = end;
		}

		public void CopyToClipboard() {
			if (hexBox.Document == null)
				return;

			try {
				var dataObj = new DataObject();
				InitializeDataObject(dataObj);
				Clipboard.SetDataObject(dataObj, true);
			}
			catch (OutOfMemoryException) {
				//TODO: Show a msg box
				try {
					Clipboard.SetText("Out of memory");
				}
				catch (ExternalException) {
				}
			}
			catch (ExternalException) {
			}
		}

		protected abstract void InitializeDataObject(DataObject dataObj);

		protected byte[] ReadByteArray() {
			return hexBox.Document.ReadBytes(start, (int)(end - start + 1));
		}

		protected int ReadByte(ulong offset) {
			return hexBox.Document.ReadByte(offset);
		}
	}

	sealed class HexStringFormatter : DataFormatter {
		readonly string hexByteFormatString;

		public HexStringFormatter(HexBox hexBox, ulong start, ulong end, bool lowerHex) : base(hexBox, start, end) {
			this.hexByteFormatString = lowerHex ? "{0:x2}" : "{0:X2}";
		}

		protected override void InitializeDataObject(DataObject dataObj) {
			var sb = new StringBuilder();

			ulong offs = start;
			for (;;) {
				int b = ReadByte(offs);
				if (b < 0)
					sb.Append("??");
				else
					sb.Append(string.Format(hexByteFormatString, (byte)b));

				if (offs++ >= end)
					break;
			}

			dataObj.SetText(sb.ToString());
		}
	}

	sealed class UTF8StringFormatter : DataFormatter {
		public UTF8StringFormatter(HexBox hexBox, ulong start, ulong end) : base(hexBox, start, end) {
		}

		protected override void InitializeDataObject(DataObject dataObj) {
			var s = Encoding.UTF8.GetString(ReadByteArray());
			dataObj.SetText(s);
		}
	}

	sealed class UnicodeStringFormatter : DataFormatter {
		public UnicodeStringFormatter(HexBox hexBox, ulong start, ulong end) : base(hexBox, start, end) {
		}

		protected override void InitializeDataObject(DataObject dataObj) {
			var s = Encoding.Unicode.GetString(ReadByteArray());
			dataObj.SetText(s);
		}
	}

	abstract class LanguageArrayFormatter : DataFormatter {
		public const int BYTES_PER_LINE = 16;

		protected string allocStringStart;
		protected string allocStringEnd;
		protected string unknownHex;
		protected string hexFormat;
		protected string eol = Environment.NewLine;

		protected LanguageArrayFormatter(HexBox hexBox, ulong start, ulong end) : base(hexBox, start, end) {
		}

		protected override void InitializeDataObject(DataObject dataObj) {
			var sb = new StringBuilder();

			sb.Append(allocStringStart);
			sb.Append(eol);
			ulong offs = start;
			for (int i = 0; ; i++) {
				if (i >= BYTES_PER_LINE) {
					i = 0;
					sb.Append(eol);
				}
				if (i == 0)
					sb.Append('\t');
				else
					sb.Append(' ');

				int b = ReadByte(offs);
				if (b < 0)
					sb.Append(unknownHex);
				else
					sb.Append(string.Format(hexFormat, (byte)b));

				if (offs++ >= end)
					break;
				sb.Append(',');
			}
			sb.Append(eol);
			sb.Append(allocStringEnd);
			sb.AppendLine();

			dataObj.SetText(sb.ToString());
		}
	}

	sealed class CSharpArrayFormatter : LanguageArrayFormatter {
		public CSharpArrayFormatter(HexBox hexBox, ulong start, ulong end, bool lowerHex) : base(hexBox, start, end) {
			allocStringStart = "new byte[] {";
			allocStringEnd = "};";
			unknownHex = "0x??";
			hexFormat = lowerHex ? "0x{0:x2}" : "0x{0:X2}";
		}
	}

	sealed class VBArrayFormatter : LanguageArrayFormatter {
		public VBArrayFormatter(HexBox hexBox, ulong start, ulong end, bool lowerHex) : base(hexBox, start, end) {
			allocStringStart = "New Byte() {";
			allocStringEnd = "}";
			unknownHex = "&H??";
			hexFormat = lowerHex ? "&H{0:x2}" : "&H{0:X2}";
			eol = " _" + Environment.NewLine;
		}
	}

	sealed class UILayoutFormatter : DataFormatter {
		public UILayoutFormatter(HexBox hexBox, ulong start, ulong end) : base(hexBox, start, end) {
		}

		protected override void InitializeDataObject(DataObject dataObj) {
			var hexLines = hexBox.CreateHexLines(start, end);
			CopyText(dataObj, hexLines);
			CopyHtml(dataObj, hexLines);
		}

		void CopyText(DataObject dataObj, List<HexLine> lines) {
			var sb = new StringBuilder(lines.Count == 0 ? 0 : lines.Count * (lines[0].Text.Length + Environment.NewLine.Length));

			foreach (var line in lines) {
				sb.Append(line.Text);
				sb.AppendLine();
			}

			dataObj.SetText(sb.ToString());
		}

		void CopyHtml(DataObject dataObj, List<HexLine> lines) {
			var writer = new HtmlClipboardFormatWriter();

			var cssWriter = new StringBuilder();
			foreach (var line in lines) {
				foreach (var part in line.LineParts)
					Write(writer, line.Text, part, cssWriter);
				writer.WriteBr();
			}

			dataObj.SetData(DataFormats.Html, writer.ToString());
		}

		void Write(HtmlClipboardFormatWriter writer, string line, HexLinePart part, StringBuilder cssWriter) {
			WriteCss(cssWriter, part);
			writer.WriteSpan(cssWriter.ToString(), line.Substring(part.Offset, part.Length));
		}

		void WriteCss(StringBuilder writer, HexLinePart part) {
			writer.Clear();

			WriteCssColor(writer, "color", part.TextRunProperties.ForegroundBrush);

			var tf = part.TextRunProperties.Typeface;
			if (tf.Weight != FontWeights.Normal)
				writer.Append(string.Format("font-weight: {0}; ", tf.Weight.ToString().ToLowerInvariant()));
			if (tf.Style != FontStyles.Normal)
				writer.Append(string.Format("font-style: {0}; ", tf.Style.ToString().ToLowerInvariant()));
		}

		void WriteCssColor(StringBuilder writer, string name, Brush brush) {
			var scb = brush as SolidColorBrush;
			if (scb != null)
				writer.Append(string.Format(name + ": rgb({0}, {1}, {2}); ", scb.Color.R, scb.Color.G, scb.Color.B));
		}
	}
}
