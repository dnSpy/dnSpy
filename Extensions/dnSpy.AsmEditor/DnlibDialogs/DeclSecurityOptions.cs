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
	sealed class DeclSecurityOptions {
		public SecurityAction Action;
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();
		public List<SecurityAttribute> SecurityAttributes = new List<SecurityAttribute>();
		public string? V1XMLString;

		public DeclSecurityOptions() {
		}

		public DeclSecurityOptions(DeclSecurity ds) {
			Action = ds.Action;
			CustomAttributes.AddRange(ds.CustomAttributes);
			V1XMLString = ds.GetNet1xXmlString();
			if (V1XMLString is null)
				SecurityAttributes.AddRange(ds.SecurityAttributes);
		}

		public DeclSecurity CopyTo(ModuleDef module, DeclSecurity ds) {
			ds.Action = Action;
			ds.CustomAttributes.Clear();
			ds.CustomAttributes.AddRange(CustomAttributes);
			ds.SecurityAttributes.Clear();
			if (V1XMLString is null)
				ds.SecurityAttributes.AddRange(SecurityAttributes);
			else
				ds.SecurityAttributes.Add(SecurityAttribute.CreateFromXml(module, V1XMLString));
			return ds;
		}

		public DeclSecurity Create(ModuleDef module) => module.UpdateRowId(CopyTo(module, new DeclSecurityUser()));
	}
}
