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
using System.Globalization;
using dnSpy.Contracts.Hex.Text;

namespace dnSpy.Contracts.Hex.Files.PE {
	/// <summary>
	/// 32-bit Unix (epoch) time
	/// </summary>
	public class UnixTime32Data : UInt32Data {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public UnixTime32Data(HexBufferSpan span)
			: base(span) {
			if (span.Length != 4)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public UnixTime32Data(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 4))) {
		}

		/// <summary>
		/// Reads the value as a <see cref="DateTime"/>
		/// </summary>
		/// <returns></returns>
		public DateTime ReadDateTime() => Epoch.AddSeconds(ReadValue());

		static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) {
			formatter.WriteUInt32(ReadValue());
			formatter.WriteSpace();
			formatter.Write("(", PredefinedClassifiedTextTags.Punctuation);
			formatter.Write(ReadDateTime().ToLocalTime().ToString(CultureInfo.CurrentCulture.DateTimeFormat), PredefinedClassifiedTextTags.Text);
			formatter.Write(")", PredefinedClassifiedTextTags.Punctuation);
		}
	}

	/// <summary>
	/// RVA data
	/// </summary>
	public class RvaData : UInt32Data {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public RvaData(HexBufferSpan span)
			: base(span) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public RvaData(HexBuffer buffer, HexPosition position)
			: base(buffer, position) {
		}

		/// <summary>
		/// Returns the span the field value references or null. The span can be empty.
		/// </summary>
		/// <param name="file">File</param>
		/// <returns></returns>
		public override HexSpan? GetFieldReferenceSpan(HexBufferFile file) {
			var peHeaders = file.GetHeaders<PeHeaders>();
			if (peHeaders == null)
				return null;
			var rva = ReadValue();
			if (rva == 0)
				return null;
			return new HexSpan(peHeaders.RvaToBufferPosition(rva), 0);
		}
	}

	/// <summary>
	/// 32-bit file offset
	/// </summary>
	public class FileOffsetData : UInt32Data {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public FileOffsetData(HexBufferSpan span)
			: base(span) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public FileOffsetData(HexBuffer buffer, HexPosition position)
			: base(buffer, position) {
		}

		/// <summary>
		/// Returns the span the field value references or null. The span can be empty.
		/// </summary>
		/// <param name="file">File</param>
		/// <returns></returns>
		public override HexSpan? GetFieldReferenceSpan(HexBufferFile file) {
			var offset = ReadValue();
			if (offset == 0)
				return null;
			var pos = file.GetHeaders<PeHeaders>()?.FilePositionToBufferPosition(offset) ?? HexPosition.Min(HexPosition.MaxEndPosition - 1, file.Span.Start + offset);
			return new HexSpan(pos, 0);
		}
	}
}
