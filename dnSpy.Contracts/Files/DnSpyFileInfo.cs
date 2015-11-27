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

using System;

namespace dnSpy.Contracts.Files {
	/// <summary>
	/// File info
	/// </summary>
	public struct DnSpyFileInfo {
		/// <summary>
		/// Name, eg. filename if <see cref="Type"/> is <see cref="FilesConstants.FILETYPE_FILE"/>
		/// </summary>
		public string Name {
			get { return name ?? string.Empty; }
		}
		readonly string name;

		/// <summary>
		/// File type, eg. <see cref="FilesConstants.FILETYPE_FILE"/>
		/// </summary>
		public Guid Type {
			get { return type; }
		}
		readonly Guid type;

		/// <summary>
		/// Creates a <see cref="DnSpyFileInfo"/> used by files on disk
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <returns></returns>
		public static DnSpyFileInfo CreateFile(string filename) {
			return new DnSpyFileInfo(filename, FilesConstants.FILETYPE_FILE);
		}

		/// <summary>
		/// Creates a <see cref="DnSpyFileInfo"/> used by files in the GAC
		/// </summary>
		/// <param name="asmFullName">Full name of assembly</param>
		/// <returns></returns>
		public static DnSpyFileInfo CreateGacFile(string asmFullName) {
			return new DnSpyFileInfo(asmFullName, FilesConstants.FILETYPE_GAC);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name, see <see cref="Name"/></param>
		/// <param name="type">Type, see <see cref="Type"/></param>
		public DnSpyFileInfo(string name, Guid type) {
			this.name = name ?? string.Empty;
			this.type = type;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return string.Format("{0} {1}", Name, Type);
		}
	}
}
