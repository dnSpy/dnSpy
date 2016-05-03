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
using dnlib.DotNet;

namespace dnSpy.AsmEditor.Assembly {
	sealed class AssemblyOptions {
		public AssemblyHashAlgorithm HashAlgorithm;
		public Version Version;
		public AssemblyAttributes Attributes;
		public PublicKey PublicKey;
		public UTF8String Name;
		public string Culture;
		public Module.ClrVersion ClrVersion;
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();
		public List<DeclSecurity> DeclSecurities = new List<DeclSecurity>();

		public AssemblyOptions() {
		}

		public AssemblyOptions(AssemblyDef asm) {
			this.HashAlgorithm = asm.HashAlgorithm;
			this.Version = asm.Version;
			this.Attributes = asm.Attributes;
			this.PublicKey = asm.PublicKey;
			this.Name = asm.Name;
			this.Culture = asm.Culture;
			this.ClrVersion = Module.ClrVersion.DefaultVersion;
			this.CustomAttributes.AddRange(asm.CustomAttributes);
			this.DeclSecurities.AddRange(asm.DeclSecurities);
		}

		public AssemblyDef CopyTo(AssemblyDef asm) {
			asm.HashAlgorithm = this.HashAlgorithm;
			asm.Version = this.Version;
			asm.Attributes = this.Attributes;
			asm.PublicKey = this.PublicKey;
			asm.Name = this.Name ?? UTF8String.Empty;
			asm.Culture = this.Culture;
			asm.CustomAttributes.Clear();
			asm.CustomAttributes.AddRange(CustomAttributes);
			asm.DeclSecurities.Clear();
			asm.DeclSecurities.AddRange(DeclSecurities);
			return asm;
		}

		public AssemblyDef CreateAssemblyDef(ModuleDef ownerModule) => ownerModule.UpdateRowId(CopyTo(new AssemblyDefUser()));

		public static AssemblyOptions Create(string name) {
			return new AssemblyOptions {
				HashAlgorithm = AssemblyHashAlgorithm.SHA1,
				Version = new Version(0, 0, 0, 0),
				Attributes = AssemblyAttributes.None,
				PublicKey = new PublicKey(Array.Empty<byte>()),
				Name = name,
				Culture = string.Empty,
				ClrVersion = Module.ClrVersion.DefaultVersion,
			};
		}
	}
}
