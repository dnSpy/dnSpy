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
		ValueTypeDef	= 0x00080000,
		TypeDefOther	= GenericTypeDef | NonGenericTypeDef | EnumTypeDef | InterfaceTypeDef | ClassTypeDef | ValueTypeDef,
		AnyTypeDef		= TypeDef | TypeDefOther,
		All				= AssemblyDef | ModuleDef | Namespace | TypeDef |
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

			if ((flags & VisibleMembersFlags.AssemblyDef) != 0) AddString(sb, "AssemblyDef", ref count);
			if ((flags & VisibleMembersFlags.ModuleDef) != 0) AddString(sb, "ModuleDef", ref count);
			if ((flags & VisibleMembersFlags.Namespace) != 0) AddString(sb, "Namespace", ref count);
			if ((flags & VisibleMembersFlags.TypeDef) != 0) AddString(sb, "TypeDef", ref count);
			if ((flags & VisibleMembersFlags.FieldDef) != 0) AddString(sb, "FieldDef", ref count);
			if ((flags & VisibleMembersFlags.MethodDef) != 0) AddString(sb, "MethodDef", ref count);
			if ((flags & VisibleMembersFlags.PropertyDef) != 0) AddString(sb, "PropertyDef", ref count);
			if ((flags & VisibleMembersFlags.EventDef) != 0) AddString(sb, "EventDef", ref count);
			if ((flags & VisibleMembersFlags.AssemblyRef) != 0) AddString(sb, "AssemblyRef", ref count);
			if ((flags & VisibleMembersFlags.BaseTypes) != 0) AddString(sb, "BaseTypes", ref count);
			if ((flags & VisibleMembersFlags.DerivedTypes) != 0) AddString(sb, "DerivedTypes", ref count);
			if ((flags & VisibleMembersFlags.ModuleRef) != 0) AddString(sb, "ModuleRef", ref count);
			if ((flags & VisibleMembersFlags.ResourceList) != 0) AddString(sb, "Resources", ref count);
			if ((flags & VisibleMembersFlags.NonNetFile) != 0) AddString(sb, "NonNetFile", ref count);
			if ((flags & VisibleMembersFlags.GenericTypeDef) != 0) AddString(sb, "Generic TypeDef", ref count);
			if ((flags & VisibleMembersFlags.NonGenericTypeDef) != 0) AddString(sb, "Non-Generic TypeDef", ref count);
			if ((flags & VisibleMembersFlags.EnumTypeDef) != 0) AddString(sb, "Enum TypeDef", ref count);
			if ((flags & VisibleMembersFlags.InterfaceTypeDef) != 0) AddString(sb, "Interface TypeDef", ref count);
			if ((flags & VisibleMembersFlags.ClassTypeDef) != 0) AddString(sb, "Class TypeDef", ref count);
			if ((flags & VisibleMembersFlags.ValueTypeDef) != 0) AddString(sb, "Value Type TypeDef", ref count);

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
