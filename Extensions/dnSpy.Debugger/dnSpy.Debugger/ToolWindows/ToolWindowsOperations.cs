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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Output;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows {
	abstract class ToolWindowsOperations {
		public abstract bool CanShowOutput { get; }
		public abstract void ShowOutput();
		public abstract bool CanShowCodeBreakpoints { get; }
		public abstract void ShowCodeBreakpoints();
		public abstract bool CanShowModuleBreakpoints { get; }
		public abstract void ShowModuleBreakpoints();
		public abstract bool CanShowCallStack { get; }
		public abstract void ShowCallStack();
		public abstract bool CanShowAutos { get; }
		public abstract void ShowAutos();
		public abstract bool CanShowLocals { get; }
		public abstract void ShowLocals();
		public abstract bool CanShowWatch(int index);
		public abstract void ShowWatch(int index);
		public abstract bool CanShowThreads { get; }
		public abstract void ShowThreads();
		public abstract bool CanShowModules { get; }
		public abstract void ShowModules();
		public abstract bool CanShowExceptions { get; }
		public abstract void ShowExceptions();
		public abstract bool CanShowProcesses { get; }
		public abstract void ShowProcesses();
		public abstract bool CanShowMemory(int index);
		public abstract void ShowMemory(int index);
	}

	[Export(typeof(ToolWindowsOperations))]
	sealed class ToolWindowsOperationsImpl : ToolWindowsOperations {
		readonly UIDispatcher uiDispatcher;
		readonly IDsToolWindowService toolWindowService;
		readonly Lazy<IOutputService> outputService;
		readonly Lazy<DbgManager> dbgManager;
		readonly Lazy<Memory.MemoryToolWindowContentProvider> memoryToolWindowContentProvider;
		readonly Lazy<Watch.WatchToolWindowContentProvider> watchToolWindowContentProvider;

		[ImportingConstructor]
		ToolWindowsOperationsImpl(UIDispatcher uiDispatcher, IDsToolWindowService toolWindowService, Lazy<IOutputService> outputService, Lazy<DbgManager> dbgManager, Lazy<Memory.MemoryToolWindowContentProvider> memoryToolWindowContentProvider, Lazy<Watch.WatchToolWindowContentProvider> watchToolWindowContentProvider) {
			this.uiDispatcher = uiDispatcher;
			this.toolWindowService = toolWindowService;
			this.outputService = outputService;
			this.dbgManager = dbgManager;
			this.memoryToolWindowContentProvider = memoryToolWindowContentProvider;
			this.watchToolWindowContentProvider = watchToolWindowContentProvider;
		}

		public override bool CanShowOutput => true;
		public override void ShowOutput() {
			toolWindowService.Show(new Guid("90A45E97-727E-4F31-8692-06E19218D99A"));
			uiDispatcher.UIBackground(() => outputService.Value.Select(Logger.OutputLogger.GUID_OUTPUT_LOGGER_DEBUG));
		}

		public override bool CanShowCodeBreakpoints => true;
		public override void ShowCodeBreakpoints() => toolWindowService.Show(CodeBreakpoints.CodeBreakpointsToolWindowContent.THE_GUID);
		public override bool CanShowModuleBreakpoints => true;
		public override void ShowModuleBreakpoints() => toolWindowService.Show(ModuleBreakpoints.ModuleBreakpointsToolWindowContent.THE_GUID);
		public override bool CanShowCallStack => dbgManager.Value.IsDebugging;
		public override void ShowCallStack() => toolWindowService.Show(CallStack.CallStackToolWindowContent.THE_GUID);
		public override bool CanShowAutos => dbgManager.Value.IsDebugging;
		public override void ShowAutos() => toolWindowService.Show(Autos.AutosToolWindowContentProvider.THE_GUID);
		public override bool CanShowLocals => dbgManager.Value.IsDebugging;
		public override void ShowLocals() => toolWindowService.Show(Locals.LocalsToolWindowContentProvider.THE_GUID);
		public override bool CanShowWatch(int index) => dbgManager.Value.IsDebugging;
		public override void ShowWatch(int index) => toolWindowService.Show(watchToolWindowContentProvider.Value.GetWindowGuid(index));
		public override bool CanShowThreads => dbgManager.Value.IsDebugging;
		public override void ShowThreads() => toolWindowService.Show(Threads.ThreadsToolWindowContent.THE_GUID);
		public override bool CanShowModules => dbgManager.Value.IsDebugging;
		public override void ShowModules() => toolWindowService.Show(Modules.ModulesToolWindowContent.THE_GUID);
		public override bool CanShowExceptions => true;
		public override void ShowExceptions() => toolWindowService.Show(Exceptions.ExceptionsToolWindowContent.THE_GUID);
		public override bool CanShowProcesses => dbgManager.Value.IsDebugging;
		public override void ShowProcesses() => toolWindowService.Show(Processes.ProcessesToolWindowContent.THE_GUID);
		public override bool CanShowMemory(int index) => dbgManager.Value.IsDebugging;
		public override void ShowMemory(int index) => toolWindowService.Show(memoryToolWindowContentProvider.Value.GetWindowGuid(index));
	}
}
