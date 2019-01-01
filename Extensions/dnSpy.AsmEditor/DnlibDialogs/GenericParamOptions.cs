/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class GenericParamOptions {
		public ushort Number;
		public GenericParamAttributes Flags;
		public UTF8String Name;
		public ITypeDefOrRef Kind;
		public List<GenericParamConstraint> GenericParamConstraints = new List<GenericParamConstraint>();
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();

		public GenericParamOptions() {
		}

		public GenericParamOptions(GenericParam gp) {
			Number = gp.Number;
			Flags = gp.Flags;
			Name = gp.Name;
			Kind = gp.Kind;
			GenericParamConstraints.AddRange(gp.GenericParamConstraints);
			CustomAttributes.AddRange(gp.CustomAttributes);
		}

		public GenericParam CopyTo(GenericParam gp) {
			gp.Number = Number;
			gp.Flags = Flags;
			gp.Name = Name ?? UTF8String.Empty;
			gp.Kind = Kind;
			gp.GenericParamConstraints.Clear();
			gp.GenericParamConstraints.AddRange(GenericParamConstraints);
			gp.CustomAttributes.Clear();
			gp.CustomAttributes.AddRange(CustomAttributes);
			return gp;
		}

		public GenericParam Create(ModuleDef ownerModule) => ownerModule.UpdateRowId(CopyTo(new GenericParamUser()));
	}
}
