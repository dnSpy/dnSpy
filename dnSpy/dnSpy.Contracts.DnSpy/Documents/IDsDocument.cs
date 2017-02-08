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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.PE;

namespace dnSpy.Contracts.Documents {
	/// <summary>
	/// A document, see also <see cref="IDsDotNetDocument"/> and <see cref="IDsPEDocument"/>
	/// </summary>
	public interface IDsDocument : IAnnotations {
		/// <summary>
		/// Used to serialize this instance. Null if it can't be serialized.
		/// </summary>
		DsDocumentInfo? SerializedDocument { get; }

		/// <summary>
		/// Gets a key for this document. Eg. a <see cref="FilenameKey"/> instance if it's a file
		/// loaded from disk. It's used to detect duplicate documents when adding a new document.
		/// </summary>
		IDsDocumentNameKey Key { get; }

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
		List<IDsDocument> Children { get; }

		/// <summary>
		/// true if <see cref="Children"/> has been initialized
		/// </summary>
		bool ChildrenLoaded { get; }
	}

	/// <summary>
	/// Extension methods
	/// </summary>
	public static class DsDocumentExtensionMethods {
		/// <summary>
		/// Gets the short name of <paramref name="document"/>, which is usually the filename without
		/// the extension.
		/// </summary>
		/// <param name="document">Document</param>
		/// <returns></returns>
		public static string GetShortName(this IDsDocument document) {
			var shortName = GetShortName(document.Filename);
			if (string.IsNullOrEmpty(shortName))
				shortName = GetDefaultShortName(document) ?? string.Empty;
			return shortName;
		}

		static string GetDefaultShortName(IDsDocument document) => document.ModuleDef?.Name;

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
		/// <param name="document">Document</param>
		/// <returns></returns>
		public static IEnumerable<IDsDocument> NonLoadedDescendantsAndSelf(this IDsDocument document) {
			if (document == null)
				throw new ArgumentNullException(nameof(document));
			yield return document;
			if (document.ChildrenLoaded) {
				foreach (var child in document.Children) {
					foreach (var f in child.NonLoadedDescendantsAndSelf())
						yield return f;
				}
			}
		}

		/// <summary>
		/// Gets all modules in this instance and any children
		/// </summary>
		/// <typeparam name="T"><see cref="ModuleDefMD"/> or <see cref="ModuleDefMD"/></typeparam>
		/// <param name="document">Document</param>
		/// <returns></returns>
		public static IEnumerable<T> GetModules<T>(this IDsDocument document) where T : ModuleDef => GetModules(new HashSet<T>(), new[] { document });

		/// <summary>
		/// Gets all modules in this instance and any children
		/// </summary>
		/// <typeparam name="T"><see cref="ModuleDefMD"/> or <see cref="ModuleDefMD"/></typeparam>
		/// <param name="documents">Documents</param>
		/// <returns></returns>
		public static IEnumerable<T> GetModules<T>(this IEnumerable<IDsDocument> documents) where T : ModuleDef => GetModules(new HashSet<T>(), documents);

		static IEnumerable<T> GetModules<T>(HashSet<T> hash, IEnumerable<IDsDocument> documents) where T : ModuleDef {
			foreach (var f in documents.SelectMany(f => f.NonLoadedDescendantsAndSelf())) {
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
		public static IEnumerable<IDsDocument> GetAllChildrenAndSelf(this IDsDocument self) {
			yield return self;
			foreach (var c in self.GetAllChildren())
				yield return c;
		}

		/// <summary>
		/// Gets all its children and their children
		/// </summary>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static IEnumerable<IDsDocument> GetAllChildren(this IDsDocument self) {
			foreach (var c in GetAllChildren(self.Children))
				yield return c;
		}

		static IEnumerable<IDsDocument> GetAllChildren(IEnumerable<IDsDocument> documents) {
			foreach (var d in documents) {
				yield return d;
				foreach (var c in GetAllChildren(d.Children))
					yield return c;
			}
		}
	}
}
