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

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	sealed class ParamDefOptions
	{
		public UTF8String Name;
		public ushort Sequence;
		public ParamAttributes Attributes;
		public Constant Constant;
		public MarshalType MarshalType;
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();

		public ParamDefOptions()
		{
		}

		public ParamDefOptions(ParamDef pd)
		{
			this.Name = pd.Name;
			this.Sequence = pd.Sequence;
			this.Attributes = pd.Attributes;
			this.Constant = pd.Constant;
			this.MarshalType = pd.MarshalType;
			this.CustomAttributes.AddRange(pd.CustomAttributes);
		}

		public ParamDef CopyTo(ParamDef pd)
		{
			pd.Name = this.Name ?? UTF8String.Empty;
			pd.Sequence = this.Sequence;
			pd.Attributes = this.Attributes;
			pd.Constant = this.Constant;
			pd.MarshalType = this.MarshalType;
			pd.CustomAttributes.Clear();
			pd.CustomAttributes.AddRange(CustomAttributes);
			return pd;
		}

		public ParamDef Create(ModuleDef ownerModule)
		{
			return ownerModule.UpdateRowId(CopyTo(new ParamDefUser()));
		}
	}
}
