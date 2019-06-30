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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Base class of all classes that contain metadata
	/// </summary>
	public abstract class DmdLazyMetadataBytes {
	}

	/// <summary>
	/// Metadata in a <see cref="byte"/> array
	/// </summary>
	public sealed class DmdLazyMetadataBytesArray : DmdLazyMetadataBytes {
		/// <summary>
		/// Gets the raw PE file bytes
		/// </summary>
		public byte[] Bytes { get; }

		/// <summary>
		/// true if file layout, false if memory layout
		/// </summary>
		public bool IsFileLayout { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bytes">Raw PE file bytes</param>
		/// <param name="isFileLayout">true if file layout, false if memory layout</param>
		public DmdLazyMetadataBytesArray(byte[] bytes, bool isFileLayout) {
			Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
			IsFileLayout = isFileLayout;
		}
	}

	/// <summary>
	/// Metadata in a file
	/// </summary>
	public sealed class DmdLazyMetadataBytesFile : DmdLazyMetadataBytes {
		/// <summary>
		/// Gets the filename
		/// </summary>
		public string Filename { get; }

		/// <summary>
		/// true if file layout, false if memory layout
		/// </summary>
		public bool IsFileLayout { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="isFileLayout">true if file layout, false if memory layout</param>
		public DmdLazyMetadataBytesFile(string filename, bool isFileLayout = true) {
			Filename = filename ?? throw new ArgumentNullException(nameof(filename));
			IsFileLayout = isFileLayout;
		}
	}

	/// <summary>
	/// Metadata in memory
	/// </summary>
	public sealed class DmdLazyMetadataBytesPtr : DmdLazyMetadataBytes {
		/// <summary>
		/// Gets the address of the PE file
		/// </summary>
		public IntPtr Address { get; }

		/// <summary>
		/// Gets the size of the PE file
		/// </summary>
		public uint Size { get; }

		/// <summary>
		/// true if file layout, false if memory layout
		/// </summary>
		public bool IsFileLayout { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="address">Address of the PE file</param>
		/// <param name="size">Size of the PE file</param>
		/// <param name="isFileLayout">true if file layout, false if memory layout</param>
		public DmdLazyMetadataBytesPtr(IntPtr address, uint size, bool isFileLayout) {
			if (address == IntPtr.Zero)
				throw new ArgumentNullException(nameof(address));
			if (size == 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			Address = address;
			Size = size;
			IsFileLayout = isFileLayout;
		}
	}

	/// <summary>
	/// COM <c>IMetaDataImport</c> metadata
	/// </summary>
	public sealed class DmdLazyMetadataBytesCom : DmdLazyMetadataBytes {
		/// <summary>
		/// Gets the COM <c>IMetaDataImport</c> instance
		/// </summary>
		public object ComMetadata => MetaDataImport;

		internal Impl.COMD.IMetaDataImport2 MetaDataImport { get; }

		/// <summary>
		/// Gets the dispatcher to use when accessing <see cref="ComMetadata"/>
		/// </summary>
		public DmdDispatcher Dispatcher { get; }

		/// <summary>
		/// Gets the helper class
		/// </summary>
		public DmdDynamicModuleHelper DynamicModuleHelper { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="comMetadata">COM <c>IMetaDataImport</c> instance</param>
		/// <param name="dynamicModuleHelper">Helper class</param>
		/// <param name="dispatcher">Dispatcher to use when accessing <paramref name="comMetadata"/></param>
		public DmdLazyMetadataBytesCom(object comMetadata, DmdDynamicModuleHelper dynamicModuleHelper, DmdDispatcher dispatcher) {
			MetaDataImport = comMetadata as Impl.COMD.IMetaDataImport2 ?? throw new ArgumentException("Only " + nameof(Impl.COMD.IMetaDataImport2) + " is supported");
			DynamicModuleHelper = dynamicModuleHelper ?? throw new ArgumentNullException(nameof(dynamicModuleHelper));
			Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
		}

		internal DmdLazyMetadataBytesCom(Impl.COMD.IMetaDataImport2 metaDataImport, DmdDynamicModuleHelper dynamicModuleHelper, DmdDispatcher dispatcher) {
			MetaDataImport = metaDataImport ?? throw new ArgumentNullException(nameof(metaDataImport));
			DynamicModuleHelper = dynamicModuleHelper ?? throw new ArgumentNullException(nameof(dynamicModuleHelper));
			Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
		}
	}
}
