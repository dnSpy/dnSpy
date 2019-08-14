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
using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace dnSpy.Decompiler.MSBuild {
	readonly struct DefaultNamespaceFinder {
		readonly ModuleDef module;

		public DefaultNamespaceFinder(ModuleDef module) => this.module = module;

		readonly struct Info {
			public readonly string FirstPart;
			public readonly string CommonPrefix;
			public readonly string[] Namespaces;
			public Info(string first, string common, string[] namespaces) {
				FirstPart = first;
				CommonPrefix = common;
				Namespaces = namespaces;
			}
		}

		public string Find() {
			var nss = new HashSet<string>(module.Types.Select(a => a.Namespace.String).Where(IsValidNamespace), StringComparer.Ordinal);
			var infos = new List<Info>();
			foreach (var f in nss.Select(a => GetFirstPart(a)).Distinct()) {
				var ary = nss.Where(a => a.Equals(f) || a.StartsWith(f + ".", StringComparison.Ordinal)).ToArray();
				var info = new Info(f, GetCommon(ary), ary);
				infos.Add(info);
			}

			var info2 = PickNamespace(infos);

			var modNs = GetModuleNamespace(module);
			var foundNs = info2.CommonPrefix;
			if (!string.IsNullOrEmpty(modNs) && !(foundNs is null) && foundNs.StartsWith(modNs + "."))
				foundNs = modNs;

			return foundNs ?? string.Empty;
		}

		Info PickNamespace(List<Info> infos) {
			if (infos.Count == 0)
				return new Info();
			if (infos.Count == 1)
				return infos[0];

			var modNs = GetModuleNamespace(module);
			var info = infos.FirstOrDefault(a => modNs.Equals(a.CommonPrefix) || a.CommonPrefix.StartsWith(modNs));
			if (!(info.CommonPrefix is null))
				return info;

			// Here if it's eg. mscorlib, System.Xml or other system assemblies with several
			// different namespaces.
			return new Info();
		}

		static string GetCommon(string[] namespaces) {
			string? foundNs = null;
			var sb = new StringBuilder();
			foreach (var ns in namespaces) {
				Debug.Assert(IsValidNamespace(ns));
				if (foundNs is null)
					foundNs = ns;
				else
					foundNs = GetCommonNamespace(sb, foundNs, ns);
			}
			Debug2.Assert(!(foundNs is null));
			return foundNs ?? string.Empty;
		}

		static string GetFirstPart(string ns) {
			int i = ns.IndexOf('.');
			return i < 0 ? ns : ns.Substring(0, i);
		}

		static bool IsValidNamespace(string ns) => !string.IsNullOrEmpty(ns) && ns != "XamlGeneratedNamespace";

		static string GetCommonNamespace(StringBuilder sb, string a, string b) {
			sb.Clear();
			var na = a.Split('.');
			var nb = b.Split('.');
			for (int i = 0; i < na.Length && i < nb.Length; i++) {
				if (!StringComparer.Ordinal.Equals(na[i], nb[i]))
					break;
				if (sb.Length > 0)
					sb.Append('.');
				sb.Append(na[i]);
			}
			return sb.ToString();
		}

		static string GetModuleNamespace(ModuleDef module) {
			var asm = module.Assembly;
			string s;
			if (!(asm is null) && module.IsManifestModule)
				s = asm.Name;
			else {
				s = module.Name;
				if (s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
					s = s.Substring(0, s.Length - 4);
				if (s.EndsWith(".netmodule", StringComparison.OrdinalIgnoreCase))
					s = s.Substring(0, s.Length - 10);
				else
					s = string.Empty;
			}
			return s.Replace('-', '_');
		}
	}
}
