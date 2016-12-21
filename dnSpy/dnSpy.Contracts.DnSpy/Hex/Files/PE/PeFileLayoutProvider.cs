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

namespace dnSpy.Contracts.Hex.Files.PE {
	/// <summary>
	/// Detects whether a PE file was loaded by the OS PE loader
	/// </summary>
	public abstract class PeFileLayoutProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected PeFileLayoutProvider() { }

		/// <summary>
		/// Gets the PE file layout
		/// </summary>
		/// <param name="file">File</param>
		/// <returns></returns>
		public abstract PeFileLayout GetLayout(HexBufferFile file);
	}

	/// <summary>
	/// PE file layout
	/// </summary>
	public enum PeFileLayout {
		/// <summary>
		/// Unknown layout
		/// </summary>
		Unknown,

		/// <summary>
		/// File layout
		/// </summary>
		File,

		/// <summary>
		/// Memory layout, the OS loader has loaded the file into memory
		/// </summary>
		Memory,
	}
}
