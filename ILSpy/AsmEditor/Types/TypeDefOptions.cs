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

using System.Collections.Generic;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.AsmEditor.Types
{
	sealed class TypeDefOptions
	{
		public TypeAttributes Attributes;
		public UTF8String Namespace;
		public UTF8String Name;
		public ushort? PackingSize;
		public uint? ClassSize;
		public ITypeDefOrRef BaseType;
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();
		public List<DeclSecurity> DeclSecurities = new List<DeclSecurity>();

		public TypeDefOptions()
		{
		}

		public TypeDefOptions(TypeDef type)
		{
			this.Attributes = type.Attributes;
			this.Namespace = type.Namespace;
			this.Name = type.Name;
			if (type.ClassLayout == null) {
				PackingSize = null;
				ClassSize = null;
			}
			else {
				PackingSize = type.ClassLayout.PackingSize;
				ClassSize = type.ClassLayout.ClassSize;
			}
			this.BaseType = type.BaseType;
			this.CustomAttributes.AddRange(type.CustomAttributes);
			this.DeclSecurities.AddRange(type.DeclSecurities);
		}

		public TypeDef CopyTo(TypeDef type, ModuleDef ownerModule)
		{
			type.Attributes = this.Attributes;
			type.Namespace = this.Namespace;
			type.Name = this.Name;
			if (PackingSize != null || ClassSize != null)
				type.ClassLayout = ownerModule.UpdateRowId(new ClassLayoutUser(PackingSize ?? 0, ClassSize ?? 0));
			else
				type.ClassLayout = null;
			type.BaseType = this.BaseType;
			type.CustomAttributes.Clear();
			type.CustomAttributes.AddRange(CustomAttributes);
			type.DeclSecurities.Clear();
			type.DeclSecurities.AddRange(DeclSecurities);
			return type;
		}

		public TypeDef CreateTypeDef(ModuleDef ownerModule)
		{
			return ownerModule.UpdateRowId(CopyTo(new TypeDefUser(UTF8String.Empty), ownerModule));
		}

		public static TypeDefOptions Create(UTF8String ns, UTF8String name, ITypeDefOrRef baseType, bool isNestedType)
		{
			return new TypeDefOptions {
				Attributes = (isNestedType ? TypeAttributes.NestedPublic : TypeAttributes.Public) | TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.AnsiClass,
				Namespace = ns ?? UTF8String.Empty,
				Name = name ?? UTF8String.Empty,
				PackingSize = null,
				ClassSize = null,
				BaseType = baseType,
			};
		}
	}
}
