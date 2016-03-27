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
using System.Diagnostics;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Properties;

namespace dnSpy.Files.TreeView {
	static class ExeUtils {
		public static string GetArchString(ModuleDef module) {
			if (module == null)
				return "???";

			if (module.Machine == Machine.I386) {
				// See https://github.com/dotnet/coreclr/blob/master/src/inc/corhdr.h
				int c = (module.Is32BitRequired ? 2 : 0) + (module.Is32BitPreferred ? 1 : 0);
				switch (c) {
				case 0: // no special meaning, MachineType and ILONLY flag determine image requirements
					if (!module.IsILOnly)
						return "x86";
					return dnSpy_Resources.Arch_AnyCPU;
				case 1: // illegal, reserved for future use
					break;
				case 2: // image is x86-specific
					return "x86";
				case 3: // image is platform neutral and prefers to be loaded 32-bit when possible
					return dnSpy_Resources.Arch_AnyCPU_Preferred;
				}
			}

			return GetArchString(module.Machine);
		}

		public static string GetArchString(Machine machine) {
			switch (machine) {
			case Machine.I386:		return "x86";
			case Machine.AMD64:		return "x64";
			case Machine.IA64:		return "IA-64";
			case Machine.ARMNT:		return "ARM";
			case Machine.ARM64:		return "ARM64";
			default:				return machine.ToString();
			}
		}

		public static string GetDotNetVersion(ModuleDef module) {
			if (module == null)
				return dnSpy_Resources.UnknownDotNetRuntime;

			var asm = module.Assembly;
			if (asm != null && module.IsManifestModule) {
				var asmNetVer = GetDotNetVersion(asm);
				if (asmNetVer != null)
					return asmNetVer;
			}

			//TODO: Check for Silverlight etc...
			if (module.IsClr10)
				return ".NET Framework 1.0";
			if (module.IsClr11)
				return ".NET Framework 1.1";
			if (module.IsClr20)
				return ".NET Framework 2.0 - 3.5";
			if (module.IsClr40)
				return ".NET Framework 4.0 - 4.6";

			return dnSpy_Resources.UnknownDotNetRuntime;
		}

		static string GetDotNetVersion(AssemblyDef asm) {
			var ca = asm.CustomAttributes.Find("System.Runtime.Versioning.TargetFrameworkAttribute");
			if (ca == null)
				return null;

			if (ca.ConstructorArguments.Count != 1)
				return null;
			var arg = ca.ConstructorArguments[0];
			if (arg.Type.GetElementType() != ElementType.String)
				return null;
			var s = arg.Value as UTF8String;
			if (UTF8String.IsNullOrEmpty(s))
				return null;

			// See corclr/src/mscorlib/src/System/Runtime/Versioning/BinaryCompatibility.cs
			var values = s.String.Split(new char[] { ',' });
			if (values.Length < 2 || values.Length > 3)
				return null;
			var id = values[0].Trim();
			if (id.Length == 0)
				return null;

			string versionString = null;
			string profile = null;
			for (int i = 1; i < values.Length; i++) {
				var kvp = values[i].Split('=');
				if (kvp.Length != 2)
					return null;

				var key = kvp[0].Trim();
				var value = kvp[1].Trim();

				if (key.Equals("Version", StringComparison.OrdinalIgnoreCase)) {
					if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
						value = value.Substring(1);
					versionString = value;
					Version version = null;
					if (!Version.TryParse(value, out version))
						return null;
				}
				else if (key.Equals("Profile", StringComparison.OrdinalIgnoreCase)) {
					if (!string.IsNullOrEmpty(value))
						profile = value;
				}
			}

			var name = GetFrameworkName(id, versionString);
			if (name == null)
				return null;

			if (profile != null)
				name = name + " (" + profile + ")";
			return name;
		}

		static string GetFrameworkName(string id, string versionString) {
			if (versionString == null)
				return null;

			switch (id) {
			case ".NETFramework":
				if (versionString == "4.0")
					versionString = "4";
				return ".NET Framework " + versionString;

			case ".NETPortable":
				return ".NET Portable " + versionString;

			case ".NETCore":
				return ".NET Core " + versionString;

			case "DNXCore":
				return "DNX Core " + versionString;

			case "WindowsPhone":
				return "Windows Phone " + versionString;

			case "WindowsPhoneApp":
				return "Windows Phone App " + versionString;

			case "Silverlight":
				return "Silverlight " + versionString;

			default:
				Debug.Fail("Unknown target framework: " + id);
				if (id.Length > 20)
					return null;
				return id + " " + versionString;
			}
		}
	}
}
