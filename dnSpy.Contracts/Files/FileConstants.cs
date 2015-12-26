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
	/// Constants
	/// </summary>
	public static class FileConstants {
		/// <summary>
		/// Order of default <see cref="IDnSpyFileCreator"/> instance
		/// </summary>
		public const double ORDER_DEFAULT_FILE_CREATOR = double.MaxValue;

		/// <summary>
		/// A normal <see cref="IDnSpyFile"/> created from a file. <see cref="DnSpyFileInfo.Name"/>
		/// is the filename.
		/// </summary>
		public static readonly Guid FILETYPE_FILE = new Guid("57E89016-3E28-43A2-88C0-42D067520C14");

		/// <summary>
		/// A <see cref="IDnSpyFile"/> created from a file in the GAC. <see cref="DnSpyFileInfo.Name"/>
		/// is the assembly name.
		/// </summary>
		public static readonly Guid FILETYPE_GAC = new Guid("1A7BE658-FD95-46A9-BA03-A05D87161342");

		/// <summary>
		/// A <see cref="IDnSpyFile"/> created from a file in the GAC or the reference assemblies
		/// folder. <see cref="DnSpyFileInfo.Name"/> is the assembly name followed by 
		/// <see cref="REFERENCE_ASSEMBLY_SEPARATOR"/> followed by the path to the reference file in
		/// case it's not found in the GAC.
		/// </summary>
		public static readonly Guid FILETYPE_REFASM = new Guid("75AB0E5C-D1D7-4811-93E1-0AF26CE3856C");

		/// <summary>
		/// String separating assembly full name and reference assembly path
		/// </summary>
		public static readonly string REFERENCE_ASSEMBLY_SEPARATOR = "|";
	}
}
