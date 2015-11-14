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
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet;
using dnlib.PE;

namespace dnSpy.Contracts.Files {
	/// <summary>
	/// A file, see also <see cref="IDnSpyDotNetFile"/> and <see cref="IDnSpyPEFile"/>
	/// </summary>
	public interface IDnSpyFile : IAnnotations {
		/// <summary>
		/// Gets a key for this file. Eg. a <see cref="FilenameKey"/> instance if it's a
		/// file loaded from disk. It's used to detect duplicate files when adding a new file.
		/// </summary>
		IDnSpyFilenameKey Key { get; }

		/// <summary>
		/// Gets the assembly and module this file is in or null if it's not a .NET file
		/// </summary>
		SerializedDnSpyModule? SerializedDnSpyModule { get; }

		/// <summary>
		/// Gets the assembly or null if it's not a .NET file or if it's a netmodule
		/// </summary>
		AssemblyDef AssemblyDef { get; }

		/// <summary>
		/// Gets the module or null if it's not a .NET file
		/// </summary>
		ModuleDef ModuleDef { get; }

		/// <summary>
		/// Gets the PE image or null if it's not available
		/// </summary>
		IPEImage PEImage { get; }

		/// <summary>
		/// Gets/sets the filename
		/// </summary>
		string Filename { get; set; }

		/// <summary>
		/// true if it was loaded from a file
		/// </summary>
		bool LoadedFromFile { get; }

		/// <summary>
		/// true if it was not loaded by the user
		/// </summary>
		bool IsAutoLoaded { get; set; }

		/// <summary>
		/// Gets any children. Eg. if it's a .NET assembly, the children would be modules of the
		/// assembly.
		/// </summary>
		List<IDnSpyFile> Children { get; }
	}

	/// <summary>
	/// Extension methods
	/// </summary>
	public static class DnSpyFileExtensionMethods {
		/// <summary>
		/// Gets the short name of <paramref name="file"/>, which is usually the filename without
		/// the extension.
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static string GetShortName(this IDnSpyFile file) {
			var shortName = GetShortName(file.Filename);
			if (string.IsNullOrEmpty(shortName))
				shortName = GetDefaultShortName(file) ?? string.Empty;
			return shortName;
		}

		static string GetDefaultShortName(IDnSpyFile file) {
			var m = file.ModuleDef;
			return m == null ? null : m.Name;
		}

		static string GetShortName(string filename) {
			if (string.IsNullOrEmpty(filename))
				return filename;
			try {
				var s = Path.GetFileNameWithoutExtension(filename);
				if (!string.IsNullOrWhiteSpace(s))
					return s;
				s = Path.GetFileName(filename);
				if (!string.IsNullOrWhiteSpace(s))
					return s;
			}
			catch (ArgumentException) {
			}
			return filename;
		}
	}
}
