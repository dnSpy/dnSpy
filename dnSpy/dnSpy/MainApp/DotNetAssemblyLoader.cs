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

#if NET
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace dnSpy.MainApp {
	sealed class DotNetAssemblyLoader {
		string[] searchPaths;
		readonly HashSet<string> searchPathsHash;
		static readonly string[] assemblyExtensions = new string[] { ".dll" };

		public DotNetAssemblyLoader(AssemblyLoadContext loadContext) {
			loadContext.Resolving += AssemblyLoadContext_Resolving;
			searchPaths = Array.Empty<string>();
			searchPathsHash = new HashSet<string>(StringComparer.Ordinal);
		}

		public void AddSearchPath(string path) {
			if (!Directory.Exists(path))
				return;
			if (!searchPathsHash.Add(path))
				return;
			var searchPaths = new string[this.searchPaths.Length + 1];
			Array.Copy(this.searchPaths, 0, searchPaths, 0, this.searchPaths.Length);
			searchPaths[searchPaths.Length - 1] = path;
			this.searchPaths = searchPaths;
		}

		Assembly? AssemblyLoadContext_Resolving(AssemblyLoadContext context, AssemblyName name) {
			foreach (var path in searchPaths) {
				foreach (var asmExt in assemblyExtensions) {
					var filename = Path.Combine(path, name.Name + asmExt);
					if (File.Exists(filename))
						return context.LoadFromAssemblyPath(filename);
				}
			}
			return null;
		}
	}
}
#endif
