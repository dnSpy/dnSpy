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

namespace dnSpy.Contracts.Search {
	/// <summary>
	/// Filter flags
	/// </summary>
	[Flags]
	public enum VisibleMembersFlags {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		AssemblyDef		= 0x00000001,
		ModuleDef		= 0x00000002,
		Namespace		= 0x00000004,
		TypeDef			= 0x00000008,
		FieldDef		= 0x00000010,
		MethodDef		= 0x00000020,
		PropertyDef		= 0x00000040,
		EventDef		= 0x00000080,
		AssemblyRef		= 0x00000100,
		BaseTypes		= 0x00000200,
		DerivedTypes	= 0x00000400,
		ModuleRef		= 0x00000800,
		ResourceList	= 0x00001000,
		NonNetFile		= 0x00002000,
		GenericTypeDef	= 0x00004000,
		NonGenericTypeDef=0x00008000,
		EnumTypeDef		= 0x00010000,
		InterfaceTypeDef= 0x00020000,
		ClassTypeDef	= 0x00040000,
		StructTypeDef	= 0x00080000,
		DelegateTypeDef	= 0x00100000,
		MethodBody		= 0x00200000,
		InstanceConstructor = 0x00400000,
		ParamDefs		= 0x00800000,
		ParamDef		= 0x01000000,
		Locals			= 0x02000000,
		Local			= 0x04000000,
		Resource		= 0x08000000,
		ResourceElement	= 0x10000000,
		Other			= 0x20000000,
		Attributes		= 0x40000000,
		TypeDefOther	= GenericTypeDef | NonGenericTypeDef | EnumTypeDef | InterfaceTypeDef | ClassTypeDef | StructTypeDef | DelegateTypeDef,
		AnyTypeDef		= TypeDef | TypeDefOther,
		// What's shown in the normal treeview
		TreeViewAll		= AssemblyDef | ModuleDef | Namespace | TypeDef |
						  FieldDef | MethodDef | PropertyDef | EventDef |
						  AssemblyRef | BaseTypes | DerivedTypes | ModuleRef |
						  ResourceList | NonNetFile | Resource | ResourceElement |
						  Other,
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
