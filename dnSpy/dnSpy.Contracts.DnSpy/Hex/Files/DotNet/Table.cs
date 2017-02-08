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

// from dnlib

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// The metadata tables
	/// </summary>
	public enum Table : byte {
		/// <summary>Module table (00h)</summary>
		Module,
		/// <summary>TypeRef table (01h)</summary>
		TypeRef,
		/// <summary>TypeDef table (02h)</summary>
		TypeDef,
		/// <summary>FieldPtr table (03h)</summary>
		FieldPtr,
		/// <summary>Field table (04h)</summary>
		Field,
		/// <summary>MethodPtr table (05h)</summary>
		MethodPtr,
		/// <summary>Method table (06h)</summary>
		Method,
		/// <summary>ParamPtr table (07h)</summary>
		ParamPtr,
		/// <summary>Param table (08h)</summary>
		Param,
		/// <summary>InterfaceImpl table (09h)</summary>
		InterfaceImpl,
		/// <summary>MemberRef table (0Ah)</summary>
		MemberRef,
		/// <summary>Constant table (0Bh)</summary>
		Constant,
		/// <summary>CustomAttribute table (0Ch)</summary>
		CustomAttribute,
		/// <summary>FieldMarshal table (0Dh)</summary>
		FieldMarshal,
		/// <summary>DeclSecurity table (0Eh)</summary>
		DeclSecurity,
		/// <summary>ClassLayout table (0Fh)</summary>
		ClassLayout,
		/// <summary>FieldLayout table (10h)</summary>
		FieldLayout,
		/// <summary>StandAloneSig table (11h)</summary>
		StandAloneSig,
		/// <summary>EventMap table (12h)</summary>
		EventMap,
		/// <summary>EventPtr table (13h)</summary>
		EventPtr,
		/// <summary>Event table (14h)</summary>
		Event,
		/// <summary>PropertyMap table (15h)</summary>
		PropertyMap,
		/// <summary>PropertyPtr table (16h)</summary>
		PropertyPtr,
		/// <summary>Property table (17h)</summary>
		Property,
		/// <summary>MethodSemantics table (18h)</summary>
		MethodSemantics,
		/// <summary>MethodImpl table (19h)</summary>
		MethodImpl,
		/// <summary>ModuleRef table (1Ah)</summary>
		ModuleRef,
		/// <summary>TypeSpec table (1Bh)</summary>
		TypeSpec,
		/// <summary>ImplMap table (1Ch)</summary>
		ImplMap,
		/// <summary>FieldRVA table (1Dh)</summary>
		FieldRVA,
		/// <summary>ENCLog table (1Eh)</summary>
		ENCLog,
		/// <summary>ENCMap table (1Fh)</summary>
		ENCMap,
		/// <summary>Assembly table (20h)</summary>
		Assembly,
		/// <summary>AssemblyProcessor table (21h)</summary>
		AssemblyProcessor,
		/// <summary>AssemblyOS table (22h)</summary>
		AssemblyOS,
		/// <summary>AssemblyRef table (23h)</summary>
		AssemblyRef,
		/// <summary>AssemblyRefProcessor table (24h)</summary>
		AssemblyRefProcessor,
		/// <summary>AssemblyRefOS table (25h)</summary>
		AssemblyRefOS,
		/// <summary>File table (26h)</summary>
		File,
		/// <summary>ExportedType table (27h)</summary>
		ExportedType,
		/// <summary>ManifestResource table (28h)</summary>
		ManifestResource,
		/// <summary>NestedClass table (29h)</summary>
		NestedClass,
		/// <summary>GenericParam table (2Ah)</summary>
		GenericParam,
		/// <summary>MethodSpec table (2Bh)</summary>
		MethodSpec,
		/// <summary>GenericParamConstraint table (2Ch)</summary>
		GenericParamConstraint,

		/// <summary>(Portable PDB) Document table (30h)</summary>
		Document = 0x30,
		/// <summary>(Portable PDB) MethodDebugInformation table (31h)</summary>
		MethodDebugInformation,
		/// <summary>(Portable PDB) LocalScope table (32h)</summary>
		LocalScope,
		/// <summary>(Portable PDB) LocalVariable table (33h)</summary>
		LocalVariable,
		/// <summary>(Portable PDB) LocalConstant table (34h)</summary>
		LocalConstant,
		/// <summary>(Portable PDB) ImportScope table (35h)</summary>
		ImportScope,
		/// <summary>(Portable PDB) StateMachineMethod table (36h)</summary>
		StateMachineMethod,
		/// <summary>(Portable PDB) CustomDebugInformation table (37h)</summary>
		CustomDebugInformation,
	}
}
