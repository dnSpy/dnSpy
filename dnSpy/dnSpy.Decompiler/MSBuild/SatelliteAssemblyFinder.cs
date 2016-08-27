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
using System.Globalization;
using System.IO;
using System.Linq;
using dnlib.DotNet;

namespace dnSpy.Decompiler.MSBuild {
	sealed class SatelliteAssemblyFinder : IDisposable {
		readonly HashSet<string> cultures;
		readonly Dictionary<string, ModuleDef> openedModules;

		public SatelliteAssemblyFinder() {
			this.cultures = new HashSet<string>(CultureInfo.GetCultures(CultureTypes.AllCultures).Select(a => a.Name), StringComparer.OrdinalIgnoreCase);
			this.openedModules = new Dictionary<string, ModuleDef>(StringComparer.OrdinalIgnoreCase);
		}

		bool IsValidCulture(string name) => !string.IsNullOrEmpty(name) && cultures.Contains(name);

		public IEnumerable<ModuleDef> GetSatelliteAssemblies(ModuleDef module) {
			var asm = module.Assembly;
			if (asm == null)
				yield break;
			var satAsmName = new AssemblyNameInfo(asm);
			satAsmName.Name = asm.Name + ".resources";
			foreach (var filename in GetFiles(asm, module)) {
				if (!File.Exists(filename))
					continue;
				var satAsm = TryOpenAssembly(filename);
				if (satAsm == null || !AssemblyNameComparer.NameAndPublicKeyTokenOnly.Equals(satAsmName, satAsm))
					continue;
				yield return satAsm.ManifestModule;
			}
		}

		IEnumerable<string> GetFiles(AssemblyDef asm, ModuleDef mod) {
			var baseDir = GetBaseDirectory(asm, mod);
			if (string.IsNullOrEmpty(baseDir))
				yield break;
			var baseDirs = new List<string>();
			baseDirs.Add(baseDir);
			//TODO: Add all privatePath dirs found in app.config
			foreach (var bd in baseDirs) {
				foreach (var dir in GetDirectories(baseDir)) {
					var name = Path.GetFileName(dir);
					if (!IsValidCulture(name))
						continue;
					yield return Path.Combine(dir, asm.Name + ".resources.dll");
					yield return Path.Combine(dir, asm.Name, asm.Name + ".resources.dll");
				}
			}
		}

		string GetBaseDirectory(AssemblyDef asm, ModuleDef mod) {
			if (string.IsNullOrEmpty(mod.Location))
				return string.Empty;
			try {
				return Path.GetDirectoryName(mod.Location);
			}
			catch {
				return string.Empty;
			}
		}

		static string[] GetDirectories(string dir) {
			try {
				return Directory.GetDirectories(dir);
			}
			catch {
			}
			return Array.Empty<string>();
		}

		AssemblyDef TryOpenAssembly(string filename) {
			lock (openedModules) {
				ModuleDef mod;
				if (openedModules.TryGetValue(filename, out mod))
					return mod.Assembly;
				openedModules[filename] = null;
				if (!File.Exists(filename))
					return null;
				try {
					mod = ModuleDefMD.Load(filename);
					if (mod.Assembly == null || UTF8String.IsNullOrEmpty(mod.Assembly.Culture)) {
						mod.Dispose();
						return null;
					}
					openedModules[filename] = mod;
					return mod.Assembly;
				}
				catch {
					return null;
				}
			}
		}

		public void Dispose() {
			foreach (var mod in openedModules.Values)
				mod.Dispose();
		}
	}
}
