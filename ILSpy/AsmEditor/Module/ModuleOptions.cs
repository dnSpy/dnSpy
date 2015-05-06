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
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.PE;

namespace ICSharpCode.ILSpy.AsmEditor.Module
{
	sealed class ModuleOptions
	{
		public Guid? Mvid;
		public Guid? EncId;
		public Guid? EncBaseId;
		public string Name;
		public ModuleKind Kind;
		public Characteristics Characteristics;
		public DllCharacteristics DllCharacteristics;
		public string RuntimeVersion;
		public Machine Machine;
		public ComImageFlags Cor20HeaderFlags;
		public uint? Cor20HeaderRuntimeVersion;
		public ushort? TablesHeaderVersion;
		public IManagedEntryPoint ManagedEntryPoint;
		public RVA NativeEntryPoint;
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();

		public ModuleOptions()
		{
		}

		public ModuleOptions(ModuleDef module)
		{
			this.Mvid = module.Mvid;
			this.EncId = module.EncId;
			this.EncBaseId = module.EncBaseId;
			this.Name = module.Name;
			this.Kind = module.Kind;
			this.Characteristics = module.Characteristics;
			this.DllCharacteristics = module.DllCharacteristics;
			this.RuntimeVersion = module.RuntimeVersion;
			this.Machine = module.Machine;
			this.Cor20HeaderFlags = module.Cor20HeaderFlags;
			this.Cor20HeaderRuntimeVersion = module.Cor20HeaderRuntimeVersion;
			this.TablesHeaderVersion = module.TablesHeaderVersion;
			this.ManagedEntryPoint = module.ManagedEntryPoint;
			this.NativeEntryPoint = module.NativeEntryPoint;
			this.CustomAttributes.AddRange(module.CustomAttributes);
		}

		public ModuleDef CopyTo(ModuleDef module)
		{
			module.Mvid = this.Mvid;
			module.EncId = this.EncId;
			module.EncBaseId = this.EncBaseId;
			module.Name = this.Name;
			module.Kind = this.Kind;
			module.Characteristics = this.Characteristics;
			module.DllCharacteristics = this.DllCharacteristics;
			module.RuntimeVersion = this.RuntimeVersion;
			module.Machine = this.Machine;
			module.Cor20HeaderFlags = this.Cor20HeaderFlags;
			module.Cor20HeaderRuntimeVersion = this.Cor20HeaderRuntimeVersion;
			module.TablesHeaderVersion = this.TablesHeaderVersion;
			if (ManagedEntryPoint != null)
				module.ManagedEntryPoint = this.ManagedEntryPoint;
			else
				module.NativeEntryPoint = this.NativeEntryPoint;
			module.CustomAttributes.Clear();
			module.CustomAttributes.AddRange(CustomAttributes);
			return module;
		}

		public ModuleDef CreateModuleDef()
		{
			return CopyTo(new ModuleDefUser());
		}
	}
}
