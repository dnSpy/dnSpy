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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Plugin;

namespace dnSpy.Plugin {
	[Export, PartCreationPolicy(CreationPolicy.Shared)]
	sealed class PluginManager {
		readonly Lazy<IAutoLoaded, IAutoLoadedMetadata>[] mefAutoLoaded;
		readonly Lazy<IPlugin, IPluginMetadata>[] mefPlugins;

		[ImportingConstructor]
		PluginManager([ImportMany] IEnumerable<Lazy<IAutoLoaded, IAutoLoadedMetadata>> mefAutoLoaded, [ImportMany] IEnumerable<Lazy<IPlugin, IPluginMetadata>> mefPlugins) {
			this.mefAutoLoaded = mefAutoLoaded.OrderBy(a => a.Metadata.Order).ToArray();
			this.mefPlugins = mefPlugins.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public void LoadPlugins() {
			LoadAutoLoaded(AutoLoadedLoadType.BeforePlugins);
			foreach (var m in mefPlugins) {
				var o = m.Value;
			}
			LoadAutoLoaded(AutoLoadedLoadType.AfterPlugins);
			NotifyPlugins(PluginEvent.Loaded, null);
			LoadAutoLoaded(AutoLoadedLoadType.AfterPluginsLoaded);
		}

		void LoadAutoLoaded(AutoLoadedLoadType loadType) {
			foreach (var m in mefAutoLoaded.Where(a => a.Metadata.LoadType == loadType)) {
				var o = m.Value;
			}
		}

		void NotifyPlugins(PluginEvent @event, object obj) {
			foreach (var m in mefPlugins)
				m.Value.OnEvent(@event, obj);
		}
	}
}
