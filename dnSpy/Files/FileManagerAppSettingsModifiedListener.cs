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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Settings.Dialog;

namespace dnSpy.Files {
	[ExportAppSettingsModifiedListener(Order = AppSettingsConstants.ORDER_SETTINGS_LISTENER_FILEMANAGER)]
	sealed class FileManagerAppSettingsModifiedListener : IAppSettingsModifiedListener {
		readonly IFileManager fileManager;

		[ImportingConstructor]
		FileManagerAppSettingsModifiedListener(IFileManager fileManager) {
			this.fileManager = fileManager;
		}

		public void OnSettingsModified(IAppRefreshSettings appRefreshSettings) {
			if (appRefreshSettings.Has(AppSettingsConstants.DISABLE_MMAP))
				DisableMemoryMappedIO();
		}

		IEnumerable<ModuleDefMD> GetModules(HashSet<ModuleDefMD> hash, IEnumerable<IDnSpyFile> files) {
			foreach (var f in files) {
				var mod = f.ModuleDef as ModuleDefMD;
				if (mod != null && !hash.Contains(mod)) {
					hash.Add(mod);
					yield return mod;
				}
				var asm = mod.Assembly;
				foreach (var m in asm.Modules) {
					mod = m as ModuleDefMD;
					if (mod != null && !hash.Contains(mod)) {
						hash.Add(mod);
						yield return mod;
					}
				}
				foreach (var m in GetModules(hash, f.Children))
					yield return m;
			}
		}

		void DisableMemoryMappedIO() {
			var hash = new HashSet<ModuleDefMD>();
			foreach (var m in GetModules(hash, fileManager.GetFiles()))
				m.MetaData.PEImage.UnsafeDisableMemoryMappedIO();
		}
	}
}
