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
	sealed class GenericParamOptions
	{
		public ushort Number;
		public GenericParamAttributes Flags;
		public UTF8String Name;
		public ITypeDefOrRef Kind;
		public List<GenericParamConstraint> GenericParamConstraints = new List<GenericParamConstraint>();
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();

		public GenericParamOptions()
		{
		}

		public GenericParamOptions(GenericParam gp)
		{
			this.Number = gp.Number;
			this.Flags = gp.Flags;
			this.Name = gp.Name;
			this.Kind = gp.Kind;
			this.GenericParamConstraints.AddRange(gp.GenericParamConstraints);
			this.CustomAttributes.AddRange(gp.CustomAttributes);
		}

		public GenericParam CopyTo(GenericParam gp)
		{
			gp.Number = this.Number;
			gp.Flags = this.Flags;
			gp.Name = this.Name ?? UTF8String.Empty;
			gp.Kind = this.Kind;
			gp.GenericParamConstraints.Clear();
			gp.GenericParamConstraints.AddRange(this.GenericParamConstraints);
			gp.CustomAttributes.Clear();
			gp.CustomAttributes.AddRange(this.CustomAttributes);
			return gp;
		}

		public GenericParam CreateGenericParam(ModuleDef ownerModule)
		{
			return ownerModule.UpdateRowId(CopyTo(new GenericParamUser()));
		}
	}
}
