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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// A file in a <see cref="HexBuffer"/>
	/// </summary>
	public abstract class HexBufferFile : VSUTIL.IPropertyOwner {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="span">Span of file</param>
		/// <param name="name">Name</param>
		/// <param name="filename">Filename if possible, otherwise any name</param>
		/// <param name="tags">Tags, see eg. <see cref="PredefinedBufferFileTags"/></param>
		protected HexBufferFile(HexBuffer buffer, HexSpan span, string name, string filename, string[] tags) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			if (filename == null)
				throw new ArgumentNullException(nameof(filename));
			if (tags == null)
				throw new ArgumentNullException(nameof(tags));
			Buffer = buffer;
			Span = span;
			Name = name;
			Filename = filename;
			Tags = new ReadOnlyCollection<string>(tags.ToArray());
			Properties = new VSUTIL.PropertyCollection();
		}

		/// <summary>
		/// Gets the properties
		/// </summary>
		public VSUTIL.PropertyCollection Properties { get; }

		/// <summary>
		/// Gets the buffer
		/// </summary>
		public HexBuffer Buffer { get; }

		/// <summary>
		/// Gets the file span
		/// </summary>
		public HexSpan Span { get; }

		/// <summary>
		/// Gets the name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the filename if possible, otherwise it could be any name
		/// </summary>
		public string Filename { get; }

		/// <summary>
		/// Gets all the tags, see eg. <see cref="PredefinedBufferFileTags"/>
		/// </summary>
		public ReadOnlyCollection<string> Tags { get; }

		/// <summary>
		/// Parent file or null if it's not a nested file
		/// </summary>
		public abstract HexBufferFile ParentFile { get; }

		/// <summary>
		/// true if it's a nested file (<see cref="ParentFile"/> is not null)
		/// </summary>
		public bool IsNestedFile => ParentFile != null;

		/// <summary>
		/// Gets all nested files
		/// </summary>
		public abstract IEnumerable<HexBufferFile> Files { get; }

		/// <summary>
		/// Creates a file. Overlapping files isn't supported.
		/// </summary>
		/// <param name="span">Span of file</param>
		/// <returns></returns>
		public HexBufferFile CreateFile(HexSpan span) =>
			CreateFiles(new BufferFileOptions(span, string.Empty, string.Empty, Array.Empty<string>())).Single();

		/// <summary>
		/// Creates a file. Overlapping files isn't supported.
		/// </summary>
		/// <param name="span">Span of file</param>
		/// <param name="name">Name</param>
		/// <param name="filename">Filename if possible, otherwise any name</param>
		/// <param name="tags">Tags, see eg. <see cref="PredefinedBufferFileTags"/></param>
		/// <returns></returns>
		public HexBufferFile CreateFile(HexSpan span, string name, string filename, string[] tags) =>
			CreateFiles(new BufferFileOptions(span, name, filename, tags)).Single();

		/// <summary>
		/// Creates files. Overlapping files isn't supported.
		/// </summary>
		/// <param name="options">File options</param>
		/// <returns></returns>
		public abstract HexBufferFile[] CreateFiles(params BufferFileOptions[] options);

		/// <summary>
		/// Raised after files are added
		/// </summary>
		public abstract event EventHandler<BufferFilesAddedEventArgs> BufferFilesAdded;

		/// <summary>
		/// Finds a file
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="checkNestedFiles">true to check nested files</param>
		/// <returns></returns>
		public abstract HexBufferFile GetFile(HexPosition position, bool checkNestedFiles);

		/// <summary>
		/// true if it has been removed
		/// </summary>
		public abstract bool IsRemoved { get; }

		/// <summary>
		/// Raised after it is removed
		/// </summary>
		public abstract event EventHandler Removed;

		/// <summary>
		/// Gets a structure
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="checkNestedFiles">true to check nested files, false to only check this file</param>
		/// <returns></returns>
		public abstract ComplexData GetStructure(HexPosition position, bool checkNestedFiles = true);

		/// <summary>
		/// Gets a structure. Nested files aren't checked.
		/// </summary>
		/// <param name="id">Id, see eg. <see cref="PE.PredefinedPeDataIds"/></param>
		/// <returns></returns>
		public abstract ComplexData GetStructure(string id);

		/// <summary>
		/// true if <see cref="StructuresInitialized"/> has been raised
		/// </summary>
		public abstract bool IsStructuresInitialized { get; }

		/// <summary>
		/// Raised after the default structures have been added
		/// </summary>
		public abstract event EventHandler StructuresInitialized;

		/// <summary>
		/// Gets headers. Nested files aren't checked.
		/// </summary>
		/// <typeparam name="THeaders">Type</typeparam>
		/// <returns></returns>
		public abstract THeaders GetHeaders<THeaders>() where THeaders : class, IBufferFileHeaders;
	}
}
