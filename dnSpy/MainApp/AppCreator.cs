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
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using dnSpy.Contracts.Settings;
using dnSpy.Settings;

namespace dnSpy.MainApp {
	static class AppCreator {
		public static AppImpl Create(IEnumerable<Assembly> asms, string pattern) {
			var container = InitializeCompositionContainer(asms, pattern);
			ReadSettings(container.GetExportedValue<ISettingsManager>());
			var appImpl = container.GetExportedValue<AppImpl>();
			appImpl.CompositionContainer = container;
			return appImpl;
		}

		static void ReadSettings(ISettingsManager settingsManager) {
			try {
				new XmlSettingsReader(settingsManager).Read();
			}
			catch {
				//TODO: Show error to user
			}
		}

		static CompositionContainer InitializeCompositionContainer(IEnumerable<Assembly> asms, string pattern) {
			var aggregateCatalog = new AggregateCatalog();
			var ourAsm = typeof(AppCreator).Assembly;
			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(ourAsm));
			foreach (var asm in asms) {
				if (ourAsm != asm)
					aggregateCatalog.Catalogs.Add(new AssemblyCatalog(asm));
			}
			AddFiles(aggregateCatalog, pattern);
			return new CompositionContainer(aggregateCatalog);
		}

		static void AddFiles(AggregateCatalog aggregateCatalog, string pattern) {
			var dir = Path.GetDirectoryName(typeof(AppCreator).Assembly.Location);
			var random = new Random();
			var files = Directory.GetFiles(dir, pattern).OrderBy(a => random.Next()).ToArray();
			foreach (var file in files) {
				try {
					aggregateCatalog.Catalogs.Add(new AssemblyCatalog(Assembly.LoadFile(file)));
				}
				catch {
					Debug.Fail(string.Format("Failed to load file '{0}'", file));
				}
			}
		}
	}
}
