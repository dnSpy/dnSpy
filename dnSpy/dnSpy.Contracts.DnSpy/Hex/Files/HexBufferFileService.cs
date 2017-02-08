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
using System.Collections.Generic;
using System.Linq;

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// Creates and removes <see cref="HexBufferFile"/>s from a <see cref="HexBuffer"/>
	/// </summary>
	public abstract class HexBufferFileService {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexBufferFileService() { }

		/// <summary>
		/// Gets the buffer
		/// </summary>
		public abstract HexBuffer Buffer { get; }

		/// <summary>
		/// Gets all files
		/// </summary>
		public abstract IEnumerable<HexBufferFile> Files { get; }

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
		/// Removes all files
		/// </summary>
		public void RemoveAllFiles() => RemoveFiles(HexSpan.FullSpan);

		/// <summary>
		/// Removes all files overlapping with <paramref name="span"/>
		/// </summary>
		/// <param name="span">Span</param>
		public abstract void RemoveFiles(HexSpan span);

		/// <summary>
		/// Removes a file
		/// </summary>
		/// <param name="file">File to remove</param>
		public abstract void RemoveFile(HexBufferFile file);

		/// <summary>
		/// Removes files
		/// </summary>
		/// <param name="files">Files to remove</param>
		public abstract void RemoveFiles(IEnumerable<HexBufferFile> files);

		/// <summary>
		/// Raised after files are added
		/// </summary>
		public abstract event EventHandler<BufferFilesAddedEventArgs> BufferFilesAdded;

		/// <summary>
		/// Raised after files are removed
		/// </summary>
		public abstract event EventHandler<BufferFilesRemovedEventArgs> BufferFilesRemoved;

		/// <summary>
		/// Finds a file
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="checkNestedFiles">true to check nested files</param>
		/// <returns></returns>
		public abstract HexBufferFile GetFile(HexPosition position, bool checkNestedFiles);

		/// <summary>
		/// Gets a <see cref="HexBufferFile"/> and structure at <paramref name="position"/> or null if
		/// there's no structure
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public FileAndStructure? GetFileAndStructure(HexPosition position) {
			// Don't get the inner-most file since if it doesn't contain any structures,
			// nothing will be returned.
			var file = GetFile(position, checkNestedFiles: false);
			return file?.GetFileAndStructure(position, checkNestedFiles: true);
		}
	}

	/// <summary>
	/// File and structure
	/// </summary>
	public struct FileAndStructure {
		/// <summary>
		/// Gets the file
		/// </summary>
		public HexBufferFile File { get; }

		/// <summary>
		/// Gets the structure
		/// </summary>
		public ComplexData Structure { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="file">File</param>
		/// <param name="structure">Structure</param>
		public FileAndStructure(HexBufferFile file, ComplexData structure) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			if (structure == null)
				throw new ArgumentNullException(nameof(structure));
			File = file;
			Structure = structure;
		}
	}
}
