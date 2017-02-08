/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Text;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// #GUID heap record data
	/// </summary>
	public sealed class GuidHeapRecordData : GuidData {
		/// <summary>
		/// Gets the heap
		/// </summary>
		public GUIDHeap Heap { get; }

		/// <summary>
		/// Gets the GUID index (1-based)
		/// </summary>
		public uint Index { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="heap">Owner heap</param>
		/// <param name="index">Guid index (1-based)</param>
		public GuidHeapRecordData(HexBuffer buffer, HexPosition position, GUIDHeap heap, uint index)
			: base(buffer, position) {
			if (heap == null)
				throw new ArgumentNullException(nameof(heap));
			if (index == 0)
				throw new ArgumentOutOfRangeException(nameof(index));
			Heap = heap;
			Index = index;
		}
	}

	/// <summary>
	/// #Strings heap record data
	/// </summary>
	public sealed class StringsHeapRecordData : StructureData {
		const string NAME = "StringZ";

		/// <summary>
		/// Gets the owner heap
		/// </summary>
		public StringsHeap Heap { get; }

		/// <summary>
		/// Gets tokens of records referencing this string
		/// </summary>
		public ReadOnlyCollection<uint> Tokens { get; }

		/// <summary>
		/// Gets the string
		/// </summary>
		public StructField<StringData> String { get; }

		/// <summary>
		/// Gets the terminator or null if there's none
		/// </summary>
		public StructField<ByteData> Terminator { get; }

		/// <summary>
		/// Gets the fields
		/// </summary>
		protected override BufferField[] Fields { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="stringSpan">Span of string, not including the terminating zero</param>
		/// <param name="hasTerminatingZero">true if there's a terminating zero, false if there's no terminating zero
		/// or if the string is too long</param>
		/// <param name="heap">Owner heap</param>
		/// <param name="tokens">Tokens of records referencing this string</param>
		public StringsHeapRecordData(HexBuffer buffer, HexSpan stringSpan, bool hasTerminatingZero, StringsHeap heap, uint[] tokens)
			: base(NAME, new HexBufferSpan(buffer, HexSpan.FromBounds(stringSpan.Start, stringSpan.End + (hasTerminatingZero ? 1 : 0)))) {
			if (heap == null)
				throw new ArgumentNullException(nameof(heap));
			if (tokens == null)
				throw new ArgumentNullException(nameof(tokens));
			Heap = heap;
			Tokens = new ReadOnlyCollection<uint>(tokens);
			String = new StructField<StringData>("String", new StringData(new HexBufferSpan(buffer, stringSpan), Encoding.UTF8));
			if (hasTerminatingZero)
				Terminator = new StructField<ByteData>("Terminator", new ByteData(buffer, stringSpan.End));
			if (Terminator != null) {
				Fields = new BufferField[] {
					String,
					Terminator,
				};
			}
			else {
				Fields = new BufferField[] {
					String,
				};
			}
		}
	}

	/// <summary>
	/// #US heap record data
	/// </summary>
	public sealed class USHeapRecordData : StructureData {
		const string NAME = "US";

		/// <summary>
		/// Gets the owner heap
		/// </summary>
		public USHeap Heap { get; }

		/// <summary>
		/// Gets the length
		/// </summary>
		public StructField<BlobEncodedUInt32Data> Length { get; }

		/// <summary>
		/// Gets the string data
		/// </summary>
		public StructField<StringData> String { get; }

		/// <summary>
		/// Gets the terminal byte or null if none exists
		/// </summary>
		public StructField<ByteData> TerminalByte { get; }

		/// <summary>
		/// Gets the fields
		/// </summary>
		protected override BufferField[] Fields { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="lengthSpan">Span of length</param>
		/// <param name="stringSpan">Span of string data</param>
		/// <param name="terminalByteSpan">Span of terminal byte (0 or 1 byte)</param>
		/// <param name="heap">Owner heap</param>
		public USHeapRecordData(HexBuffer buffer, HexSpan lengthSpan, HexSpan stringSpan, HexSpan terminalByteSpan, USHeap heap)
			: base(NAME, new HexBufferSpan(buffer, HexSpan.FromBounds(lengthSpan.Start, terminalByteSpan.End))) {
			if (lengthSpan.End != stringSpan.Start)
				throw new ArgumentOutOfRangeException(nameof(stringSpan));
			if (stringSpan.End != terminalByteSpan.Start)
				throw new ArgumentOutOfRangeException(nameof(stringSpan));
			if (!terminalByteSpan.IsEmpty && terminalByteSpan.Length != 1)
				throw new ArgumentOutOfRangeException(nameof(terminalByteSpan));
			if (heap == null)
				throw new ArgumentNullException(nameof(heap));
			Heap = heap;
			Length = new StructField<BlobEncodedUInt32Data>("Length", new BlobEncodedUInt32Data(new HexBufferSpan(buffer, lengthSpan)));
			String = new StructField<StringData>("String", new StringData(new HexBufferSpan(buffer, stringSpan), Encoding.Unicode));
			if (!terminalByteSpan.IsEmpty)
				TerminalByte = new StructField<ByteData>("TerminalByte", new ByteData(new HexBufferSpan(buffer, terminalByteSpan)));
			if (TerminalByte != null) {
				Fields = new BufferField[] {
					Length,
					String,
					TerminalByte,
				};
			}
			else {
				Fields = new BufferField[] {
					Length,
					String,
				};
			}
		}
	}

	/// <summary>
	/// #Blob heap record data
	/// </summary>
	public sealed class BlobHeapRecordData : StructureData {
		const string NAME = "Blob";

		/// <summary>
		/// Gets the owner heap
		/// </summary>
		public BlobHeap Heap { get; }

		/// <summary>
		/// Gets the tokens referencing the blob or an empty collection if none (eg. referenced from data in the #Blob)
		/// </summary>
		public ReadOnlyCollection<uint> Tokens { get; }

		/// <summary>
		/// Gets the length
		/// </summary>
		public StructField<BlobEncodedUInt32Data> Length { get; }

		/// <summary>
		/// Gets the data
		/// </summary>
		public StructField<BufferData> Data { get; }

		/// <summary>
		/// Gets the fields
		/// </summary>
		protected override BufferField[] Fields { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="span">Span</param>
		/// <param name="lengthSpan">Span of length</param>
		/// <param name="data">Data</param>
		/// <param name="tokens">Tokens referencing this blob or an empty collection</param>
		/// <param name="heap">Owner heap</param>
		public BlobHeapRecordData(HexBuffer buffer, HexSpan span, HexSpan lengthSpan, BufferData data, ReadOnlyCollection<uint> tokens, BlobHeap heap)
			: base(NAME, new HexBufferSpan(buffer, span)) {
			if (lengthSpan.Start != span.Start)
				throw new ArgumentOutOfRangeException(nameof(lengthSpan));
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			if (tokens == null)
				throw new ArgumentNullException(nameof(tokens));
			if (heap == null)
				throw new ArgumentNullException(nameof(heap));
			Heap = heap;
			Tokens = tokens;
			Length = new StructField<BlobEncodedUInt32Data>("Length", new BlobEncodedUInt32Data(new HexBufferSpan(buffer, lengthSpan)));
			Data = new StructField<BufferData>("Data", data);
			Fields = new BufferField[] {
				Length,
				Data,
			};
		}
	}

	/// <summary>
	/// #Strings heap reference
	/// </summary>
	public abstract class StringsHeapData : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		protected StringsHeapData(HexBufferSpan span)
			: base(span) {
		}

		/// <summary>
		/// Reads the rid
		/// </summary>
		/// <returns></returns>
		protected abstract uint ReadOffset();

		/// <summary>
		/// Returns the span the field value references or null. The span can be empty.
		/// </summary>
		/// <param name="file">File</param>
		/// <returns></returns>
		public override HexSpan? GetFieldReferenceSpan(HexBufferFile file) {
			var stringsStream = file.GetHeaders<DotNetMetadataHeaders>()?.StringsStream;
			if (stringsStream == null)
				return null;
			uint offset = ReadOffset();
			if (offset >= stringsStream.Span.Length)
				return null;
			var pos = stringsStream.Span.Span.Start + offset;
			int len = GetStringLength(pos, stringsStream.Span.Span.End);
			return new HexSpan(pos, (ulong)len);
		}

		int GetStringLength(HexPosition position, HexPosition heapEnd) {
			var buffer = Span.Buffer;
			const int MAX_LEN = 0x400;
			var end = HexPosition.Min(heapEnd, position + MAX_LEN);
			var start = position;
			while (position < end) {
				if (buffer.ReadByte(position++) == 0)
					break;
			}
			return (int)(position - start).ToUInt64();
		}
	}

	/// <summary>
	/// 16-bit #Strings heap reference
	/// </summary>
	public class StringsHeapData16 : StringsHeapData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public StringsHeapData16(HexBufferSpan span)
			: base(span) {
			if (span.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public StringsHeapData16(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 2))) {
		}

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteUInt16((ushort)ReadOffset());

		/// <summary>
		/// Reads the token value
		/// </summary>
		/// <returns></returns>
		protected override uint ReadOffset() => Span.Buffer.ReadUInt16(Span.Start);
	}

	/// <summary>
	/// 32-bit #Strings heap reference
	/// </summary>
	public class StringsHeapData32 : StringsHeapData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public StringsHeapData32(HexBufferSpan span)
			: base(span) {
			if (span.Length != 4)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public StringsHeapData32(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 4))) {
		}

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteUInt32(ReadOffset());

		/// <summary>
		/// Reads the token value
		/// </summary>
		/// <returns></returns>
		protected override uint ReadOffset() => Span.Buffer.ReadUInt32(Span.Start);
	}

	/// <summary>
	/// #Blob heap reference
	/// </summary>
	public abstract class BlobHeapData : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		protected BlobHeapData(HexBufferSpan span)
			: base(span) {
		}

		/// <summary>
		/// Reads the rid
		/// </summary>
		/// <returns></returns>
		protected abstract uint ReadOffset();

		/// <summary>
		/// Returns the span the field value references or null. The span can be empty.
		/// </summary>
		/// <param name="file">File</param>
		/// <returns></returns>
		public override HexSpan? GetFieldReferenceSpan(HexBufferFile file) {
			var blobStream = file.GetHeaders<DotNetMetadataHeaders>()?.BlobStream;
			if (blobStream == null)
				return null;
			uint offset = ReadOffset();
			if (offset >= blobStream.Span.Length)
				return null;
			var start = blobStream.Span.Span.Start + offset;
			var pos = start;
			int size = (int)Utils.ReadCompressedUInt32(Span.Buffer, ref pos);
			ulong blobSize = pos + size > blobStream.Span.Span.End ? 0 : (ulong)size + (pos - start).ToUInt64();
			return new HexSpan(start, blobSize);
		}
	}

	/// <summary>
	/// 16-bit #Blob heap reference
	/// </summary>
	public class BlobHeapData16 : BlobHeapData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public BlobHeapData16(HexBufferSpan span)
			: base(span) {
			if (span.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public BlobHeapData16(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 2))) {
		}

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteUInt16((ushort)ReadOffset());

		/// <summary>
		/// Reads the token value
		/// </summary>
		/// <returns></returns>
		protected override uint ReadOffset() => Span.Buffer.ReadUInt16(Span.Start);
	}

	/// <summary>
	/// 32-bit #Blob heap reference
	/// </summary>
	public class BlobHeapData32 : BlobHeapData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public BlobHeapData32(HexBufferSpan span)
			: base(span) {
			if (span.Length != 4)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public BlobHeapData32(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 4))) {
		}

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteUInt32(ReadOffset());

		/// <summary>
		/// Reads the token value
		/// </summary>
		/// <returns></returns>
		protected override uint ReadOffset() => Span.Buffer.ReadUInt32(Span.Start);
	}

	/// <summary>
	/// #GUID heap reference
	/// </summary>
	public abstract class GUIDHeapData : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		protected GUIDHeapData(HexBufferSpan span)
			: base(span) {
		}

		/// <summary>
		/// Reads the rid
		/// </summary>
		/// <returns></returns>
		protected abstract uint ReadIndex();

		/// <summary>
		/// Returns the span the field value references or null. The span can be empty.
		/// </summary>
		/// <param name="file">File</param>
		/// <returns></returns>
		public override HexSpan? GetFieldReferenceSpan(HexBufferFile file) {
			var guidStream = file.GetHeaders<DotNetMetadataHeaders>()?.GUIDStream;
			if (guidStream == null)
				return null;
			uint index = ReadIndex();
			if (index == 0 || !guidStream.IsValidIndex(index))
				return null;
			return new HexSpan(guidStream.Span.Span.Start + (index - 1) * 16, 16);
		}
	}

	/// <summary>
	/// 16-bit #GUID heap reference
	/// </summary>
	public class GUIDHeapData16 : GUIDHeapData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public GUIDHeapData16(HexBufferSpan span)
			: base(span) {
			if (span.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public GUIDHeapData16(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 2))) {
		}

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteUInt16((ushort)ReadIndex());

		/// <summary>
		/// Reads the token value
		/// </summary>
		/// <returns></returns>
		protected override uint ReadIndex() => Span.Buffer.ReadUInt16(Span.Start);
	}

	/// <summary>
	/// 32-bit #GUID heap reference
	/// </summary>
	public class GUIDHeapData32 : GUIDHeapData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public GUIDHeapData32(HexBufferSpan span)
			: base(span) {
			if (span.Length != 4)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public GUIDHeapData32(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 4))) {
		}

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteUInt32(ReadIndex());

		/// <summary>
		/// Reads the token value
		/// </summary>
		/// <returns></returns>
		protected override uint ReadIndex() => Span.Buffer.ReadUInt32(Span.Start);
	}
}
