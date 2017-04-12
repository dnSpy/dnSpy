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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger.References;
using dnSpy.Contracts.Documents;

namespace dnSpy.Debugger.Modules {
	[ExportReferenceNavigator]
	sealed class ReferenceNavigatorImpl : ReferenceNavigator {
		readonly Lazy<DbgLoadModuleReferenceHandler, IDbgLoadModuleReferenceHandlerMetadata>[] dbgLoadModuleReferenceHandlers;

		[ImportingConstructor]
		ReferenceNavigatorImpl([ImportMany] IEnumerable<Lazy<DbgLoadModuleReferenceHandler, IDbgLoadModuleReferenceHandlerMetadata>> dbgLoadModuleReferenceHandlers) =>
			this.dbgLoadModuleReferenceHandlers = dbgLoadModuleReferenceHandlers.OrderBy(a => a.Metadata.Order).ToArray();

		public override bool GoTo(object reference, ReadOnlyCollection<object> options) {
			if (reference is DbgLoadModuleReference moduleRef) {
				GoTo(moduleRef, options);
				return true;
			}

			return false;
		}

		void GoTo(DbgLoadModuleReference moduleRef, ReadOnlyCollection<object> options) {
			foreach (var lz in dbgLoadModuleReferenceHandlers) {
				if (lz.Value.GoTo(moduleRef, options))
					return;
			}
			Debug.Fail($"No handler for module {moduleRef.Module.Name}");
		}
	}
}
