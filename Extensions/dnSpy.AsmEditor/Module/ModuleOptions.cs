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
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.PE;

namespace dnSpy.AsmEditor.Module {
	sealed class ModuleOptions {
		public Guid? Mvid;
		public Guid? EncId;
		public Guid? EncBaseId;
		public UTF8String? Name;
		public ModuleKind Kind;
		public Characteristics Characteristics;
		public DllCharacteristics DllCharacteristics;
		public string? RuntimeVersion;
		public Machine Machine;
		public ComImageFlags Cor20HeaderFlags;
		public uint? Cor20HeaderRuntimeVersion;
		public ushort? TablesHeaderVersion;
		public IManagedEntryPoint? ManagedEntryPoint;
		public RVA NativeEntryPoint;
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();

		public ModuleOptions() {
		}

		public ModuleOptions(ModuleDef module) {
			Mvid = module.Mvid;
			EncId = module.EncId;
			EncBaseId = module.EncBaseId;
			Name = module.Name;
			Kind = module.Kind;
			Characteristics = module.Characteristics;
			DllCharacteristics = module.DllCharacteristics;
			RuntimeVersion = module.RuntimeVersion;
			Machine = module.Machine;
			Cor20HeaderFlags = module.Cor20HeaderFlags;
			Cor20HeaderRuntimeVersion = module.Cor20HeaderRuntimeVersion;
			TablesHeaderVersion = module.TablesHeaderVersion;
			ManagedEntryPoint = module.ManagedEntryPoint;
			NativeEntryPoint = module.NativeEntryPoint;
			CustomAttributes.AddRange(module.CustomAttributes);
		}

		public ModuleDef CopyTo(ModuleDef module) {
			module.Mvid = Mvid;
			module.EncId = EncId;
			module.EncBaseId = EncBaseId;
			module.Name = Name ?? UTF8String.Empty;
			module.Kind = Kind;
			module.Characteristics = Characteristics;
			module.DllCharacteristics = DllCharacteristics;
			module.RuntimeVersion = RuntimeVersion;
			module.Machine = Machine;
			module.Cor20HeaderFlags = Cor20HeaderFlags;
			module.Cor20HeaderRuntimeVersion = Cor20HeaderRuntimeVersion;
			module.TablesHeaderVersion = TablesHeaderVersion;
			if (!(ManagedEntryPoint is null))
				module.ManagedEntryPoint = ManagedEntryPoint;
			else
				module.NativeEntryPoint = NativeEntryPoint;
			module.CustomAttributes.Clear();
			module.CustomAttributes.AddRange(CustomAttributes);
			return module;
		}

		public ModuleDef CreateModuleDef() {
			var module = CopyTo(new ModuleDefUser());
			return module.UpdateRowId(module);
		}
	}
}
