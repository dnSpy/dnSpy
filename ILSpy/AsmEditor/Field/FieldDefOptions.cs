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

using dnlib.PE;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.AsmEditor.Field
{
	sealed class FieldDefOptions
	{
		public FieldAttributes Attributes;
		public UTF8String Name;
		public FieldSig FieldSig;
		public uint? FieldOffset;
		public MarshalType MarshalType;
		public RVA RVA;
		public byte[] InitialValue;
		public ImplMap ImplMap;
		public Constant Constant;

		public FieldDefOptions()
		{
		}

		public FieldDefOptions(FieldDef field)
		{
			this.Attributes = field.Attributes;
			this.Name = field.Name;
			this.FieldSig = field.FieldSig;
			this.FieldOffset = field.FieldOffset;
			this.MarshalType = field.MarshalType;
			this.RVA = field.RVA;
			this.InitialValue = field.InitialValue;
			this.ImplMap = field.ImplMap;
			this.Constant = field.Constant;
		}

		public FieldDef CopyTo(FieldDef field)
		{
			field.Attributes = this.Attributes;
			field.Name = this.Name;
			field.FieldSig = this.FieldSig;
			field.FieldOffset = this.FieldOffset;
			field.MarshalType = this.MarshalType;
			field.RVA = this.RVA;
			field.InitialValue = this.InitialValue;
			field.ImplMap = this.ImplMap;
			field.Constant = this.Constant;
			return field;
		}

		public FieldDef CreateFieldDef()
		{
			return new FieldDefUser(Name, FieldSig, Attributes) {
				FieldOffset = FieldOffset,
				MarshalType = MarshalType,
				RVA = RVA,
				InitialValue = InitialValue,
				ImplMap = ImplMap,
				Constant = Constant,
			};
		}

		public static FieldDefOptions Create(UTF8String name, FieldSig fieldSig)
		{
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
