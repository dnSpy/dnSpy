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

using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.Files.TreeView {
	[Export(typeof(IDnSpyFileNodeCreator)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class DefaultDnSpyFileNodeCreator : IDnSpyFileNodeCreator {
		public double Order {
			get { return double.MaxValue; }
		}

		public IDnSpyFileNode Create(IFileTreeView fileTreeView, IDnSpyFileNode owner, IDnSpyFile file) {
			if (file.ModuleDef != null) {
				if (file.AssemblyDef == null || owner != null)
					return new ModuleFileNode(file);
				return new AssemblyFileNode(file);
			}
			Debug.Assert(file.AssemblyDef == null);
			if (file.PEImage != null)
				return new PEFileNode(file);

			return null;
		}
	}
}
