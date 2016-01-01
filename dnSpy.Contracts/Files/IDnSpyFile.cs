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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.PE;

namespace dnSpy.Contracts.Files {
	/// <summary>
	/// A file, see also <see cref="IDnSpyDotNetFile"/> and <see cref="IDnSpyPEFile"/>
	/// </summary>
	public interface IDnSpyFile : IAnnotations {
		/// <summary>
		/// Used to serialize this instance. Null if it can't be serialized.
		/// </summary>
		DnSpyFileInfo? SerializedFile { get; }

		/// <summary>
		/// Gets a key for this file. Eg. a <see cref="FilenameKey"/> instance if it's a
		/// file loaded from disk. It's used to detect duplicate files when adding a new file.
		/// </summary>
		IDnSpyFilenameKey Key { get; }

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
		/// true if it was not loaded by the user
		/// </summary>
		bool IsAutoLoaded { get; set; }

		/// <summary>
		/// Gets any children. Eg. if it's a .NET assembly, the children would be modules of the
		/// assembly.
		/// </summary>
		List<IDnSpyFile> Children { get; }

		/// <summary>
		/// true if <see cref="Children"/> has been initialized
		/// </summary>
		bool ChildrenLoaded { get; }
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

		/// <summary>
		/// Gets self and all descendants that have been loaded
		/// </summary>
		/// <param name="file">This</param>
		/// <returns></returns>
		public static IEnumerable<IDnSpyFile> NonLoadedDescendantsAndSelf(this IDnSpyFile file) {
			if (file == null)
				throw new ArgumentNullException();
			yield return file;
			if (file.ChildrenLoaded) {
				foreach (var child in file.Children) {
					foreach (var f in child.NonLoadedDescendantsAndSelf())
						yield return f;
				}
			}
		}

		/// <summary>
		/// Gets all modules in this instance and any children
		/// </summary>
		/// <typeparam name="T"><see cref="ModuleDefMD"/> or <see cref="ModuleDefMD"/></typeparam>
		/// <param name="file">File</param>
		/// <returns></returns>
		public static IEnumerable<T> GetModules<T>(this IDnSpyFile file) where T : ModuleDef {
			return GetModules(new HashSet<T>(), new[] { file });
		}

		/// <summary>
		/// Gets all modules in this instance and any children
		/// </summary>
		/// <typeparam name="T"><see cref="ModuleDefMD"/> or <see cref="ModuleDefMD"/></typeparam>
		/// <param name="files">Files</param>
		/// <returns></returns>
		public static IEnumerable<T> GetModules<T>(this IEnumerable<IDnSpyFile> files) where T : ModuleDef {
			return GetModules(new HashSet<T>(), files);
		}

		static IEnumerable<T> GetModules<T>(HashSet<T> hash, IEnumerable<IDnSpyFile> files) where T : ModuleDef {
			foreach (var f in files.SelectMany(f => f.NonLoadedDescendantsAndSelf())) {
				var mod = f.ModuleDef as T;
				if (mod != null && !hash.Contains(mod)) {
					hash.Add(mod);
					yield return mod;
				}
				var asm = mod.Assembly;
				foreach (var m in asm.Modules) {
					mod = m as T;
					if (mod != null && !hash.Contains(mod)) {
						hash.Add(mod);
						yield return mod;
					}
				}
			}
		}

		/// <summary>
		/// Gets self and all its children
		/// </summary>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static IEnumerable<IDnSpyFile> GetAllChildrenAndSelf(this IDnSpyFile self) {
			yield return self;
			foreach (var c in self.GetAllChildren())
				yield return c;
		}

		/// <summary>
		/// Gets all its children and their children
		/// </summary>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static IEnumerable<IDnSpyFile> GetAllChildren(this IDnSpyFile self) {
			foreach (var c in GetAllChildren(self.Children))
				yield return c;
		}

		static IEnumerable<IDnSpyFile> GetAllChildren(IEnumerable<IDnSpyFile> files) {
			foreach (var f in files) {
				yield return f;
				foreach (var c in GetAllChildren(f.Children))
					yield return c;
			}
		}
	}
}
