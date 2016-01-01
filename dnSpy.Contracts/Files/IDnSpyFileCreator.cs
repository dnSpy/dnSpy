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

namespace dnSpy.Contracts.Files {
	/// <summary>
	/// Creates <see cref="IDnSpyFile"/>s
	/// </summary>
	public interface IDnSpyFileCreator {
		/// <summary>
		/// Order of this instance
		/// </summary>
		double Order { get; }

		/// <summary>
		/// Creates a new <see cref="IDnSpyFile"/> instance or returns null. This method can be
		/// called in <c>any</c> thread so the code must be thread safe.
		/// </summary>
		/// <param name="fileManager">File manager</param>
		/// <param name="fileInfo">File to create</param>
		/// <returns></returns>
		IDnSpyFile Create(IFileManager fileManager, DnSpyFileInfo fileInfo);

		/// <summary>
		/// Creates a <see cref="IDnSpyFilenameKey"/> instance
		/// </summary>
		/// <param name="fileManager">File manager</param>
		/// <param name="fileInfo">File to create</param>
		/// <returns></returns>
		IDnSpyFilenameKey CreateKey(IFileManager fileManager, DnSpyFileInfo fileInfo);
	}
}
