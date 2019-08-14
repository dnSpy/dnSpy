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
using dnlib.PE;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// A PE file (but not a .NET file)
	/// </summary>
	public abstract class PEDocumentNode : DsDocumentNode {
		/// <summary>
		/// true if it's an .exe file, false if it's a .dll file
		/// </summary>
		public bool IsExe => (Document.PEImage!.ImageNTHeaders.FileHeader.Characteristics & Characteristics.Dll) == 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="document">Document</param>
		protected PEDocumentNode(IDsDocument document)
			: base(document) => Debug2.Assert(!(document.PEImage is null) && document.ModuleDef is null);
	}
}
