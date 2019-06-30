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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Converts <see cref="DbgModule"/>s to/from <see cref="ModuleId"/>s
	/// </summary>
	abstract class DbgModuleIdProviderService {
		public abstract ModuleId? GetModuleId(DbgModule module);
		public abstract DbgModule? GetModule(ModuleId moduleId);
	}

	[Export(typeof(DbgModuleIdProviderService))]
	sealed class DbgModuleIdProviderServiceImpl : DbgModuleIdProviderService {
		readonly DbgManager dbgManager;
		readonly Lazy<DbgModuleIdProvider>[] dbgModuleIdProviders;

		[ImportingConstructor]
		DbgModuleIdProviderServiceImpl(DbgManager dbgManager, [ImportMany] IEnumerable<Lazy<DbgModuleIdProvider>> dbgModuleIdProviders) {
			this.dbgManager = dbgManager;
			this.dbgModuleIdProviders = dbgModuleIdProviders.ToArray();
		}

		sealed class CachedModuleId {
			public ModuleId? Id { get; }
			public CachedModuleId(ModuleId? id) => Id = id;
		}

		public override ModuleId? GetModuleId(DbgModule module) {
			if (module is null)
				throw new ArgumentNullException(nameof(module));
			// Don't cache dynamic modules. The reason is that their ModuleIds could change,
			// see CorDebug's DbgEngineImpl.UpdateDynamicModuleIds()
			if (module.IsDynamic)
				return GetModuleIdCore(module);
			return module.GetOrCreateData(() => new CachedModuleId(GetModuleIdCore(module))).Id;
		}

		ModuleId? GetModuleIdCore(DbgModule module) {
			foreach (var lz in dbgModuleIdProviders) {
				var id = lz.Value.GetModuleId(module);
				if (!(id is null))
					return id;
			}
			return null;
		}

		public override DbgModule? GetModule(ModuleId moduleId) {
			foreach (var p in dbgManager.Processes) {
				foreach (var r in p.Runtimes) {
					foreach (var m in r.Modules) {
						if (GetModuleId(m) == moduleId)
							return m;
					}
				}
			}
			return null;
		}
	}
}
