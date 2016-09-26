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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Tabs;
using dnSpy.Decompiler.MSBuild;
using dnSpy.Documents.Tabs.Dialogs;
using dnSpy.Properties;

namespace dnSpy.Documents.Tabs {
	[ExportAutoLoaded]
	sealed class SaveCommandInit : IAutoLoaded {
		[ImportingConstructor]
		SaveCommandInit(ISaveService saveService, IAppWindow appWindow, IDocumentTabService documentTabService) {
			appWindow.MainWindowCommands.Add(ApplicationCommands.Save, (s, e) => saveService.Save(documentTabService.ActiveTab), (s, e) => e.CanExecute = saveService.CanSave(documentTabService.ActiveTab));
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Header = "res:ExportToProjectCommand", Icon = "Solution", Group = MenuConstants.GROUP_APP_MENU_FILE_SAVE, Order = 0)]
	sealed class ExportProjectCommand : MenuItemBase {
		readonly IAppWindow appWindow;
		readonly IDocumentTreeView documentTreeView;
		readonly IDecompilerService decompilerService;
		readonly IDocumentTreeViewSettings documentTreeViewSettings;
		readonly IExportToProjectSettings exportToProjectSettings;
		readonly Lazy<IBamlDecompiler> bamlDecompiler;

		[ImportingConstructor]
		ExportProjectCommand(IAppWindow appWindow, IDocumentTreeView documentTreeView, IDecompilerService decompilerService, IDocumentTreeViewSettings documentTreeViewSettings, IExportToProjectSettings exportToProjectSettings, [ImportMany] IEnumerable<Lazy<IBamlDecompiler>> bamlDecompilers) {
			this.appWindow = appWindow;
			this.documentTreeView = documentTreeView;
			this.decompilerService = decompilerService;
			this.documentTreeViewSettings = documentTreeViewSettings;
			this.exportToProjectSettings = exportToProjectSettings;
			this.bamlDecompiler = bamlDecompilers.FirstOrDefault();
		}

		public override bool IsEnabled(IMenuItemContext context) =>
			GetModules().Length > 0 && decompilerService.AllDecompilers.Any(a => a.ProjectFileExtension != null);

		public override void Execute(IMenuItemContext context) {
			var modules = GetModules();
			if (modules.Length == 0)
				return;

			var decompiler = decompilerService.Decompiler;
			if (decompiler.ProjectFileExtension == null) {
				decompiler = decompilerService.AllDecompilers.FirstOrDefault(a => a.ProjectFileExtension != null);
				Debug.Assert(decompiler != null);
				if (decompiler == null)
					return;
			}

			var task = new ExportTask(this, modules);
			var vm = new ExportToProjectVM(new PickDirectory(), decompilerService, task, bamlDecompiler != null);
			task.vm = vm;
			vm.ProjectVersion = exportToProjectSettings.ProjectVersion;
			vm.CreateResX = documentTreeViewSettings.DeserializeResources;
			vm.DontReferenceStdLib = modules.Any(a => a.Assembly.IsCorLib());
			vm.Decompiler = decompiler;
			vm.SolutionFilename = GetSolutionFilename(modules);
			vm.FilesToExportMessage = CreateFilesToExportMessage(modules);

			var win = new ExportToProjectDlg();
			task.dlg = win;
			win.DataContext = vm;
			win.Owner = appWindow.MainWindow;
			using (documentTreeView.DocumentService.DisableAssemblyLoad())
				win.ShowDialog();
			if (vm.IsComplete)
				exportToProjectSettings.ProjectVersion = vm.ProjectVersion;
			task.Dispose();
		}

		sealed class ExportTask : IExportTask, IMSBuildProjectWriterLogger, IMSBuildProgressListener {
			readonly ExportProjectCommand owner;
			readonly ModuleDef[] modules;
			readonly CancellationTokenSource cancellationTokenSource;
			readonly CancellationToken cancellationToken;
			readonly Dispatcher dispatcher;
			readonly IBamlDecompiler bamlDecompiler;

