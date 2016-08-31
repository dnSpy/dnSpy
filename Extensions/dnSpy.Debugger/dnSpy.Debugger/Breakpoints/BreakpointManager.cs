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
using System.Linq;
using dndbg.Engine;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Breakpoints {
	abstract class BreakpointsEventArgs : EventArgs {
		public Breakpoint[] Breakpoints { get; }

		protected BreakpointsEventArgs(Breakpoint breakpoint) {
			if (breakpoint == null)
				throw new ArgumentNullException(nameof(breakpoint));
			Breakpoints = new[] { breakpoint };
		}

		protected BreakpointsEventArgs(Breakpoint[] breakpoints) {
			if (breakpoints == null)
				throw new ArgumentNullException(nameof(breakpoints));
			Breakpoints = breakpoints;
		}
	}

	sealed class BreakpointsAddedEventArgs : BreakpointsEventArgs {
		public BreakpointsAddedEventArgs(Breakpoint breakpoint)
			: base(breakpoint) {
		}
	}

	sealed class BreakpointsRemovedEventArgs : BreakpointsEventArgs {
		public BreakpointsRemovedEventArgs(Breakpoint breakpoint)
			: base(breakpoint) {
		}

		public BreakpointsRemovedEventArgs(Breakpoint[] breakpoints)
			: base(breakpoints) {
		}
	}

	interface IBreakpointManager {
		Breakpoint[] GetBreakpoints();
		ILCodeBreakpoint[] GetILCodeBreakpoints();
		event EventHandler<BreakpointsAddedEventArgs> BreakpointsAdded;
		event EventHandler<BreakpointsRemovedEventArgs> BreakpointsRemoved;
		void Add(Breakpoint bp);
		void Remove(Breakpoint bp);
		void Clear();
		void Toggle(IDocumentViewer documentViewer, int textPosition);
		bool? GetAddRemoveBreakpointsInfo(out int count);
		bool GetEnableDisableBreakpointsInfo(out int count);
		Func<object, object> OnRemoveBreakpoints { get; set; }
	}

	/// <summary>
	/// Created just before the first breakpoint gets added
	/// </summary>
	interface IBreakpointListener {
	}

	[Export, Export(typeof(IBreakpointManager)), Export(typeof(ILoadBeforeDebug))]
	sealed class BreakpointManager : IBreakpointManager, ILoadBeforeDebug {
		public event EventHandler<BreakpointsAddedEventArgs> BreakpointsAdded;
		public event EventHandler<BreakpointsRemovedEventArgs> BreakpointsRemoved;

		readonly HashSet<DebugEventBreakpoint> eventBreakpoints = new HashSet<DebugEventBreakpoint>();
		readonly HashSet<ILCodeBreakpoint> ilCodeBreakpoints = new HashSet<ILCodeBreakpoint>();

		readonly IFileTabManager fileTabManager;
		readonly ITheDebugger theDebugger;
		readonly IMessageBoxManager messageBoxManager;
		readonly IModuleIdProvider moduleIdProvider;
		readonly Lazy<IBreakpointListener>[] breakpointListeners;
		bool breakpointListenersInitialized;

		[ImportingConstructor]
		BreakpointManager(IFileTabManager fileTabManager, ITheDebugger theDebugger, IMessageBoxManager messageBoxManager, IModuleIdProvider moduleIdProvider, [ImportMany] IEnumerable<Lazy<IBreakpointListener>> breakpointListeners) {
			this.fileTabManager = fileTabManager;
			this.theDebugger = theDebugger;
			this.messageBoxManager = messageBoxManager;
			this.moduleIdProvider = moduleIdProvider;
			this.breakpointListeners = breakpointListeners.ToArray();

			fileTabManager.FileCollectionChanged += FileTabManager_FileCollectionChanged;
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
			if (theDebugger.IsDebugging)
				AddDebuggerBreakpoints();
		}

		public Breakpoint[] GetBreakpoints() {
			var bps = new Breakpoint[ilCodeBreakpoints.Count + eventBreakpoints.Count];
			int i = 0;
			foreach (var bp in ilCodeBreakpoints)
				bps[i++] = bp;
			foreach (var bp in eventBreakpoints)
				bps[i++] = bp;
			return bps;
		}

		public ILCodeBreakpoint[] GetILCodeBreakpoints() => ilCodeBreakpoints.ToArray();

		public Func<object, object> OnRemoveBreakpoints { get; set; }
		void FileTabManager_FileCollectionChanged(object sender, NotifyFileCollectionChangedEventArgs e) {
			switch (e.Type) {
			case NotifyFileCollectionType.Clear:
			case NotifyFileCollectionType.Remove:
				var existing = new HashSet<ModuleId>(fileTabManager.FileTreeView.GetAllModuleNodes().Select(a => moduleIdProvider.Create(a.DnSpyFile.ModuleDef)));
				var removed = new HashSet<ModuleId>(e.Files.Select(a => moduleIdProvider.Create(a.ModuleDef)));
				existing.Remove(new ModuleId());
				removed.Remove(new ModuleId());
				object orbArg = null;
				if (OnRemoveBreakpoints != null)
					orbArg = OnRemoveBreakpoints(orbArg);
				foreach (var ilbp in GetILCodeBreakpoints()) {
					// Don't auto-remove BPs in dynamic modules since they have no disk file. The
					// user must delete these him/herself.
					if (ilbp.MethodToken.Module.IsDynamic)
						continue;

					// If the file is still in the TV, don't delete anything. This can happen if
					// we've loaded an in-memory module and the node just got removed.
					if (existing.Contains(ilbp.MethodToken.Module))
						continue;

					if (removed.Contains(ilbp.MethodToken.Module))
						Remove(ilbp);
				}
				OnRemoveBreakpoints?.Invoke(orbArg);
				break;

			case NotifyFileCollectionType.Add:
				break;
			}
		}

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			switch (theDebugger.ProcessState) {
			case DebuggerProcessState.Starting:
				AddDebuggerBreakpoints();
				break;

			case DebuggerProcessState.Continuing:
			case DebuggerProcessState.Running:
			case DebuggerProcessState.Paused:
				break;

			case DebuggerProcessState.Terminated:
				RemoveDebuggerBreakpoints();
				break;
			}
		}

		void AddDebuggerBreakpoints() {
			foreach (var bp in GetBreakpoints())
				InitializeDebuggerBreakpoint(bp);
		}

		void RemoveDebuggerBreakpoints() {
			foreach (var bp in GetBreakpoints())
				UninitializeDebuggerBreakpoint(bp);
		}

		void InitializeDebuggerBreakpoint(Breakpoint bp) {
			var debugger = theDebugger.Debugger;
			if (debugger == null || theDebugger.ProcessState == DebuggerProcessState.Terminated)
				return;

			switch (bp.Kind) {
			case BreakpointKind.ILCode:
				var ilbp = (ILCodeBreakpoint)bp;
				Func<ILCodeBreakpointConditionContext, bool> cond = null;//TODO: Let user pick what cond to use
				Debug.Assert(ilbp.DnBreakpoint == null);
				ilbp.DnBreakpoint = debugger.CreateBreakpoint(ilbp.MethodToken.Module.ToDnModuleId(), ilbp.MethodToken.Token, ilbp.ILOffset, cond);
				break;

			case BreakpointKind.DebugEvent:
				//TODO:
				break;

			default:
				throw new InvalidOperationException();
			}
		}

		void UninitializeDebuggerBreakpoint(Breakpoint bp) {
			var dnbp = bp.DnBreakpoint;
			bp.DnBreakpoint = null;
			if (dnbp != null) {
				var dbg = theDebugger.Debugger;
				if (dbg != null)
					dbg.RemoveBreakpoint(dnbp);
			}
		}

		public void Add(Breakpoint bp) {
			if (!breakpointListenersInitialized) {
				breakpointListenersInitialized = true;
				foreach (var lazy in breakpointListeners) {
					var b = lazy.Value;
				}
			}

			var ilbp = bp as ILCodeBreakpoint;
			if (ilbp != null) {
				bool b = ilCodeBreakpoints.Add(ilbp);
				Debug.Assert(b);
				if (b) {
					InitializeDebuggerBreakpoint(bp);
					BreakpointsAdded?.Invoke(this, new BreakpointsAddedEventArgs(bp));
				}
				return;
			}

			var debp = bp as DebugEventBreakpoint;
			if (debp != null) {
				bool b = eventBreakpoints.Add(debp);
				Debug.Assert(b);
				if (b) {
					InitializeDebuggerBreakpoint(bp);
					BreakpointsAdded?.Invoke(this, new BreakpointsAddedEventArgs(bp));
				}
				return;
			}
		}

		public void Remove(Breakpoint bp) {
			var ilbp = bp as ILCodeBreakpoint;
			if (ilbp != null) {
				bool b = ilCodeBreakpoints.Remove(ilbp);
				Debug.Assert(b);
				if (b) {
					UninitializeDebuggerBreakpoint(bp);
					BreakpointsRemoved?.Invoke(this, new BreakpointsRemovedEventArgs(bp));
				}
				return;
			}

			var debp = bp as DebugEventBreakpoint;
			if (debp != null) {
				bool b = eventBreakpoints.Remove(debp);
				Debug.Assert(b);
				if (b) {
					UninitializeDebuggerBreakpoint(bp);
					BreakpointsRemoved?.Invoke(this, new BreakpointsRemovedEventArgs(bp));
				}
				return;
			}
		}

		public bool CanClear => ilCodeBreakpoints.Count != 0 || eventBreakpoints.Count != 0;

		public bool ClearAskUser() {
			var res = messageBoxManager.ShowIgnorableMessage(new Guid("37250D26-E844-49F4-904B-29600B90476C"), dnSpy_Debugger_Resources.AskDeleteAllBreakpoints, MsgBoxButton.Yes | MsgBoxButton.No);
			if (res != null && res != MsgBoxButton.Yes)
				return false;
			Clear();
			return true;
		}

		public void Clear() {
			var bps = GetBreakpoints();
			ilCodeBreakpoints.Clear();
			eventBreakpoints.Clear();
			foreach (var bp in bps)
				UninitializeDebuggerBreakpoint(bp);
			BreakpointsRemoved?.Invoke(this, new BreakpointsRemovedEventArgs(bps));
		}

		public bool CanToggleBreakpoint => fileTabManager.ActiveTab.TryGetDocumentViewer().GetMethodDebugService().Count != 0;

		public bool ToggleBreakpoint() {
			if (!CanToggleBreakpoint)
				return false;

			var documentViewer = fileTabManager.ActiveTab.TryGetDocumentViewer();
			if (documentViewer == null)
				return false;
			Toggle(documentViewer, documentViewer.Caret.Position.BufferPosition.Position);
			return true;
		}

		public bool? GetAddRemoveBreakpointsInfo(out int count) {
			count = 0;
			var documentViewer = fileTabManager.ActiveTab.TryGetDocumentViewer();
			if (documentViewer == null)
				return null;
			var ilbps = GetILCodeBreakpoints(documentViewer, documentViewer.Caret.Position.BufferPosition);
			count = ilbps.Count;
			if (ilbps.Count == 0)
				return null;
			return IsEnabled(ilbps);
		}

		public bool CanDisableBreakpoint {
			get {
				var documentViewer = fileTabManager.ActiveTab.TryGetDocumentViewer();
				if (documentViewer == null)
					return false;
				return GetILCodeBreakpoints(documentViewer, documentViewer.Caret.Position.BufferPosition).Count != 0;
			}
		}

		public bool DisableBreakpoint() {
			if (!CanDisableBreakpoint)
				return false;

			var documentViewer = fileTabManager.ActiveTab.TryGetDocumentViewer();
			if (documentViewer == null)
				return false;
			var ilbps = GetILCodeBreakpoints(documentViewer, documentViewer.Caret.Position.BufferPosition);
			bool isEnabled = IsEnabled(ilbps);
			foreach (var ilbp in ilbps)
				ilbp.IsEnabled = !isEnabled;
			return ilbps.Count > 0;
		}

		public bool GetEnableDisableBreakpointsInfo(out int count) {
			count = 0;
			var documentViewer = fileTabManager.ActiveTab.TryGetDocumentViewer();
			if (documentViewer == null)
				return false;
			var ilbps = GetILCodeBreakpoints(documentViewer, documentViewer.Caret.Position.BufferPosition);
			count = ilbps.Count;
			return IsEnabled(ilbps);
		}

		public bool CanDisableAllBreakpoints => GetBreakpoints().Any(b => b.IsEnabled);

		public void DisableAllBreakpoints() {
			foreach (var bp in GetBreakpoints())
				bp.IsEnabled = false;
		}

		public bool CanEnableAllBreakpoints => GetBreakpoints().Any(b => !b.IsEnabled);

		public void EnableAllBreakpoints() {
			foreach (var bp in GetBreakpoints())
				bp.IsEnabled = true;
		}

		static bool IsEnabled(IEnumerable<ILCodeBreakpoint> bps) {
			foreach (var bp in bps) {
				if (bp.IsEnabled)
					return true;
			}
			return false;
		}

		List<ILCodeBreakpoint> GetILCodeBreakpoints(IDocumentViewer documentViewer, int textPosition) =>
			GetILCodeBreakpoints(documentViewer, documentViewer.GetMethodDebugService().FindByTextPosition(textPosition, true));

		//TODO: This method (and all callers) should take an ITextView instead of an IDocumentViewer as a parameter
		List<ILCodeBreakpoint> GetILCodeBreakpoints(IDocumentViewer documentViewer, IList<MethodSourceStatement> methodStatements) {
			var list = new List<ILCodeBreakpoint>();
			if (methodStatements.Count == 0)
				return list;
			var service = documentViewer.TryGetMethodDebugService();
			if (service == null)
				return list;
			var methodStatement = methodStatements[0];
			foreach (var ilbp in GetILCodeBreakpoints()) {
				var info = service.TryGetMethodDebugInfo(ilbp.MethodToken);
				if (info == null)
					continue;
				var statement = info.GetSourceStatementByCodeOffset(ilbp.ILOffset);
				if (statement == null)
					continue;
				if (statement.Value.TextSpan != methodStatement.Statement.TextSpan)
					continue;

				list.Add(ilbp);
			}

			return list;
		}

		public void Toggle(IDocumentViewer documentViewer, int textPosition) {
			var statements = documentViewer.GetMethodDebugService().FindByTextPosition(textPosition, true);
			var ilbps = GetILCodeBreakpoints(documentViewer, statements);
			if (ilbps.Count > 0) {
				if (IsEnabled(ilbps)) {
					foreach (var ilbp in ilbps)
						Remove(ilbp);
				}
				else {
					foreach (var bpm in ilbps)
						bpm.IsEnabled = true;
				}
			}
			else if (statements.Count > 0) {
				foreach (var methodStatement in statements) {
					var md = methodStatement.Method;
					var modId = moduleIdProvider.Create(md.Module);
					var key = new ModuleTokenId(modId, md.MDToken);
					Add(new ILCodeBreakpoint(key, methodStatement.Statement.BinSpan.Start));
				}
				documentViewer.MoveCaretToPosition(statements[0].Statement.TextSpan.Start);
			}
		}
	}
}
