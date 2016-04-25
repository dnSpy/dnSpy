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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Tabs;
using dnSpy.Files.Tabs.Dialogs;
using dnSpy.Languages.MSBuild;
using dnSpy.Properties;
using dnSpy.Shared.Menus;
using dnSpy.Shared.MVVM;

namespace dnSpy.Files.Tabs {
	[ExportAutoLoaded]
	sealed class SaveCommandInit : IAutoLoaded {
		[ImportingConstructor]
		SaveCommandInit(ISaveManager saveManager, IAppWindow appWindow, IFileTabManager fileTabManager) {
			appWindow.MainWindowCommands.Add(ApplicationCommands.Save, (s, e) => saveManager.Save(fileTabManager.ActiveTab), (s, e) => e.CanExecute = saveManager.CanSave(fileTabManager.ActiveTab));
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Header = "res:ExportToProjectCommand", Icon = "Solution", Group = MenuConstants.GROUP_APP_MENU_FILE_SAVE, Order = 0)]
	sealed class ExportProjectCommand : MenuItemBase {
		readonly IAppWindow appWindow;
		readonly IFileTreeView fileTreeView;
		readonly ILanguageManager languageManager;
		readonly IFileTreeViewSettings fileTreeViewSettings;
		readonly IExportToProjectSettings exportToProjectSettings;
		readonly Lazy<IBamlDecompiler> bamlDecompiler;

		[ImportingConstructor]
		ExportProjectCommand(IAppWindow appWindow, IFileTreeView fileTreeView, ILanguageManager languageManager, IFileTreeViewSettings fileTreeViewSettings, IExportToProjectSettings exportToProjectSettings, [ImportMany] IEnumerable<Lazy<IBamlDecompiler>> bamlDecompilers) {
			this.appWindow = appWindow;
			this.fileTreeView = fileTreeView;
			this.languageManager = languageManager;
			this.fileTreeViewSettings = fileTreeViewSettings;
			this.exportToProjectSettings = exportToProjectSettings;
			this.bamlDecompiler = bamlDecompilers.FirstOrDefault();
		}

		public override bool IsEnabled(IMenuItemContext context) {
			return GetModules().Length > 0 && languageManager.AllLanguages.Any(a => a.ProjectFileExtension != null);
		}

		public override void Execute(IMenuItemContext context) {
			var modules = GetModules();
			if (modules.Length == 0)
				return;

			var lang = languageManager.Language;
			if (lang.ProjectFileExtension == null) {
				lang = languageManager.AllLanguages.FirstOrDefault(a => a.ProjectFileExtension != null);
				Debug.Assert(lang != null);
				if (lang == null)
					return;
			}

			var task = new ExportTask(this, modules);
			var vm = new ExportToProjectVM(new PickDirectory(), languageManager, task, bamlDecompiler != null);
			task.vm = vm;
			vm.ProjectVersion = exportToProjectSettings.ProjectVersion;
			vm.CreateResX = fileTreeViewSettings.DeserializeResources;
			vm.DontReferenceStdLib = modules.Any(a => a.Assembly.IsCorLib());
			vm.Language = lang;
			vm.SolutionFilename = GetSolutionFilename(modules);
			vm.FilesToExportMessage = CreateFilesToExportMessage(modules);

			var win = new ExportToProjectDlg();
			task.dlg = win;
			win.DataContext = vm;
			win.Owner = appWindow.MainWindow;
			using (fileTreeView.FileManager.DisableAssemblyLoad())
				win.ShowDialog();
			if (vm.IsComplete)
				exportToProjectSettings.ProjectVersion = vm.ProjectVersion;
		}

		sealed class ExportTask : IExportTask, IMSBuildProjectWriterLogger, IMSBuildProgressListener {
			readonly ExportProjectCommand owner;
			readonly ModuleDef[] modules;
			readonly CancellationTokenSource cancellationTokenSource;
			readonly Dispatcher dispatcher;
			readonly IBamlDecompiler bamlDecompiler;

			internal ExportToProjectDlg dlg;
			internal ExportToProjectVM vm;

