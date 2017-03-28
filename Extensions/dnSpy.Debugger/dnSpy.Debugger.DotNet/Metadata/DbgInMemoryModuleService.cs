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

using System;
using System.ComponentModel.Composition;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Documents;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Loads modules that only exist in memory
	/// </summary>
	abstract class DbgInMemoryModuleService {
		public abstract ModuleDef LoadModule(DbgModule module);
		public abstract ModuleDef FindModule(DbgModule module);
	}

	[Export(typeof(DbgInMemoryModuleService))]
	sealed class DbgInMemoryModuleServiceImpl : DbgInMemoryModuleService {
		readonly IDsDocumentService documentService;

		[ImportingConstructor]
		DbgInMemoryModuleServiceImpl(IDsDocumentService documentService) => this.documentService = documentService;

		public override ModuleDef LoadModule(DbgModule module) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			return null;//TODO:
		}

		public override ModuleDef FindModule(DbgModule module) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			return null;//TODO:
		}
	}
}
