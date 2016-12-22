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

using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// .NET metadata-only headers, present if the file is a .NET PE file or a .NET metadata only file (eg. portable pdb file)
	/// </summary>
	public abstract class DotNetMetadataHeaders : IBufferFileHeaders {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="metadataSpan">Metadata span</param>
		protected DotNetMetadataHeaders(HexSpan metadataSpan) {
			MetadataSpan = metadataSpan;
		}

		/// <summary>
		/// Gets the metadata span
		/// </summary>
		public HexSpan MetadataSpan { get; }

		/// <summary>
		/// Gets the metadata header
		/// </summary>
		public abstract DotNetMetadataHeaderData MetadataHeader { get; }

		/// <summary>
		/// Gets the tables stream (#~ or #-) or null if none
		/// </summary>
		public abstract TablesHeap TablesStream { get; }

		/// <summary>
		/// Gets the #Strings stream or null if none
		/// </summary>
		public abstract StringsHeap StringsStream { get; }

		/// <summary>
		/// Gets the #US stream or null if none
		/// </summary>
		public abstract USHeap USStream { get; }

		/// <summary>
		/// Gets the #GUID stream or null if none
		/// </summary>
		public abstract GUIDHeap GUIDStream { get; }

		/// <summary>
		/// Gets the #Blob stream or null if none
		/// </summary>
		public abstract BlobHeap BlobStream { get; }

		/// <summary>
		/// Gets the #Pdb stream or null if none
		/// </summary>
		public abstract PdbHeap PdbStream { get; }

		/// <summary>
		/// Gets all heaps
		/// </summary>
		public abstract ReadOnlyCollection<DotNetHeap> Streams { get; }
	}
}
