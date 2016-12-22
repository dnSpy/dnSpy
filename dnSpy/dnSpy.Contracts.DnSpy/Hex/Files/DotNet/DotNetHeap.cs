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
	}

	/// <summary>
	/// .NET tables (#~ or #-) heap
	/// </summary>
	public abstract class TablesHeap : DotNetHeap {
		/// <summary>
		/// Gets the metadata type
		/// </summary>
		public MetaDataType MetaDataType { get; }

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
		/// <param name="metaDataType">Metadata type</param>
		protected TablesHeap(HexBufferSpan span, MetaDataType metaDataType)
			: base(span, DotNetHeapKind.Tables) {
			MetaDataType = metaDataType;
		}
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
	public class UnknownHeap : DotNetHeap {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Heap span</param>
		public UnknownHeap(HexBufferSpan span)
			: base(span, DotNetHeapKind.Unknown) {
		}
	}
}
