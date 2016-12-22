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

// from dnlib

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// MD table column size
	/// </summary>
	public enum ColumnSize : byte {
		/// <summary>RID into Module table</summary>
		Module,
		/// <summary>RID into TypeRef table</summary>
		TypeRef,
		/// <summary>RID into TypeDef table</summary>
		TypeDef,
		/// <summary>RID into FieldPtr table</summary>
		FieldPtr,
		/// <summary>RID into Field table</summary>
		Field,
		/// <summary>RID into MethodPtr table</summary>
		MethodPtr,
		/// <summary>RID into Method table</summary>
		Method,
		/// <summary>RID into ParamPtr table</summary>
		ParamPtr,
		/// <summary>RID into Param table</summary>
		Param,
		/// <summary>RID into InterfaceImpl table</summary>
		InterfaceImpl,
		/// <summary>RID into MemberRef table</summary>
		MemberRef,
		/// <summary>RID into Constant table</summary>
		Constant,
		/// <summary>RID into CustomAttribute table</summary>
		CustomAttribute,
		/// <summary>RID into FieldMarshal table</summary>
		FieldMarshal,
		/// <summary>RID into DeclSecurity table</summary>
		DeclSecurity,
		/// <summary>RID into ClassLayout table</summary>
		ClassLayout,
		/// <summary>RID into FieldLayout table</summary>
		FieldLayout,
		/// <summary>RID into StandAloneSig table</summary>
		StandAloneSig,
		/// <summary>RID into EventMap table</summary>
		EventMap,
		/// <summary>RID into EventPtr table</summary>
		EventPtr,
		/// <summary>RID into Event table</summary>
		Event,
		/// <summary>RID into PropertyMap table</summary>
		PropertyMap,
		/// <summary>RID into PropertyPtr table</summary>
		PropertyPtr,
		/// <summary>RID into Property table</summary>
		Property,
		/// <summary>RID into MethodSemantics table</summary>
		MethodSemantics,
		/// <summary>RID into MethodImpl table</summary>
		MethodImpl,
		/// <summary>RID into ModuleRef table</summary>
		ModuleRef,
		/// <summary>RID into TypeSpec table</summary>
		TypeSpec,
		/// <summary>RID into ImplMap table</summary>
		ImplMap,
		/// <summary>RID into FieldRVA table</summary>
		FieldRVA,
		/// <summary>RID into ENCLog table</summary>
		ENCLog,
		/// <summary>RID into ENCMap table</summary>
		ENCMap,
		/// <summary>RID into Assembly table</summary>
		Assembly,
		/// <summary>RID into AssemblyProcessor table</summary>
		AssemblyProcessor,
		/// <summary>RID into AssemblyOS table</summary>
		AssemblyOS,
		/// <summary>RID into AssemblyRef table</summary>
		AssemblyRef,
		/// <summary>RID into AssemblyRefProcessor table</summary>
		AssemblyRefProcessor,
		/// <summary>RID into AssemblyRefOS table</summary>
		AssemblyRefOS,
		/// <summary>RID into File table</summary>
		File,
		/// <summary>RID into ExportedType table</summary>
		ExportedType,
		/// <summary>RID into ManifestResource table</summary>
		ManifestResource,
		/// <summary>RID into NestedClass table</summary>
		NestedClass,
		/// <summary>RID into GenericParam table</summary>
		GenericParam,
		/// <summary>RID into MethodSpec table</summary>
		MethodSpec,
		/// <summary>RID into GenericParamConstraint table</summary>
		GenericParamConstraint,
		/// <summary>RID into Document table</summary>
		Document = 0x30,
		/// <summary>RID into MethodDebugInformation table</summary>
		MethodDebugInformation,
		/// <summary>RID into LocalScope table</summary>
		LocalScope,
		/// <summary>RID into LocalVariable table</summary>
		LocalVariable,
		/// <summary>RID into LocalConstant table</summary>
		LocalConstant,
		/// <summary>RID into ImportScope table</summary>
		ImportScope,
		/// <summary>RID into StateMachineMethod table</summary>
		StateMachineMethod,
		/// <summary>RID into CustomDebugInformation table</summary>
		CustomDebugInformation,
		/// <summary>8-bit byte</summary>
		Byte = 0x40,
		/// <summary>16-bit signed int</summary>
		Int16,
		/// <summary>16-bit unsigned int</summary>
		UInt16,
		/// <summary>32-bit signed int</summary>
		Int32,
		/// <summary>32-bit unsigned int</summary>
		UInt32,
		/// <summary>Index into #Strings stream</summary>
		Strings,
		/// <summary>Index into #GUID stream</summary>
		GUID,
		/// <summary>Index into #Blob stream</summary>
		Blob,
		/// <summary>TypeDefOrRef encoded token</summary>
		TypeDefOrRef,
		/// <summary>HasConstant encoded token</summary>
		HasConstant,
		/// <summary>HasCustomAttribute encoded token</summary>
		HasCustomAttribute,
		/// <summary>HasFieldMarshal encoded token</summary>
		HasFieldMarshal,
		/// <summary>HasDeclSecurity encoded token</summary>
		HasDeclSecurity,
		/// <summary>MemberRefParent encoded token</summary>
		MemberRefParent,
		/// <summary>HasSemantic encoded token</summary>
		HasSemantic,
		/// <summary>MethodDefOrRef encoded token</summary>
		MethodDefOrRef,
		/// <summary>MemberForwarded encoded token</summary>
		MemberForwarded,
		/// <summary>Implementation encoded token</summary>
		Implementation,
		/// <summary>CustomAttributeType encoded token</summary>
		CustomAttributeType,
		/// <summary>ResolutionScope encoded token</summary>
		ResolutionScope,
		/// <summary>TypeOrMethodDef encoded token</summary>
		TypeOrMethodDef,
		/// <summary>HasCustomDebugInformation encoded token</summary>
		HasCustomDebugInformation,
	}
}
