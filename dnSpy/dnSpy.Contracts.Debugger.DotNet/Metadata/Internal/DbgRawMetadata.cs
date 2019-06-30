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

namespace dnSpy.Contracts.Debugger.DotNet.Metadata.Internal {
	/// <summary>
	/// Raw .NET metadata stored in some memory location
	/// </summary>
	public abstract class DbgRawMetadata {
		/// <summary>
		/// true if it's file layout, false if it's memory layout
		/// </summary>
		public abstract bool IsFileLayout { get; }

		/// <summary>
		/// true if it's memory layout, false if it's file layout
		/// </summary>
		public bool IsMemoryLayout => !IsFileLayout;

		/// <summary>
		/// Gets the address of the data (first byte of the PE file)
		/// </summary>
		public abstract IntPtr Address { get; }

		/// <summary>
		/// Gets the size of the data (size of the PE file in memory)
		/// </summary>
		public abstract int Size { get; }

		/// <summary>
		/// Gets the address of the .NET metadata (BSJB header)
		/// </summary>
		public abstract IntPtr MetadataAddress { get; }

		/// <summary>
		/// Gets the size of the metadata
		/// </summary>
		public abstract int MetadataSize { get; }

		/// <summary>
		/// Increments the reference count and returns the same instance
		/// </summary>
		/// <returns></returns>
		public abstract DbgRawMetadata AddRef();

		/// <summary>
		/// Decrements the reference count
		/// </summary>
		public abstract void Release();

		/// <summary>
		/// Re-reads the memory if possible
		/// </summary>
		public abstract void UpdateMemory();
	}
}
