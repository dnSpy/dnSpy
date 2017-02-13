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
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnSpy.Decompiler.MSBuild {
	sealed class ResourceNameCreator {
		readonly ModuleDef module;
		readonly FilenameCreator filenameCreator;

		public ResourceNameCreator(ModuleDef module, FilenameCreator filenameCreator) {
			this.module = module;
			this.filenameCreator = filenameCreator;
		}

		public string GetResxFilename(string resourceName, out string typeFullName) {
			const string RESOURCES_EXT = ".resources";
			const string RESX_EXT = ".resx";
			var n = resourceName;
			if (n.EndsWith(RESOURCES_EXT, StringComparison.OrdinalIgnoreCase))
				n = n.Substring(0, n.Length - RESOURCES_EXT.Length);

			var type = module.Find(n, true);
			if (type != null && DotNetUtils.IsWinForm(type)) {
				typeFullName = type.ReflectionFullName;
				return filenameCreator.CreateFromNamespaceName(RESX_EXT, type.Namespace, type.Name);
			}

			var resXType = GetResXType(type, n);
			if (resXType != null) {
				typeFullName = resXType.ReflectionFullName;
				return filenameCreator.CreateFromNamespaceName(RESX_EXT, resXType.ReflectionNamespace, GetResxDesignerFilename(resXType.ReflectionNamespace, n));
			}

			typeFullName = n;
			return filenameCreator.Create(RESX_EXT, n);
		}

		string GetResxDesignerFilename(string ns, string name) {
			if (name.StartsWith(ns + ".", StringComparison.Ordinal))
				return name.Substring(ns.Length + 1);
			Debug.Fail("Weird name");
			return name;
		}

		TypeDef GetResXType(TypeDef type, string name) {
			if (type != null && IsResXType(type, name))
				return type;
			return FindResXType(name);
		}

		TypeDef FindResXType(string name) {
			if (resXNameToType == null) {
				var dict = new Dictionary<string, TypeDef>(StringComparer.Ordinal);

				foreach (var t in module.Types) {
					string s = GetResXString(t);
					if (s != null)
						dict[s] = t;
				}

				resXNameToType = dict;
			}

			resXNameToType.TryGetValue(name, out var type);
			return type;
		}
		Dictionary<string, TypeDef> resXNameToType;

		static string GetResXString(TypeDef type) {
			if (!type.Fields.Any(a => a.IsStatic && a.FieldType != null && a.FieldType.ToString() == "System.Globalization.CultureInfo"))
				return null;
			if (!type.Fields.Any(a => a.IsStatic && a.FieldType != null && a.FieldType.ToString() == "System.Resources.ResourceManager"))
				return null;
			foreach (var m in type.Methods) {
				var body = m.Body;
				if (body == null)
					continue;
				var instrs = body.Instructions;
				for (int i = 0; i + 2 < instrs.Count; i++) {
					if (instrs[i].OpCode.Code != Code.Ldstr)
						continue;
					if (instrs[i + 1].OpCode.Code != Code.Ldtoken)
						continue;
					if (instrs[i + 2].OpCode.Code != Code.Call)
						continue;
					var s = instrs[i].Operand as string;
					if (s == null)
						continue;
					var cm = instrs[i + 2].Operand as IMethod;
					if (cm == null || cm.FullName != "System.Type System.Type::GetTypeFromHandle(System.RuntimeTypeHandle)")
						continue;
					return s;
				}
			}
			return null;
		}

		bool IsResXType(TypeDef type, string name) {
			foreach (var m in type.Methods) {
				var body = m.Body;
				if (body == null)
					continue;
				bool b = body.Instructions.Any(a => a.Operand is string && name.Equals((string)a.Operand));
				if (b)
					return true;
			}
			return false;
		}

		public string GetXamlResourceFilename(string resourceName) => GetBamlResourceName(resourceName);

		string GetBamlResourceName(string resourceName) {
			if (namespaces == null)
				Initialize();

			var ext = FileUtils.GetExtension(resourceName);
			var nameNoExt = resourceName.Substring(0, resourceName.Length - ext.Length);
			var ns = GetNamespace(resourceName);

			if (partialNamespaceMap.TryGetValue(ns, out string fixedNs))
				nameNoExt = fixedNs.Replace('.', '/') + "/" + nameNoExt.Substring(ns.Length + 1);

			return filenameCreator.CreateFromRelativePath(nameNoExt, ext);
		}

		static string GetNamespace(string name) {
			// name can contain illegal chars so don't use Path methods
			int i = name.LastIndexOf('/');
			if (i < 0)
				return string.Empty;
			return name.Substring(0, i).Replace('/', '.');
		}

		public string GetBamlResourceName(string resourceName, out string typeFullName) {
			if (namespaces == null)
				Initialize();

			Debug.Assert(resourceName.EndsWith(".baml", StringComparison.OrdinalIgnoreCase));
			var name = resourceName.Substring(0, resourceName.Length - ".baml".Length);
			var nameNoExt = name;
			name = name.Replace('/', '.');
			typeFullName = GetFullName(name);
			if (!string.IsNullOrEmpty(typeFullName))
				return filenameCreator.Create(".xaml", typeFullName);

			return GetBamlResourceName(nameNoExt + ".xaml");
		}

		string GetFullName(string partialName) {
			var name = partialName;
			if (!string.IsNullOrEmpty(filenameCreator.DefaultNamespace))
				name = filenameCreator.DefaultNamespace + "." + name;
			if (typeToFullNameMap.TryGetValue(name, out string fullName))
				return fullName;
			partialTypeToFullNameMap.TryGetValue(partialName, out fullName);
			return fullName;
		}

		public string GetResourceFilename(string resourceName) {
			if (namespaces == null)
				Initialize();

			string[] parts = resourceName.Split(new char[] { '.' });
			var possibleNamespaces = new List<string>(parts.Length);
			var sb = new StringBuilder(resourceName.Length);
			for (int i = 0; i < parts.Length - 1; i++) {
				if (sb.Length > 0)
					sb.Append(".");
				sb.Append(parts[i]);
				var ns = sb.ToString();
				lowerCaseNsToReal.TryGetValue(ns, out string realNs);
				possibleNamespaces.Add(realNs ?? ns);
			}
			for (int i = possibleNamespaces.Count - 1; i >= 0; i--) {
				var ns = possibleNamespaces[i];
				if (namespaces.Contains(ns)) {
					var filename = resourceName.Substring(ns.Length + 1);
					return filenameCreator.CreateFromNamespaceFilename(ns, filename);
				}
			}

			return filenameCreator.CreateName(resourceName);
		}

		void Initialize() {
			if (namespaces != null)
				return;

			// Only include actual used namespaces, eg. if "ns1.ns2.Type1" is used, but there's no
			// "ns1.TypeX", don't include "ns1" as a valid namespace.
			var hash = new HashSet<string>(module.Types.Select(a => UTF8String.ToSystemStringOrEmpty(a.Namespace)));
			hash.Remove(string.Empty);

			var sb = new StringBuilder();
			var dict = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			foreach (var ns in hash) {
				sb.Clear();
				foreach (var n in ns.Split('.')) {
					if (sb.Length > 0)
						sb.Append('.');
					sb.Append(n);
					var ns2 = sb.ToString();
					dict[ns2] = ns2;
				}
			}

			// XAML resources only include the last part of types, eg. BaseNS.NS1.Type1 => ns1/type1
			var pmap2 = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			var pmap3 = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			var pnsmap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			var stringCache = new Dictionary<string, string>(StringComparer.Ordinal);
			// Don't include nested types
			foreach (var t in module.Types) {
				string fullName;
				var nsu = t.Namespace;
				if (UTF8String.IsNullOrEmpty(nsu))
					fullName = (t.Name ?? UTF8String.Empty).String;
				else
					fullName = nsu.String + "." + (t.Name ?? UTF8String.Empty).String;
				pmap3[fullName] = fullName;
				var name = fullName;
				while (name.Length > 0) {
					pmap2[name] = fullName;
					int index = name.IndexOf('.');
					if (index < 0)
						break;
					name = name.Substring(index + 1);
				}

				var ns = (t.Namespace ?? UTF8String.Empty).String;
				while (ns.Length > 0) {
					if (stringCache.TryGetValue(ns, out string tmp))
						ns = tmp;
					else
						stringCache[ns] = ns;
					pnsmap[ns] = ns;
					int index = ns.IndexOf('.');
					if (index < 0)
						break;
					ns = ns.Substring(index + 1);
				}
			}

			partialNamespaceMap = pnsmap;
			partialTypeToFullNameMap = pmap2;
			typeToFullNameMap = pmap3;
			lowerCaseNsToReal = dict;
			namespaces = hash;
		}
		HashSet<string> namespaces;
		Dictionary<string, string> lowerCaseNsToReal;
		Dictionary<string, string> partialNamespaceMap;
		Dictionary<string, string> partialTypeToFullNameMap;
		Dictionary<string, string> typeToFullNameMap;
	}
}
