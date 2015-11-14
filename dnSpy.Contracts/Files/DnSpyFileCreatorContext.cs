/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using System.IO;

namespace dnSpy.Contracts.Files {
	/// <summary>
	/// <see cref="IDnSpyFileCreator"/> context
	/// </summary>
	public sealed class DnSpyFileCreatorContext {
		/// <summary>
		/// File manager
		/// </summary>
		public IFileManager FileManager { get; private set; }

		/// <summary>
		/// Filename, might be invalid (eg. empty or null), so call <see cref="File.Exists(string)"/>
		/// first to prevent exceptions when trying to open files.
		/// </summary>
		public string Filename { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="fileManager">File manager</param>
		/// <param name="filename">Filename</param>
		public DnSpyFileCreatorContext(IFileManager fileManager, string filename) {
			this.FileManager = fileManager;
			this.Filename = filename;
		}
	}
}
