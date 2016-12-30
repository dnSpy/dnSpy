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
using System.Collections.ObjectModel;
using System.Text;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// A .NET heap
	/// </summary>
	public abstract class DotNetHeap {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Heap span</param>
		/// <param name="heapKind">Heap kind</param>
		protected DotNetHeap(HexBufferSpan span, DotNetHeapKind heapKind) {
			if (span.IsDefault)
				throw new ArgumentException();
			Span = span;
			HeapKind = heapKind;
		}

		/// <summary>
		/// Gets the heap span
		/// </summary>
		public HexBufferSpan Span { get; }

		/// <summary>
		/// Gets the heap kind
		/// </summary>
		public DotNetHeapKind HeapKind { get; }

		/// <summary>
		/// Gets the metadata headers
		/// </summary>
		public abstract DotNetMetadataHeaders Metadata { get; }

		/// <summary>
		/// Gets a structure or null 
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public virtual ComplexData GetStructure(HexPosition position) => null;

		/// <summary>
		/// Checks whether <paramref name="offset"/> is valid. Note that some heaps
		/// treat <paramref name="offset"/> as an index, eg. <see cref="GUIDHeap"/>.
		/// </summary>
		/// <param name="offset">Offset (or index if #GUID heap)</param>
		/// <returns></returns>
		public virtual bool IsValidOffset(uint offset) => offset == 0 || offset < Span.Length;

		/// <summary>
		/// Reads a compressed <see cref="uint"/> and increments <paramref name="position"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		protected int? ReadCompressedUInt32(ref HexPosition position) {
			if (!Span.Contains(position))
				return null;
			var res = Utils.ReadCompressedUInt32(Span.Buffer, ref position);
			if (position > Span.Span.End)
				return null;
			return (int)res;
		}
	}

	/// <summary>
	/// .NET tables (#~ or #-) heap
	/// </summary>
	public abstract class TablesHeap : DotNetHeap {
		/// <summary>
		/// Gets the metadata type
		/// </summary>
		public TablesHeapType TablesHeapType { get; }

		/// <summary>
		/// Gets all metadata table infos
		/// </summary>
		public abstract ReadOnlyCollection<MDTable> MDTables { get; }

		/// <summary>
		/// Span of header
		/// </summary>
		public abstract HexSpan HeaderSpan { get; }

		/// <summary>
		/// Span of all tables
		/// </summary>
		public abstract HexSpan TablesSpan { get; }

		/// <summary>
		/// Gets the header
		/// </summary>
		public abstract TablesHeaderData Header { get; }

		/// <summary>
		/// Gets the major version, this value is cached
		/// </summary>
		public abstract byte MajorVersion { get; }

		/// <summary>
		/// Gets the minor version, this value is cached
		/// </summary>
		public abstract byte MinorVersion { get; }

		/// <summary>
		/// Gets the flags, this value is cached
		/// </summary>
		public abstract MDStreamFlags Flags { get; }

		/// <summary>
		/// Gets the valid mask, this value is cached
		/// </summary>
		public abstract ulong ValidMask { get; }

		/// <summary>
		/// Gets the sorted mask, this value is cached
		/// </summary>
		public abstract ulong SortedMask { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Heap span</param>
		/// <param name="tablesHeapType">Tables heap type</param>
		protected TablesHeap(HexBufferSpan span, TablesHeapType tablesHeapType)
			: base(span, DotNetHeapKind.Tables) {
			TablesHeapType = tablesHeapType;
		}

		/// <summary>
		/// Gets a record or null if <paramref name="token"/> is invalid
		/// </summary>
		/// <param name="token">Token</param>
		/// <returns></returns>
		public TableRecordData GetRecord(uint token) => GetRecord(new MDToken(token));

		/// <summary>
		/// Gets a record or null if <paramref name="token"/> is invalid
		/// </summary>
		/// <param name="token">Token</param>
		/// <returns></returns>
		public abstract TableRecordData GetRecord(MDToken token);
	}

	/// <summary>
	/// .NET #Strings heap
	/// </summary>
	public abstract class StringsHeap : DotNetHeap {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Heap span</param>
		protected StringsHeap(HexBufferSpan span)
			: base(span, DotNetHeapKind.Strings) {
		}

		/// <summary>
		/// Gets the span of the string, not including the terminating zero byte.
		/// Returns an empty span if <paramref name="offset"/> is invalid.
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <returns></returns>
		public HexBufferSpan GetStringSpan(uint offset) => GetStringSpan(offset, int.MaxValue);

		/// <summary>
		/// Gets the span of the string, not including the terminating zero byte.
		/// Returns an empty span if <paramref name="offset"/> is invalid.
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <param name="maxByteLength">Maximum number of bytes to read</param>
		/// <returns></returns>
		public HexBufferSpan GetStringSpan(uint offset, int maxByteLength) {
			var start = Span.Span.Start + offset;
			var pos = start;
			var buffer = Span.Buffer;
			var end = HexPosition.Min(Span.Span.End, start + maxByteLength);
			while (pos < end && buffer.ReadByte(pos) != 0)
				pos++;
			if (start <= pos)
				return new HexBufferSpan(buffer, HexSpan.FromBounds(start, pos));
			return new HexBufferSpan(buffer, Span.Span.Start, 0);
		}

		/// <summary>
		/// Reads string data which should be a UTF-8 encoded string. The array doesn't include the terminating zero.
		/// Returns an empty array if <paramref name="offset"/> is invalid
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <returns></returns>
		public byte[] ReadBytes(uint offset) => ReadBytes(offset, int.MaxValue);

		/// <summary>
		/// Reads string data which should be a UTF-8 encoded string. The array doesn't include the terminating zero.
		/// Returns an empty array if <paramref name="offset"/> is invalid
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <param name="maxByteLength">Maximum number of bytes to read</param>
		/// <returns></returns>
		public byte[] ReadBytes(uint offset, int maxByteLength) => GetStringSpan(offset, maxByteLength).GetData();

		/// <summary>
		/// Reads a string. Returns an empty string if <paramref name="offset"/> is invalid
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <returns></returns>
		public string Read(uint offset) => Read(offset, int.MaxValue);

		/// <summary>
		/// Reads a string. Returns an empty string if <paramref name="offset"/> is invalid
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <param name="maxByteLength">Maximum number of bytes to read</param>
		/// <returns></returns>
		public string Read(uint offset, int maxByteLength) => Encoding.UTF8.GetString(ReadBytes(offset, maxByteLength));
	}

	/// <summary>
	/// .NET #US heap
	/// </summary>
	public abstract class USHeap : DotNetHeap {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Heap span</param>
		protected USHeap(HexBufferSpan span)
			: base(span, DotNetHeapKind.US) {
		}

		/// <summary>
		/// Returns the span of the string data or an empty span if <paramref name="offset"/> is 0 or invalid.
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <returns></returns>
		public HexBufferSpan GetStringSpan(uint offset) {
			// The CLR assumes offset 0 contains byte 0x00
			if (offset == 0 || !IsValidOffset(offset))
				return new HexBufferSpan(Span.Start, 0);
			var pos = Span.Start.Position + offset;
			int length = ReadCompressedUInt32(ref pos) ?? -1;
			if (length < 0)
				return new HexBufferSpan(Span.Start, 0);
			if (pos + length > Span.End.Position)
				return new HexBufferSpan(Span.Start, 0);
			return new HexBufferSpan(Span.Buffer, new HexSpan(pos, (ulong)length));
		}

		/// <summary>
		/// Reads data at <paramref name="offset"/>. Returns an empty array if <paramref name="offset"/> is invalid.
		/// The returned data doesn't include the compressed data length at <paramref name="offset"/>.
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <returns></returns>
		public byte[] ReadData(uint offset) => GetStringSpan(offset).GetData();

		/// <summary>
		/// Reads the string at <paramref name="offset"/>. Returns an empty string if
		/// <paramref name="offset"/> is 0 or invalid.
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <returns></returns>
		public string Read(uint offset) => Encoding.Unicode.GetString(ReadData(offset));
	}

	/// <summary>
	/// .NET #GUID heap
	/// </summary>
	public abstract class GUIDHeap : DotNetHeap {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Heap span</param>
		protected GUIDHeap(HexBufferSpan span)
			: base(span, DotNetHeapKind.GUID) {
		}

		/// <summary>
		/// Checks whether <paramref name="index"/> is valid. This method is identical to <see cref="IsValidOffset(uint)"/>
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public bool IsValidIndex(uint index) => IsValidOffset(index);

		/// <summary>
		/// Checks whether <paramref name="index"/> is valid
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public override bool IsValidOffset(uint index) => index == 0 || (ulong)index * 16 <= Span.Length;

		/// <summary>
		/// Reads a <see cref="Guid"/>. Returns null if <paramref name="index"/> is 0 or invalid
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public Guid? Read(uint index) {
			if (index == 0 || !IsValidIndex(index))
				return null;
			return new Guid(Span.Buffer.ReadBytes(Span.Start.Position + (index - 1) * 16, 16));
		}
	}

	/// <summary>
	/// .NET #Blob heap
	/// </summary>
	public abstract class BlobHeap : DotNetHeap {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Heap span</param>
		protected BlobHeap(HexBufferSpan span)
			: base(span, DotNetHeapKind.Blob) {
		}

		/// <summary>
		/// Gets the span of data in this heap. The span doesn't include the compressed data length
		/// at <paramref name="offset"/>. An empty span is returned if <paramref name="offset"/>
		/// is invalid or 0.
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <returns></returns>
		public HexBufferSpan GetDataSpan(uint offset) {
			// The CLR assumes offset 0 contains byte 0x00
			if (offset == 0 || !IsValidOffset(offset))
				return new HexBufferSpan(Span.Start, 0);
			var pos = Span.Start.Position + offset;
			int length = ReadCompressedUInt32(ref pos) ?? -1;
			if (length < 0)
				return new HexBufferSpan(Span.Start, 0);
			if (pos + length > Span.End.Position)
				return new HexBufferSpan(Span.Start, 0);
			return new HexBufferSpan(Span.Buffer, new HexSpan(pos, (ulong)length));
		}

		/// <summary>
		/// Reads data at <paramref name="offset"/>. Returns an empty array if <paramref name="offset"/> is invalid.
		/// The returned data doesn't include the compressed data length at <paramref name="offset"/>.
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <returns></returns>
		public byte[] Read(uint offset) => GetDataSpan(offset).GetData();
	}

	/// <summary>
	/// .NET #! heap
	/// </summary>
	public abstract class HotHeap : DotNetHeap {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Heap span</param>
		protected HotHeap(HexBufferSpan span)
			: base(span, DotNetHeapKind.Hot) {
		}
	}

	/// <summary>
	/// .NET #Pdb heap
	/// </summary>
	public abstract class PdbHeap : DotNetHeap {
		/// <summary>
		/// Gets the header
		/// </summary>
		public abstract PdbStreamHeaderData Header { get; }

		/// <summary>
		/// Gets the PDB id, this value is cached
		/// </summary>
		public abstract ReadOnlyCollection<byte> PdbId { get; }

		/// <summary>
		/// Gets the entry point, this value is cached
		/// </summary>
		public abstract MDToken EntryPoint { get; }

		/// <summary>
		/// Gets a bit mask of all referenced type system tables, this value is cached
		/// </summary>
		public abstract ulong ReferencedTypeSystemTables { get; }

		/// <summary>
		/// Gets the rows, this value is cached
		/// </summary>
		public abstract ReadOnlyCollection<uint> TypeSystemTableRows { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Heap span</param>
		protected PdbHeap(HexBufferSpan span)
			: base(span, DotNetHeapKind.Pdb) {
		}
	}

	/// <summary>
	/// Unknown .NET heap
	/// </summary>
	public abstract class UnknownHeap : DotNetHeap {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Heap span</param>
		protected UnknownHeap(HexBufferSpan span)
			: base(span, DotNetHeapKind.Unknown) {
		}
	}
}
