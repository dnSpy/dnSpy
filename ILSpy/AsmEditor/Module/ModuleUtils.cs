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
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.PE;

namespace ICSharpCode.ILSpy.AsmEditor.Module
{
	static class ModuleUtils
	{
		public static ModuleDef CreateNetModule(string name, Guid mvid, ClrVersion clrVersion)
		{
			var module = CreateModuleDef(name, mvid, clrVersion);
			module.Kind = ModuleKind.NetModule;
			module.Characteristics = Characteristics.Dll | Characteristics._32BitMachine | Characteristics.ExecutableImage;
			module.DllCharacteristics = DllCharacteristics.TerminalServerAware | DllCharacteristics.NoSeh | DllCharacteristics.NxCompat | DllCharacteristics.DynamicBase;
			return module;
		}

		public static ModuleDef CreateModuleDef(string name, Guid mvid, ClrVersion clrVersion)
		{
			var clrValues = ClrVersionValues.GetValues(clrVersion);
			var module = new ModuleDefUser(name, mvid, clrValues.CorLibRef);
			module.RuntimeVersion = clrValues.RuntimeVersion;
			module.Cor20HeaderRuntimeVersion = clrValues.Cor20HeaderRuntimeVersion;
			module.TablesHeaderVersion = clrValues.TablesHeaderVersion;
			module.Location = string.Empty;
			return module;
		}

		public static AssemblyDef AddToNewAssemblyDef(ModuleDef module, ModuleKind moduleKind, out Characteristics characteristics)
		{
			var asmDef = new AssemblyDefUser(GetAssemblyName(module));
			asmDef.Modules.Add(module);
			WriteNewModuleKind(module, moduleKind, out characteristics);
			return asmDef;
		}

		static string GetAssemblyName(ModuleDef module)
		{
			string name = module.Name;
			if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
				name = name.Substring(0, name.Length - 4);
			else if (name.EndsWith(".netmodule", StringComparison.OrdinalIgnoreCase))
				name = name.Substring(0, name.Length - 10);
			if (!string.IsNullOrWhiteSpace(name))
				return name;
			return module.Name;
		}

		public static void WriteNewModuleKind(ModuleDef module, ModuleKind moduleKind, out Characteristics characteristics)
		{
			module.Kind = moduleKind;
			characteristics = module.Characteristics;
			module.Characteristics = SaveModule.CharacteristicsHelper.GetCharacteristics(module.Characteristics, moduleKind);
		}
	}
}
