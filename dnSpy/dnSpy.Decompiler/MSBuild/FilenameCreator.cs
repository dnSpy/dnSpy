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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using dnlib.DotNet;

namespace dnSpy.Decompiler.MSBuild {
	sealed class FilenameCreator {
		// Max length of a filename, excluding extension
		const int MaxNameLength = 60;
		// Max length of a directory name part
		const int MaxDirNameLength = 40;

		public string DefaultNamespace => defaultNamespace;
		readonly string defaultNamespace;

		readonly HashSet<string> usedNames;
		readonly string baseDir;

		public FilenameCreator(string baseDir) {
			Debug.Assert(Path.IsPathRooted(baseDir));
			this.baseDir = baseDir;
			defaultNamespace = string.Empty;
			usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}

		public FilenameCreator(string baseDir, string defaultNamespace) {
			Debug.Assert(Path.IsPathRooted(baseDir));
			this.baseDir = baseDir;
			this.defaultNamespace = defaultNamespace;
			usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}

		public string Create(string fileExt, string fullName) {
			Debug2.Assert(!(fileExt is null) && fileExt.Length > 1 && fileExt[0] == '.');
			string name = StripDefaultNamespace(fullName);
			if (string.IsNullOrEmpty(name))
				name = fullName;
			return Create(name.Split('.'), fileExt);
		}

		public string CreateFromNamespaceName(string fileExt, string ns, string name) {
			Debug2.Assert(!(fileExt is null) && fileExt.Length > 1 && fileExt[0] == '.');
			var list = GetNamespaceParts(ns);
			list.Add(name);
			return Create(list.ToArray(), fileExt);
		}

		List<string> GetNamespaceParts(string ns) {
			ns = StripDefaultNamespace(ns);
			var list = new List<string>();
			if (!string.IsNullOrEmpty(ns))
				list.AddRange(ns.Split('.'));
			return list;
		}

		string StripDefaultNamespace(string name) {
			if (defaultNamespace.Equals(name))
				return string.Empty;
			if (name.StartsWith(defaultNamespace + "."))
				return name.Substring(defaultNamespace.Length + 1);
			return name;
		}

		public string CreateFromRelativePath(string relPath, string fileExt) {
			relPath = relPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			return Create(relPath.Split(Path.DirectorySeparatorChar), fileExt);
		}

		string Create(string[] parts, string fileExt) {
			fileExt = FilenameUtils.CleanName(fileExt);
			string tempName = string.Empty;
			for (int i = 0; i < parts.Length; i++) {
				var part = parts[i];
				int maxLen = i == parts.Length - 1 ? MaxNameLength : MaxDirNameLength;
				tempName = Path.Combine(tempName, FixLongName(FilenameUtils.CleanName(part), maxLen));
			}
			tempName = Path.Combine(baseDir, tempName);
			var newName = tempName + fileExt;
			if (usedNames.Contains(newName)) {
				for (int i = 2; ; i++) {
					newName = tempName + "." + i.ToString() + fileExt;
					if (!usedNames.Contains(newName))
						break;
				}
			}
			usedNames.Add(newName);
			return newName;
		}

		public string Create(ModuleDef module) {
			string name;
			var asm = module.Assembly;
			if (!(asm is null) && module.IsManifestModule)
				name = module.Assembly.Name;
			else
				name = FileUtils.GetFilename(module.Name);
			return Create(name);
		}

		string Create(string name) {
			name = Path.Combine(baseDir, FixLongName(FilenameUtils.CleanName(name), MaxNameLength));
			if (usedNames.Contains(name)) {
				var tempName = name;
				for (int i = 2; ; i++) {
					name = tempName + "." + i.ToString();
					if (!usedNames.Contains(name))
						break;
				}
			}
			usedNames.Add(name);
			return name;
		}

		static string FixLongName(string name, int maxLen) {
			if (name.Length <= maxLen)
				return name;
			return name.Substring(0, maxLen);
		}

		public string CreateFromNamespaceFilename(string @namespace, string filename) {
			var fileExt = FileUtils.GetExtension(filename);
			var relPath = filename.Substring(0, filename.Length - fileExt.Length);
			ExtractNamespace(relPath, out string ns, out string filenameNoExt);
			if (!string.IsNullOrEmpty(ns)) {
				if (string.IsNullOrEmpty(@namespace))
					@namespace = ns;
				else
					@namespace += "." + ns;
			}
			var parts = GetNamespaceParts(@namespace);
			parts.Add(FileUtils.GetFileNameWithoutExtension(filenameNoExt));
			return Create(parts.ToArray(), fileExt);
		}

		static void ExtractNamespace(string relPath, out string ns, out string name) {
			int i = relPath.LastIndexOf('.');
			if (i < 0) {
				ns = string.Empty;
				name = relPath;
			}
			else {
				ns = relPath.Substring(0, i);
				name = relPath.Substring(i + 1);
			}
		}

		public string CreateName(string nameOnly) {
			string fileExt = FileUtils.GetExtension(nameOnly);
			var parts = new string[] { FileUtils.GetFileNameWithoutExtension(nameOnly) };
			return Create(parts, fileExt);
		}
	}
}
