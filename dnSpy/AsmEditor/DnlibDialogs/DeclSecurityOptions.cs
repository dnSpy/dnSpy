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
	sealed class DeclSecurityOptions {
		public SecurityAction Action;
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();
		public List<SecurityAttribute> SecurityAttributes = new List<SecurityAttribute>();
		public string V1XMLString;

		public DeclSecurityOptions() {
		}

		public DeclSecurityOptions(DeclSecurity ds) {
			this.Action = ds.Action;
			this.CustomAttributes.AddRange(ds.CustomAttributes);
			this.V1XMLString = ds.GetNet1xXmlString();
			if (this.V1XMLString == null)
				this.SecurityAttributes.AddRange(ds.SecurityAttributes);
		}

		public DeclSecurity CopyTo(ModuleDef module, DeclSecurity ds) {
			ds.Action = this.Action;
			ds.CustomAttributes.Clear();
			ds.CustomAttributes.AddRange(CustomAttributes);
			ds.SecurityAttributes.Clear();
			if (this.V1XMLString == null)
				ds.SecurityAttributes.AddRange(SecurityAttributes);
			else
				ds.SecurityAttributes.Add(SecurityAttribute.CreateFromXml(module, this.V1XMLString));
			return ds;
		}

		public DeclSecurity Create(ModuleDef module) {
			return module.UpdateRowId(CopyTo(module, new DeclSecurityUser()));
		}
	}
}
