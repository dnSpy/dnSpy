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
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class TypeDefOrRefAndCAOptions {
		public ITypeDefOrRef TypeDefOrRef;
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();

		public TypeDefOrRefAndCAOptions() {
		}

		public TypeDefOrRefAndCAOptions(GenericParamConstraint gpc) {
			this.TypeDefOrRef = gpc.Constraint;
			this.CustomAttributes.AddRange(gpc.CustomAttributes);
		}

		public TypeDefOrRefAndCAOptions(InterfaceImpl iface) {
			this.TypeDefOrRef = iface.Interface;
			this.CustomAttributes.AddRange(iface.CustomAttributes);
		}

		public GenericParamConstraint CopyTo(GenericParamConstraint gpc) {
			gpc.Constraint = this.TypeDefOrRef;
			gpc.CustomAttributes.Clear();
			gpc.CustomAttributes.AddRange(CustomAttributes);
			return gpc;
		}

		public InterfaceImpl CopyTo(InterfaceImpl iface) {
			iface.Interface = this.TypeDefOrRef;
			iface.CustomAttributes.Clear();
			iface.CustomAttributes.AddRange(CustomAttributes);
			return iface;
		}

		public GenericParamConstraint CreateGenericParamConstraint(ModuleDef ownerModule) {
			return ownerModule.UpdateRowId(CopyTo(new GenericParamConstraintUser()));
		}

		public InterfaceImpl CreateInterfaceImpl(ModuleDef ownerModule) {
			return ownerModule.UpdateRowId(CopyTo(new InterfaceImplUser()));
		}
	}
}
