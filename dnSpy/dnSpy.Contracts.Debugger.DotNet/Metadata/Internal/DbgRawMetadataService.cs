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

namespace dnSpy.Contracts.Debugger.DotNet.Metadata.Internal {
	/// <summary>
	/// Creates <see cref="DbgRawMetadata"/> instances
	/// </summary>
	public abstract class DbgRawMetadataService {
		/// <summary>
		/// Creates a <see cref="DbgRawMetadata"/>
		/// </summary>
		/// <param name="runtime">Runtime</param>
		/// <param name="isFileLayout">true if it's file layout, false if it's memory layout</param>
		/// <param name="moduleAddress">Address of .NET module in the process' address space</param>
		/// <param name="moduleSize">Size of module</param>
		/// <returns></returns>
		public abstract DbgRawMetadata Create(DbgRuntime runtime, bool isFileLayout, ulong moduleAddress, int moduleSize);

		/// <summary>
		/// Creates a <see cref="DbgRawMetadata"/>
		/// </summary>
		/// <param name="runtime">Runtime</param>
		/// <param name="isFileLayout">true if it's file layout, false if it's memory layout</param>
		/// <param name="moduleBytes">Raw module bytes</param>
		/// <returns></returns>
		public abstract DbgRawMetadata Create(DbgRuntime runtime, bool isFileLayout, byte[] moduleBytes);
	}
}