			internal ExportToProjectDlg dlg;
			internal ExportToProjectVM vm;

			public ExportTask(ExportProjectCommand owner, ModuleDef[] modules) {
				this.owner = owner;
				this.modules = modules;
				this.cancellationTokenSource = new CancellationTokenSource();
				this.cancellationToken = cancellationTokenSource.Token;
				this.dispatcher = Dispatcher.CurrentDispatcher;
				if (owner.bamlDecompiler != null)
					this.bamlDecompiler = owner.bamlDecompiler.Value;
			}

			public void Dispose() {
				if (disposed)
					return;
				disposed = true;
				cancellationTokenSource.Cancel();
				cancellationTokenSource.Dispose();
			}
			bool disposed;

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
						CancellationToken = cancellationToken,
						GetDisableAssemblyLoad = () => owner.documentTreeView.DocumentService.DisableAssemblyLoad(),
					};
					var options = new ProjectCreatorOptions(vm.Directory, cancellationToken);
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
						var projOpts = new ProjectModuleOptions(module, vm.Decompiler, decompilationContext) {
							DontReferenceStdLib = vm.DontReferenceStdLib,
							UnpackResources = vm.UnpackResources,
							CreateResX = vm.CreateResX,
							DecompileXaml = vm.DecompileXaml,
							ProjectGuid = hasProjectGuid ? new Guid(string.Format(guidFormat, guidNum++)) : Guid.NewGuid(),
						};
						if (bamlDecompiler != null) {
							var o = BamlDecompilerOptions.Create(vm.Decompiler);
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
				}, cancellationToken)
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
			foreach (var n in documentTreeView.TreeView.TopLevelSelection) {
				var asmNode = n.GetAssemblyNode();
				if (asmNode != null) {
					asmNode.TreeNode.EnsureChildrenLoaded();
					foreach (var c in asmNode.TreeNode.DataChildren.OfType<IModuleDocumentNode>())
						hashSet.Add(c.Document.ModuleDef);
					continue;
				}

				var modNode = n.GetModuleNode();
				if (modNode != null)
					hashSet.Add(modNode.Document.ModuleDef);
			}
			hashSet.Remove(null);
			return hashSet.ToArray();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, InputGestureText = "res:SaveKey", Icon = "Save", Group = MenuConstants.GROUP_APP_MENU_FILE_SAVE, Order = 10)]
	sealed class MenuSaveCommand : MenuItemCommand {
		readonly ISaveService saveService;
		readonly IDocumentTabService documentTabService;

		[ImportingConstructor]
		MenuSaveCommand(ISaveService saveService, IDocumentTabService documentTabService)
			: base(ApplicationCommands.Save) {
			this.saveService = saveService;
			this.documentTabService = documentTabService;
		}

		public override string GetHeader(IMenuItemContext context) => saveService.GetMenuHeader(documentTabService.ActiveTab);
	}

	[ExportMenuItem(InputGestureText = "res:SaveKey", Icon = "Save", Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 0)]
	sealed class SaveTabCtxMenuCommand : MenuItemCommand {
		readonly ISaveService saveService;
		readonly IDocumentTabService documentTabService;

		[ImportingConstructor]
		SaveTabCtxMenuCommand(ISaveService saveService, IDocumentTabService documentTabService)
			: base(ApplicationCommands.Save) {
			this.saveService = saveService;
			this.documentTabService = documentTabService;
		}

		public override bool IsVisible(IMenuItemContext context) => GetTabGroup(context) != null;
		public override string GetHeader(IMenuItemContext context) => saveService.GetMenuHeader(GetDocumentTab(context));

		ITabGroup GetTabGroup(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TABCONTROL_GUID))
				return null;
			var g = context.Find<ITabGroup>();
			return g != null && documentTabService.Owns(g) ? g : null;
		}

		IDocumentTab GetDocumentTab(IMenuItemContext context) {
			var g = GetTabGroup(context);
			return g == null ? null : documentTabService.TryGetDocumentTab(g.ActiveTabContent);
		}
	}
}
