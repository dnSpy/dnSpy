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

using System;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Debugger.DotNet.Exceptions {
	[ExportDbgExceptionFormatter(PredefinedExceptionCategories.DotNet)]
	sealed class CLRDbgExceptionFormatter : DbgExceptionFormatter {
		public override bool WriteName(IDbgTextWriter writer, DbgExceptionDefinition definition) {
			var fullName = definition.Id.Name;
			if (!string2.IsNullOrEmpty(fullName)) {
				var nsParts = fullName.Split(nsSeps);
				int pos = 0;
				var partColor = DbgTextColor.Namespace;
				for (int i = 0; i < nsParts.Length - 1; i++) {
					var ns = nsParts[i];
					var sep = fullName[pos + ns.Length];
					if (sep == '+')
						partColor = DbgTextColor.Type;
					writer.Write(partColor, ns);
					writer.Write(DbgTextColor.Operator, sep.ToString());
					pos += ns.Length + 1;
				}
				writer.Write(DbgTextColor.Type, nsParts[nsParts.Length - 1]);
			}
			return true;
		}
		static readonly char[] nsSeps = new char[] { '.', '+' };
	}
}
