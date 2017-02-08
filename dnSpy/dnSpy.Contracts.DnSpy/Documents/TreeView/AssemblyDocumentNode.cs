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

using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// A .NET assembly file
	/// </summary>
	public abstract class AssemblyDocumentNode : DsDocumentNode, IMDTokenNode {
		/// <summary>
		/// Gets the <see cref="IDsDocument"/> instance
		/// </summary>
		public new IDsDotNetDocument Document => (IDsDotNetDocument)base.Document;

		/// <summary>
		/// true if it's an .exe file, false if it's a .dll or .netmodule
		/// </summary>
		public bool IsExe => (Document.ModuleDef.Characteristics & Characteristics.Dll) == 0;

		IMDTokenProvider IMDTokenNode.Reference => Document.AssemblyDef;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="document">Document</param>
		protected AssemblyDocumentNode(IDsDotNetDocument document)
			: base(document) {
		}
	}
}
