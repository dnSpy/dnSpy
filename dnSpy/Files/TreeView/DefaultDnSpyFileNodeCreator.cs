/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.Files.TreeView {
	[ExportDnSpyFileNodeCreator(Order = double.MaxValue)]
	sealed class DefaultDnSpyFileNodeCreator : IDnSpyFileNodeCreator {
		public IDnSpyFileNode Create(IFileTreeView fileTreeView, IDnSpyFileNode owner, IDnSpyFile file) {
			var dnFile = file as IDnSpyDotNetFile;
			if (dnFile != null) {
				Debug.Assert(file.ModuleDef != null);
				if (file.AssemblyDef == null || owner != null)
					return new ModuleFileNode(dnFile);
				return new AssemblyFileNode(dnFile);
			}
			Debug.Assert(file.AssemblyDef == null && file.ModuleDef == null);
			if (file.PEImage != null)
				return new PEFileNode(file);

			return null;
		}
	}
}
