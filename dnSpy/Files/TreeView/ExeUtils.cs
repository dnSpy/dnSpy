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
using System.Linq;
using System.Text.RegularExpressions;
using dnlib.DotNet;
using dnlib.PE;

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
					return "AnyCPU";
				case 1: // illegal, reserved for future use
					break;
				case 2: // image is x86-specific
					return "x86";
				case 3: // image is platform neutral and prefers to be loaded 32-bit when possible
					return "AnyCPU (32-bit preferred)";
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
				return "Unknown runtime";

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

			return "Unknown runtime";
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
			var values = s.String.Split(new char[] { ',' });
			bool isDNF = values.Any(a => StringComparer.OrdinalIgnoreCase.Equals(a, ".NETFramework"));
			const string VERSION_PREFIX = "Version=v";
			var verString = values.FirstOrDefault(a => a.StartsWith(VERSION_PREFIX, StringComparison.OrdinalIgnoreCase));
			if (isDNF && verString != null) {
				var ver = verString.Substring(VERSION_PREFIX.Length);
				var match = new Regex(@"^\d+(?:\.\d+(?:\.\d+(?:\.\d+)?)?)?$").Match(ver);
				// "65535.65535.65535.65535"
				if (match.Success && ver.Length <= 5 * 4 + 3) {
					if (ver == "4.0")
						ver = "4";
					return ".NET Framework " + ver;
				}
			}

			return null;
		}
	}
}
