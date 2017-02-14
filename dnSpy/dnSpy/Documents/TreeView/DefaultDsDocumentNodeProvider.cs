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

using System.Diagnostics;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.Documents.TreeView {
	[ExportDsDocumentNodeProvider(Order = double.MaxValue)]
	sealed class DefaultDsDocumentNodeProvider : IDsDocumentNodeProvider {
		public DsDocumentNode Create(IDocumentTreeView documentTreeView, DsDocumentNode owner, IDsDocument document) {
			if (document is IDsDotNetDocument dnDocument) {
				Debug.Assert(document.ModuleDef != null);
				if (document.AssemblyDef == null || owner != null)
					return new ModuleDocumentNodeImpl(dnDocument);
				return new AssemblyDocumentNodeImpl(dnDocument);
			}
			Debug.Assert(document.AssemblyDef == null && document.ModuleDef == null);
			if (document.PEImage != null)
				return new PEDocumentNodeImpl(document);

			return null;
		}
	}
}
