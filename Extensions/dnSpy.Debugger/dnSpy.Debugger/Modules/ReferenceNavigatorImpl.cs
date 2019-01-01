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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.References;
using dnSpy.Contracts.Documents;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.Modules {
	interface IModuleLoader {
		void LoadModules(DbgModule[] modules, DbgLoadModuleReferenceHandlerOptions options);
	}

	[ExportReferenceNavigator]
	[Export(typeof(IModuleLoader))]
	sealed class ReferenceNavigatorImpl : ReferenceNavigator, IModuleLoader {
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<DbgLoadModuleReferenceHandler, IDbgLoadModuleReferenceHandlerMetadata>[] dbgLoadModuleReferenceHandlers;

		[ImportingConstructor]
		ReferenceNavigatorImpl(UIDispatcher uiDispatcher, [ImportMany] IEnumerable<Lazy<DbgLoadModuleReferenceHandler, IDbgLoadModuleReferenceHandlerMetadata>> dbgLoadModuleReferenceHandlers) {
			this.uiDispatcher = uiDispatcher;
			this.dbgLoadModuleReferenceHandlers = dbgLoadModuleReferenceHandlers.OrderBy(a => a.Metadata.Order).ToArray();
		}

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

		void IModuleLoader.LoadModules(DbgModule[] modules, DbgLoadModuleReferenceHandlerOptions options) {
			uiDispatcher.VerifyAccess();
			if (modules == null)
				throw new ArgumentNullException(nameof(modules));
			var hash = new HashSet<DbgModule>(modules);
			foreach (var lz in dbgLoadModuleReferenceHandlers) {
				if (hash.Count == 0)
					break;
				var loaded = lz.Value.Load(hash.ToArray(), options);
				foreach (var module in loaded)
					hash.Remove(module);
			}
			Debug.Assert(hash.Count == 0);
		}
	}
}
