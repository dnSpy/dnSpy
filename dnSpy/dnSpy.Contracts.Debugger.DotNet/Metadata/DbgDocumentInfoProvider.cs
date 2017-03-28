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

using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Contracts.Debugger.DotNet.Metadata {
	/// <summary>
	/// Creates <see cref="DbgDocumentInfo"/>s
	/// </summary>
	public abstract class DbgDocumentInfoProvider {
		/// <summary>
		/// Creates a <see cref="DbgDocumentInfo"/> or returns null
		/// </summary>
		/// <param name="document">Document</param>
		/// <returns></returns>
		public abstract DbgDocumentInfo? TryGetFileInfo(IDsDocument document);
	}

	/// <summary>
	/// Document info
	/// </summary>
	public struct DbgDocumentInfo {
		/// <summary>
		/// Gets the module id
		/// </summary>
		public ModuleId Id { get; }

		/// <summary>
		/// false if the file was loaded in a debugged process and the process is not being debugged, and in all other cases, true.
		/// </summary>
		public bool IsActive { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="id">Module id</param>
		/// <param name="isActive">false if the file was loaded in a debugged process and the process is not being debugged, and in all other cases, true.</param>
		public DbgDocumentInfo(ModuleId id, bool isActive) {
			Id = id;
			IsActive = isActive;
		}
	}
}
