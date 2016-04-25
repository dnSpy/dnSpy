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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using dnSpy.Contracts.Plugin;

namespace dnSpy.Plugin {
	interface IPluginManager {
		IEnumerable<IPlugin> Plugins { get; }
		IEnumerable<LoadedPlugin> LoadedPlugins { get; }
	}

	[Export, Export(typeof(IPluginManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class PluginManager : IPluginManager {
		readonly Lazy<IAutoLoaded, IAutoLoadedMetadata>[] mefAutoLoaded;
		readonly Lazy<IPlugin, IPluginMetadata>[] mefPlugins;

		public IEnumerable<IPlugin> Plugins {
			get { return mefPlugins.Select(a => a.Value); }
		}

		public IEnumerable<LoadedPlugin> LoadedPlugins {
			get {
				Debug.Assert(loadedPlugins != null, "Called too early");
				return (loadedPlugins ?? new LoadedPlugin[0]).AsEnumerable();
			}
			internal set {
				Debug.Assert(loadedPlugins == null);
				if (loadedPlugins != null)
					throw new InvalidOperationException();
				loadedPlugins = value.ToArray();
			}
		}
		LoadedPlugin[] loadedPlugins = null;

		[ImportingConstructor]
		PluginManager([ImportMany] IEnumerable<Lazy<IAutoLoaded, IAutoLoadedMetadata>> mefAutoLoaded, [ImportMany] IEnumerable<Lazy<IPlugin, IPluginMetadata>> mefPlugins) {
			this.mefAutoLoaded = mefAutoLoaded.OrderBy(a => a.Metadata.Order).ToArray();
			this.mefPlugins = mefPlugins.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public void LoadPlugins(Collection<ResourceDictionary> mergedDictionaries) {
			LoadAutoLoaded(AutoLoadedLoadType.BeforePlugins);
			foreach (var m in mefPlugins) {
				var plugin = m.Value;
				foreach (var rsrc in plugin.MergedResourceDictionaries) {
					var asm = plugin.GetType().Assembly.GetName();
					var uri = new Uri("pack://application:,,,/" + asm.Name + ";v" + asm.Version + ";component/" + rsrc, UriKind.Absolute);
					mergedDictionaries.Add(new ResourceDictionary { Source = uri });
				}
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

		public void OnAppLoaded() {
			NotifyPlugins(PluginEvent.AppLoaded, null);
			LoadAutoLoaded(AutoLoadedLoadType.AppLoaded);
		}

		public void OnAppExit() {
			NotifyPlugins(PluginEvent.AppExit, null);
		}
	}
}
