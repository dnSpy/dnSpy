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

using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.PE;

namespace dnSpy.AsmEditor.Field {
	sealed class FieldDefOptions {
		public FieldAttributes Attributes;
		public UTF8String Name;
		public FieldSig FieldSig;
		public uint? FieldOffset;
		public MarshalType MarshalType;
		public RVA RVA;
		public byte[] InitialValue;
		public ImplMap ImplMap;
		public Constant Constant;
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();

		public FieldDefOptions() {
		}

		public FieldDefOptions(FieldDef field) {
			Attributes = field.Attributes;
			Name = field.Name;
			FieldSig = field.FieldSig;
			FieldOffset = field.FieldOffset;
			MarshalType = field.MarshalType;
			RVA = field.RVA;
			InitialValue = field.InitialValue;
			ImplMap = field.ImplMap;
			Constant = field.Constant;
			CustomAttributes.AddRange(field.CustomAttributes);
		}

		public FieldDef CopyTo(FieldDef field) {
			field.Attributes = Attributes;
			field.Name = Name ?? UTF8String.Empty;
			field.FieldSig = FieldSig;
			field.FieldOffset = FieldOffset;
			field.MarshalType = MarshalType;
			field.RVA = RVA;
			field.InitialValue = InitialValue;
			field.ImplMap = ImplMap;
			field.Constant = Constant;
			field.CustomAttributes.Clear();
			field.CustomAttributes.AddRange(CustomAttributes);
			return field;
		}

		public FieldDef CreateFieldDef(ModuleDef ownerModule) => ownerModule.UpdateRowId(CopyTo(new FieldDefUser()));

		public static FieldDefOptions Create(UTF8String name, FieldSig fieldSig) {
			return new FieldDefOptions {
				Attributes = FieldAttributes.Public,
				Name = name,
				FieldSig = fieldSig,
				FieldOffset = null,
				MarshalType = null,
				RVA = 0,
				InitialValue = null,
				ImplMap = null,
				Constant = null,
			};
		}
	}
}
