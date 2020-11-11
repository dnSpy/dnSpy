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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.MVVM.Dialogs;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.Modules {
	[Export(typeof(ModulesSaver))]
	sealed class ModulesSaver {
		readonly IAppWindow appWindow;
		readonly IMessageBoxService messageBoxService;
		readonly IPickSaveFilename pickSaveFilename;

		[ImportingConstructor]
		ModulesSaver(IAppWindow appWindow, IMessageBoxService messageBoxService, IPickSaveFilename pickSaveFilename) {
			this.appWindow = appWindow;
			this.messageBoxService = messageBoxService;
			this.pickSaveFilename = pickSaveFilename;
		}

		public void Save(ModuleVM[] modules) {
			if (modules is null)
				throw new ArgumentNullException(nameof(modules));
			var list = new(DbgModule module, string filename)[modules.Length];
			if (modules.Length == 1) {
				var vm = modules[0];
				var filename = new PickSaveFilename().GetFilename(GetModuleFilename(vm.Module), GetDefaultExtension(GetModuleFilename(vm.Module), vm.Module.IsExe), PickFilenameConstants.DotNetAssemblyOrModuleFilter);
				if (string2.IsNullOrEmpty(filename))
					return;
				list[0] = (vm.Module, filename);
			}
			else {
				var dir = new PickDirectory().GetDirectory(null);
				if (!Directory.Exists(dir))
					return;
				Debug2.Assert(dir is not null);
				for (int i = 0; i < modules.Length; i++) {
					var file = modules[i];
					var filename = file.Module.Name;
					const StringComparison comp = StringComparison.OrdinalIgnoreCase;
					if (!(filename.EndsWith(".exe", comp) || filename.EndsWith(".dll", comp) || filename.EndsWith(".netmodule", comp)))
						filename += file.Module.IsExe ? ".exe" : ".dll";
					list[i] = (file.Module, Path.Combine(dir, filename));
				}
			}

			if (!ShowDialog(list, out var error))
				messageBoxService.Show(string.Format(dnSpy_Debugger_Resources.ErrorOccurredX, error));
		}

		bool ShowDialog((DbgModule module, string filename)[] list, [NotNullWhen(false)] out string? error) {
			error = null;
			var data = new ProgressVM(Dispatcher.CurrentDispatcher, new PEFilesSaver(list));
			var win = new ProgressDlg();
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			win.Title = list.Length == 1 ?
				dnSpy_Debugger_Resources.ModuleSaveModuleTitle :
				dnSpy_Debugger_Resources.ModuleSaveModulesTitle;
			var res = win.ShowDialog();
			if (res != true)
				return true;
			if (!data.WasError)
				return true;
			Debug2.Assert(data.ErrorMessage is not null);
			error = data.ErrorMessage;
			return false;
		}

		static string? GetModuleFilename(DbgModule module) {
			if (module.IsDynamic)
				return null;
			return module.Name;
		}

		static string GetDefaultExtension(string? name, bool isExe) {
			try {
				var ext = Path.GetExtension(name);
				if (ext is not null && ext.Length > 0 && ext[0] == '.')
					return ext.Substring(1);
			}
			catch {
			}
			return isExe ? "exe" : "dll";
		}

		public ModuleVM[] FilterModules(IEnumerable<ModuleVM> modules) =>
			modules.Where(a => !a.Module.IsDynamic && a.Module.HasAddress && a.Module.ImageLayout != DbgImageLayout.Unknown).ToArray();
	}
}