			public ExportTask(ExportProjectCommand owner, ModuleDef[] modules) {
				this.owner = owner;
				this.modules = modules;
				this.cancellationTokenSource = new CancellationTokenSource();
				this.dispatcher = Dispatcher.CurrentDispatcher;
				if (owner.bamlDecompiler != null)
					this.bamlDecompiler = owner.bamlDecompiler.Value;
			}

			public void Cancel(ExportToProjectVM vm) {
				cancellationTokenSource.Cancel();
				dlg.Close();
			}

			public void Execute(ExportToProjectVM vm) {
				vm.ProgressMinimum = 0;
				vm.ProgressMaximum = 1;
				vm.TotalProgress = 0;
				vm.IsIndeterminate = false;
				Task.Factory.StartNew(() => {
					var decompilationContext = new DecompilationContext {
						CancellationToken = cancellationTokenSource.Token,
						GetDisableAssemblyLoad = () => owner.fileTreeView.FileManager.DisableAssemblyLoad(),
					};
					var options = new ProjectCreatorOptions(vm.Directory, cancellationTokenSource.Token);
					options.ProjectVersion = vm.ProjectVersion;
					if (vm.CreateSolution)
						options.SolutionFilename = vm.SolutionFilename;
					options.Logger = this;
					options.ProgressListener = this;

					bool hasProjectGuid = vm.ProjectGuid.Value != null;
					string guidFormat = null;
					int guidNum = 0;
					if (hasProjectGuid) {
						string guidStr = vm.ProjectGuid.Value.ToString();
						guidNum = int.Parse(guidStr.Substring(36 - 8, 8), NumberStyles.HexNumber);
						guidFormat = guidStr.Substring(0, 36 - 8) + "{0:X8}";
					}
					foreach (var module in modules.OrderBy(a => a.Location, StringComparer.InvariantCultureIgnoreCase)) {
						var projOpts = new ProjectModuleOptions(module, vm.Language, decompilationContext) {
							DontReferenceStdLib = vm.DontReferenceStdLib,
							UnpackResources = vm.UnpackResources,
							CreateResX = vm.CreateResX,
							DecompileXaml = vm.DecompileXaml,
							ProjectGuid = hasProjectGuid ? new Guid(string.Format(guidFormat, guidNum++)) : Guid.NewGuid(),
						};
						if (bamlDecompiler != null) {
							var o = BamlDecompilerOptions.Create(vm.Language);
							projOpts.DecompileBaml = (a, b, c, d) => bamlDecompiler.Decompile(a, b, c, o, d);
						}
						options.ProjectModules.Add(projOpts);
					}
					var creator = new MSBuildProjectCreator(options);

					creator.Create();
					if (vm.CreateSolution)
						fileToOpen = creator.SolutionFilename;
					else
						fileToOpen = creator.ProjectFilenames.FirstOrDefault();
				}, cancellationTokenSource.Token)
				.ContinueWith(t => {
					var ex = t.Exception;
					if (ex != null)
						Error(string.Format(dnSpy_Resources.ErrorExceptionOccurred, ex));
					EmtpyErrorList();
					vm.OnExportComplete();
					if (!vm.ExportErrors) {
						dlg.Close();
						if (vm.OpenProject)
							OpenProject();
					}
				}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
			}

			void OpenProject() {
				if (!File.Exists(fileToOpen))
					return;
				try {
					Process.Start(fileToOpen);
				}
				catch {
				}
			}
			string fileToOpen;

			public void Error(string message) {
				bool start;
				lock (errorList) {
					errorList.Add(message);
					start = errorList.Count == 1;
				}
				if (start)
					dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(EmtpyErrorList));

			}
			readonly List<string> errorList = new List<string>();

			void EmtpyErrorList() {
				var list = new List<string>();
				lock (errorList) {
					list.AddRange(errorList);
					errorList.Clear();
				}
				if (list.Count > 0)
					vm.AddError(string.Join(Environment.NewLine, list.ToArray()));
			}

			void IMSBuildProgressListener.SetMaxProgress(int maxProgress) {
				dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => {
					vm.ProgressMinimum = 0;
					vm.ProgressMaximum = maxProgress;
					vm.IsIndeterminate = false;
				}));
			}

