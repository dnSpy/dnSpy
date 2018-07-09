/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using System.Diagnostics;
using dnlib.DotNet;

namespace dnSpy.Roslyn.Compiler {
	enum FrameworkKind {
		Unknown,
		DotNetFramework2,
		DotNetFramework4,
		Unity2,
		Unity4,
	}

	static class FrameworkDetector {
		public static FrameworkKind GetFrameworkKind(ModuleDef module) {
			var corlib = module.CorLibTypes.AssemblyRef;
			var asmName = UTF8String.ToSystemStringOrEmpty(module.Assembly?.Name);
			if (CheckAssemblyName(unityAssemblyNames, asmName)) {
				Debug.Assert(corlib.Version.Major == 2 || corlib.Version.Major == 4);
				return corlib.Version.Major == 2 ? FrameworkKind.Unity2 : FrameworkKind.Unity4;
			}

			var info = TryGetTargetFrameworkAttribute(module.Assembly);
			if (info.framework != null) {
				if (info.framework == ".NETFramework") {
					if (info.version.StartsWith("2.") || info.version.StartsWith("3."))
						return FrameworkKind.DotNetFramework2;
					if (info.version.StartsWith("4."))
						return FrameworkKind.DotNetFramework4;
					Debug.Fail("Unknown .NET Framework version");
					return FrameworkKind.Unknown;
				}

				return FrameworkKind.Unknown;
			}

			switch (corlib.FullName) {
			case "mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089":
				return FrameworkKind.DotNetFramework2;
			case "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089":
				return FrameworkKind.DotNetFramework4;
			}

			return FrameworkKind.Unknown;
		}

		static (string framework, string version, string profile) TryGetTargetFrameworkAttribute(AssemblyDef asm) {
			var ca = asm?.CustomAttributes.Find("System.Runtime.Versioning.TargetFrameworkAttribute");
			if (ca == null)
				return default;

			if (ca.ConstructorArguments.Count != 1)
				return default;
			var arg = ca.ConstructorArguments[0];
			if (arg.Type.GetElementType() != ElementType.String)
				return default;
			var s = arg.Value as UTF8String;
			if (UTF8String.IsNullOrEmpty(s))
				return default;
			string attrString = s;

			// See corclr/src/mscorlib/src/System/Runtime/Versioning/BinaryCompatibility.cs
			var values = attrString.Split(new char[] { ',' });
			if (values.Length < 2 || values.Length > 3)
				return default;
			var framework = values[0].Trim();
			if (framework.Length == 0)
				return default;

			string versionStr = null;
			string profile = null;
			for (int i = 1; i < values.Length; i++) {
				var kvp = values[i].Split('=');
				if (kvp.Length != 2)
					return default;

				var key = kvp[0].Trim();
				var value = kvp[1].Trim();

				if (key.Equals("Version", StringComparison.OrdinalIgnoreCase)) {
					if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
						value = value.Substring(1);
					versionStr = value;
					if (!Version.TryParse(value, out var version))
						return default;
				}
				else if (key.Equals("Profile", StringComparison.OrdinalIgnoreCase)) {
					if (!string.IsNullOrEmpty(value))
						profile = value;
				}
			}
			if (versionStr == null || versionStr.Length == 0)
				return default;

			return (framework, versionStr, profile);
		}

		static bool CheckAssemblyName(string[] names, string name) {
			foreach (var n in names) {
				if (StringComparer.OrdinalIgnoreCase.Equals(n, name))
					return true;
			}
			return false;
		}

		static readonly string[] unityAssemblyNames = new string[] {
			"Assembly-CSharp",
			"Assembly-CSharp-firstpass",
		};
	}
}
