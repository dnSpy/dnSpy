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
using dnSpy.Contracts.Hex.Text;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// A <see cref="DateTime"/>
	/// </summary>
	public class DateTimeData : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public DateTimeData(HexBufferSpan span)
			: base(span) {
			if (span.Length != 8)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public DateTimeData(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 8))) {
		}

		static DateTime ReadDateTime(HexBuffer buffer, HexPosition position) =>
			DateTime.FromBinary(buffer.ReadInt64(position));

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public DateTime ReadValue() => ReadDateTime(Span.Buffer, Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) {
			formatter.WriteUInt64(Span.Buffer.ReadUInt64(Span.Start));
			formatter.WriteSpace();
			formatter.Write("(", PredefinedClassifiedTextTags.Punctuation);
			formatter.Write(ReadValue().ToString(), PredefinedClassifiedTextTags.Text);
			formatter.Write(")", PredefinedClassifiedTextTags.Punctuation);
		}
	}

	/// <summary>
	/// A <see cref="TimeSpan"/>
	/// </summary>
	public class TimeSpanData : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public TimeSpanData(HexBufferSpan span)
			: base(span) {
			if (span.Length != 8)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public TimeSpanData(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 8))) {
		}

		static TimeSpan ReadTimeSpan(HexBuffer buffer, HexPosition position) =>
			new TimeSpan(buffer.ReadInt64(position));

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public TimeSpan ReadValue() => ReadTimeSpan(Span.Buffer, Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) {
			formatter.WriteUInt64(Span.Buffer.ReadUInt64(Span.Start));
			formatter.WriteSpace();
			formatter.Write("(", PredefinedClassifiedTextTags.Punctuation);
			formatter.Write(ReadValue().ToString(), PredefinedClassifiedTextTags.Text);
			formatter.Write(")", PredefinedClassifiedTextTags.Punctuation);
		}
	}

	/// <summary>
	/// Portable PDB Id (20 bytes)
	/// </summary>
	public sealed class PortablePdbIdData : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public PortablePdbIdData(HexBufferSpan span)
			: base(span) {
			if (span.Length != 20)
				throw new ArgumentOutOfRangeException();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public PortablePdbIdData(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 20))) {
		}

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) {
			var buffer = Span.Buffer;
			var pos = Span.Span.Start;
			var end = Span.Span.End;
			while (pos < end)
				formatter.Write(buffer.ReadByte(pos++).ToString("X2"), PredefinedClassifiedTextTags.Number);
		}
	}
}
