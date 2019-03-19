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
			HashAlgorithm = asm.HashAlgorithm;
			Version = asm.Version;
			Attributes = asm.Attributes;
			PublicKey = asm.PublicKey;
			Name = asm.Name;
			Culture = asm.Culture;
			ClrVersion = Module.ClrVersion.DefaultVersion;
			CustomAttributes.AddRange(asm.CustomAttributes);
			DeclSecurities.AddRange(asm.DeclSecurities);
		}

		public AssemblyDef CopyTo(AssemblyDef asm) {
			asm.HashAlgorithm = HashAlgorithm;
			asm.Version = Version;
			asm.Attributes = Attributes;
			asm.PublicKey = PublicKey;
			asm.Name = Name ?? UTF8String.Empty;
			asm.Culture = Culture;
			asm.CustomAttributes.Clear();
			asm.CustomAttributes.AddRange(CustomAttributes);
			asm.DeclSecurities.Clear();
			asm.DeclSecurities.AddRange(DeclSecurities);
			return asm;
		}

		public AssemblyDef CreateAssemblyDef(ModuleDef ownerModule) => ownerModule.UpdateRowId(CopyTo(new AssemblyDefUser()));

		public static AssemblyOptions Create(string name) => new AssemblyOptions {
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
