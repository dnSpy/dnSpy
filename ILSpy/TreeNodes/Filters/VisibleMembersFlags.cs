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
using System.Text;

namespace ICSharpCode.ILSpy.TreeNodes.Filters
{
	[Flags]
	enum VisibleMembersFlags
	{
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
		TypeDefOther	= GenericTypeDef | NonGenericTypeDef | EnumTypeDef | InterfaceTypeDef | ClassTypeDef | StructTypeDef | DelegateTypeDef,
		AnyTypeDef		= TypeDef | TypeDefOther,
		// What's shown in the normal treeview
		TreeViewAll		= AssemblyDef | ModuleDef | Namespace | TypeDef |
						  FieldDef | MethodDef | PropertyDef | EventDef |
						  AssemblyRef | BaseTypes | DerivedTypes | ModuleRef |
						  ResourceList | NonNetFile,
	}

	static class VisibleMembersFlagsExtensions
	{
		public static string GetListString(this VisibleMembersFlags flags)
		{
			int count;
			return flags.GetListString(out count);
		}

		public static string GetListString(this VisibleMembersFlags flags, out int count)
		{
			var sb = new StringBuilder();
			count = 0;

			if ((flags & VisibleMembersFlags.AssemblyDef) != 0) AddString(sb, "Assembly", ref count);
			if ((flags & VisibleMembersFlags.ModuleDef) != 0) AddString(sb, "Module", ref count);
			if ((flags & VisibleMembersFlags.Namespace) != 0) AddString(sb, "Namespace", ref count);
			if ((flags & VisibleMembersFlags.TypeDef) != 0) AddString(sb, "Type", ref count);
			if ((flags & VisibleMembersFlags.FieldDef) != 0) AddString(sb, "Field", ref count);
			if ((flags & VisibleMembersFlags.MethodDef) != 0) AddString(sb, "Method", ref count);
			if ((flags & VisibleMembersFlags.PropertyDef) != 0) AddString(sb, "Property", ref count);
			if ((flags & VisibleMembersFlags.EventDef) != 0) AddString(sb, "Event", ref count);
			if ((flags & VisibleMembersFlags.AssemblyRef) != 0) AddString(sb, "AssemblyRef", ref count);
			if ((flags & VisibleMembersFlags.BaseTypes) != 0) AddString(sb, "BaseTypes", ref count);
			if ((flags & VisibleMembersFlags.DerivedTypes) != 0) AddString(sb, "DerivedTypes", ref count);
			if ((flags & VisibleMembersFlags.ModuleRef) != 0) AddString(sb, "ModuleRef", ref count);
			if ((flags & VisibleMembersFlags.ResourceList) != 0) AddString(sb, "Resources", ref count);
			if ((flags & VisibleMembersFlags.NonNetFile) != 0) AddString(sb, "Non-.NET File", ref count);
			if ((flags & VisibleMembersFlags.GenericTypeDef) != 0) AddString(sb, "Generic Type", ref count);
			if ((flags & VisibleMembersFlags.NonGenericTypeDef) != 0) AddString(sb, "Non-Generic Type", ref count);
			if ((flags & VisibleMembersFlags.EnumTypeDef) != 0) AddString(sb, "Enum", ref count);
			if ((flags & VisibleMembersFlags.InterfaceTypeDef) != 0) AddString(sb, "Interface", ref count);
			if ((flags & VisibleMembersFlags.ClassTypeDef) != 0) AddString(sb, "Class", ref count);
			if ((flags & VisibleMembersFlags.StructTypeDef) != 0) AddString(sb, "Struct", ref count);
			if ((flags & VisibleMembersFlags.DelegateTypeDef) != 0) AddString(sb, "Delegate", ref count);
			if ((flags & VisibleMembersFlags.MethodBody) != 0) AddString(sb, "Method Body", ref count);
			if ((flags & VisibleMembersFlags.ParamDefs) != 0) AddString(sb, "ParamDefs", ref count);
			if ((flags & VisibleMembersFlags.ParamDef) != 0) AddString(sb, "ParamDef", ref count);
			if ((flags & VisibleMembersFlags.Locals) != 0) AddString(sb, "Locals", ref count);
			if ((flags & VisibleMembersFlags.Local) != 0) AddString(sb, "Local", ref count);
			if ((flags & VisibleMembersFlags.InstanceConstructor) != 0) AddString(sb, "Constructor", ref count);

			return sb.ToString();
		}

		static void AddString(StringBuilder sb, string text, ref int count)
		{
			if (count++ != 0)
				sb.Append(", ");
			sb.Append(text);
		}
	}
}
