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

using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// A .NET module file
	/// </summary>
	public abstract class ModuleDocumentNode : DsDocumentNode, IMDTokenNode {
		/// <summary>
		/// Gets the <see cref="IDsDocument"/> instance
		/// </summary>
		public new IDsDotNetDocument Document => (IDsDotNetDocument)base.Document;

		IMDTokenProvider? IMDTokenNode.Reference => Document.ModuleDef;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="document">Document</param>
		protected ModuleDocumentNode(IDsDotNetDocument document)
			: base(document) => Debug2.Assert(!(document.ModuleDef is null));

		/// <summary>
		/// Creates a <see cref="NamespaceNode"/>
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public NamespaceNode Create(string name) => Context.DocumentTreeView.Create(name);

		/// <summary>
		/// Returns an existing <see cref="NamespaceNode"/> instance or null
		/// </summary>
		/// <param name="ns">Namespace</param>
		/// <returns></returns>
		public abstract NamespaceNode? FindNode(string? ns);
	}
}
