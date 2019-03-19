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
using System.IO;
using System.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.StartDebugging;
using dnSpy.Contracts.Debugger.StartDebugging.Dialog;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Debugger.Dialogs.DebugProgram;

namespace dnSpy.Debugger.DbgUI {
	[Export(typeof(StartDebuggingOptionsProvider))]
	sealed class StartDebuggingOptionsProvider {
		readonly IAppWindow appWindow;
		readonly IDocumentTabService documentTabService;
		readonly Lazy<StartDebuggingOptionsPageProvider>[] startDebuggingOptionsPageProviders;
		readonly Lazy<DbgProcessStarterService> dbgProcessStarterService;
		readonly Lazy<GenericDebugEngineGuidProvider, IGenericDebugEngineGuidProviderMetadata>[] genericDebugEngineGuidProviders;
		readonly StartDebuggingOptionsMru mru;

		[ImportingConstructor]
		StartDebuggingOptionsProvider(IAppWindow appWindow, IDocumentTabService documentTabService, Lazy<DbgProcessStarterService> dbgProcessStarterService, [ImportMany] IEnumerable<Lazy<StartDebuggingOptionsPageProvider>> startDebuggingOptionsPageProviders, [ImportMany] IEnumerable<Lazy<GenericDebugEngineGuidProvider, IGenericDebugEngineGuidProviderMetadata>> genericDebugEngineGuidProviders) {
			this.appWindow = appWindow;
			this.documentTabService = documentTabService;
			this.dbgProcessStarterService = dbgProcessStarterService;
			this.startDebuggingOptionsPageProviders = startDebuggingOptionsPageProviders.ToArray();
			this.genericDebugEngineGuidProviders = genericDebugEngineGuidProviders.OrderBy(a => a.Metadata.Order).ToArray();
			mru = new StartDebuggingOptionsMru();
		}

		StartDebuggingOptionsPage[] GetStartDebuggingOptionsPages(StartDebuggingOptionsPageContext context) {
			var list = new List<StartDebuggingOptionsPage>();
			foreach (var provider in startDebuggingOptionsPageProviders)
				list.AddRange(provider.Value.Create(context));
			return list.OrderBy(a => a.DisplayOrder).ToArray();
		}

		string GetCurrentFilename() {
			var filename = documentTabService.DocumentTreeView.TreeView.SelectedItem.GetDocumentNode()?.Document.Filename ?? string.Empty;
			if (File.Exists(filename))
				return filename;
			return string.Empty;
		}

		public string GetCurrentExecutableFilename() {
			var filename = GetCurrentFilename();
			if (PortableExecutableFileHelpers.IsExecutable(filename))
				return filename;
			return null;
		}

		public (StartDebuggingOptions options, StartDebuggingOptionsInfoFlags flags) GetStartDebuggingOptions(string defaultBreakKind) {
			var breakKind = defaultBreakKind ?? PredefinedBreakKinds.DontBreak;
			var filename = GetCurrentFilename();
			var context = new StartDebuggingOptionsPageContext(filename);
			var pages = GetStartDebuggingOptionsPages(context);
			Debug.Assert(pages.Length != 0, "No debug engines!");
			if (pages.Length == 0)
				return default;

			var oldOptions = mru.TryGetOptions(filename);
			var lastOptions = mru.TryGetLastOptions();
			foreach (var page in pages) {
				if (oldOptions?.pageGuid == page.Guid)
					page.InitializePreviousOptions(WithBreakKind(oldOptions.Value.options, defaultBreakKind));
				else if (oldOptions == null && lastOptions?.pageGuid == page.Guid)
					page.InitializeDefaultOptions(filename, breakKind, WithBreakKind(lastOptions.Value.options, defaultBreakKind));
				else
					page.InitializeDefaultOptions(filename, breakKind, null);
			}

			// If there's an exact match ('oldOptions'), then prefer it.
			// Otherwise ask code that knows what kind of exe it is, but prefer the last selected guid if there are multiple matches.
			// Else use last page guid.
			var selectedPageGuid =
				oldOptions?.pageGuid ??
				GetDefaultPageGuid(pages, filename, lastOptions?.pageGuid) ??
				lastOptions?.pageGuid ??
				Guid.Empty;

			var dlg = new DebugProgramDlg();
			var vm = new DebugProgramVM(pages, selectedPageGuid);
			dlg.DataContext = vm;
			dlg.Owner = appWindow.MainWindow;
			var res = dlg.ShowDialog();
			vm.Close();
			if (res != true)
				return default;
			var info = vm.StartDebuggingOptions;
			mru.Add(info.Filename, info.Options, vm.SelectedPageGuid);
			return (info.Options, info.Flags);
		}

		static StartDebuggingOptions WithBreakKind(StartDebuggingOptions options, string breakKind) {
			if (breakKind == null)
				return options;
			options = (StartDebuggingOptions)options.Clone();
			options.BreakKind = breakKind;
			return options;
		}

		Guid? GetDefaultPageGuid(StartDebuggingOptionsPage[] pages, string filename, Guid? lastGuid) {
			var engineGuids = new List<Guid>();
			foreach (var lz in genericDebugEngineGuidProviders) {
				var engineGuid = lz.Value.GetEngineGuid(filename);
				if (engineGuid != null)
					engineGuids.Add(engineGuid.Value);
			}

			Guid? firstResult = null;
			double? firstOrder = null;
			foreach (var engineGuid in engineGuids) {
				foreach (var page in pages) {
					if (page.SupportsDebugEngine(engineGuid, out double order)) {
						// Always prefer the last used page if it matches again
						if (page.Guid == lastGuid)
							return lastGuid;

						if (firstResult == null || order < firstOrder.Value) {
							firstResult = page.Guid;
							firstOrder = order;
						}
					}
				}
				// The order of the engine guids is important so exit as soon as we find a match
				if (firstResult != null)
					break;
			}
			return firstResult;
		}

		public bool CanStartWithoutDebugging(out StartDebuggingResult result) => TryGetStartWithoutDebuggingInfo(out _, out result);

		bool TryGetStartWithoutDebuggingInfo(out string filename, out StartDebuggingResult result) {
			filename = GetCurrentFilename();
			result = StartDebuggingResult.None;
			if (!File.Exists(filename))
				return false;
			if (!dbgProcessStarterService.Value.CanStart(filename, out var startResult))
				return false;
			if ((startResult & ProcessStarterResult.WrongExtension) != 0)
				result |= StartDebuggingResult.WrongExtension;
			return true;
		}

		public bool StartWithoutDebugging(out string error) {
			if (!TryGetStartWithoutDebuggingInfo(out var filename, out _))
				throw new InvalidOperationException();
			return dbgProcessStarterService.Value.TryStart(filename, out error);
		}
	}

	[Flags]
	enum StartDebuggingResult {
		None			= 0,
		WrongExtension	= 0x00000001,
	}
}