			void IMSBuildProgressListener.SetProgress(int progress) {
				bool start;
				lock (newProgressLock) {
					start = newProgress == null;
					if (newProgress == null || progress > newProgress.Value)
						newProgress = progress;
				}
				if (start) {
					dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
						int? newValue;
						lock (newProgressLock) {
							newValue = newProgress;
							newProgress = null;
						}
						Debug.Assert(newValue != null);
						if (newValue != null)
							vm.TotalProgress = newValue.Value;
					}));
				}
			}
			readonly object newProgressLock = new object();
			int? newProgress;
		}

		static string CreateFilesToExportMessage(ModuleDef[] modules) {
			if (modules.Length == 1)
				return dnSpy_Resources.ExportToProject_ExportFileMessage;
			return string.Format(dnSpy_Resources.ExportToProject_ExportNFilesMessage, modules.Length);
		}

		static string GetSolutionFilename(IEnumerable<ModuleDef> modules) {
			foreach (var e in modules.OrderBy(a => (a.Characteristics & Characteristics.Dll) == 0 ? 0 : 1)) {
				var name = e.IsManifestModule && e.Assembly != null ? GetSolutionName(e.Assembly.Name) : GetSolutionName(e.Name);
				if (!string.IsNullOrWhiteSpace(name))
					return name;
			}
			Debug.Fail("Should never be reached");
			return GetSolutionName("solution");
		}

		static string GetSolutionName(string name) {
			if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
				name = name.Substring(0, name.Length - 4);
			else if (name.EndsWith(".netmodule", StringComparison.OrdinalIgnoreCase))
				name = name.Substring(0, name.Length - 10);
			if (!string.IsNullOrWhiteSpace(name))
				return name + ".sln";
			return null;
		}

		ModuleDef[] GetModules() {
			var hashSet = new HashSet<ModuleDef>();
			foreach (var n in fileTreeView.TreeView.TopLevelSelection) {
				var asmNode = n.GetAssemblyNode();
				if (asmNode != null) {
					asmNode.TreeNode.EnsureChildrenLoaded();
					foreach (var c in asmNode.TreeNode.DataChildren.OfType<IModuleFileNode>())
						hashSet.Add(c.DnSpyFile.ModuleDef);
					continue;
				}

				var modNode = n.GetModuleNode();
				if (modNode != null)
					hashSet.Add(modNode.DnSpyFile.ModuleDef);
			}
			hashSet.Remove(null);
			return hashSet.ToArray();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, InputGestureText = "res:SaveKey", Icon = "Save", Group = MenuConstants.GROUP_APP_MENU_FILE_SAVE, Order = 10)]
	sealed class MenuSaveCommand : MenuItemCommand {
		readonly ISaveManager saveManager;
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		MenuSaveCommand(ISaveManager saveManager, IFileTabManager fileTabManager)
			: base(ApplicationCommands.Save) {
			this.saveManager = saveManager;
			this.fileTabManager = fileTabManager;
		}

		public override string GetHeader(IMenuItemContext context) {
			return saveManager.GetMenuHeader(fileTabManager.ActiveTab);
		}
	}

	[ExportMenuItem(InputGestureText = "res:SaveKey", Icon = "Save", Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 0)]
	sealed class SaveTabCtxMenuCommand : MenuItemCommand {
		readonly ISaveManager saveManager;
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		SaveTabCtxMenuCommand(ISaveManager saveManager, IFileTabManager fileTabManager)
			: base(ApplicationCommands.Save) {
			this.saveManager = saveManager;
			this.fileTabManager = fileTabManager;
		}

		public override bool IsVisible(IMenuItemContext context) {
			return GetTabGroup(context) != null;
		}

		public override string GetHeader(IMenuItemContext context) {
			return saveManager.GetMenuHeader(GetFileTab(context));
		}

		ITabGroup GetTabGroup(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_FILES_TABCONTROL_GUID))
				return null;
			var g = context.Find<ITabGroup>();
			return g != null && fileTabManager.Owns(g) ? g : null;
		}

		IFileTab GetFileTab(IMenuItemContext context) {
			var g = GetTabGroup(context);
			return g == null ? null : fileTabManager.TryGetFileTab(g.ActiveTabContent);
		}
	}
}
