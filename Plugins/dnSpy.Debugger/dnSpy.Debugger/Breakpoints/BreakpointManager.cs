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
	sealed class BreakpointListModifiedEventArgs : EventArgs {
		/// <summary>
		/// Added/removed breakpoint
		/// </summary>
		public Breakpoint Breakpoint { get; }

		/// <summary>
		/// true if added, false if removed
		/// </summary>
		public bool Added { get; }

		public BreakpointListModifiedEventArgs(Breakpoint bp, bool added) {
			this.Breakpoint = bp;
			this.Added = added;
		}
	}

	interface IBreakpointManager {
		Breakpoint[] Breakpoints { get; }
		ILCodeBreakpoint[] ILCodeBreakpoints { get; }
		event EventHandler<BreakpointListModifiedEventArgs> OnListModified;
		void Add(Breakpoint bp);
		void Remove(Breakpoint bp);
		void Clear();
		bool? GetAddRemoveBreakpointsInfo(out int count);
		bool GetEnableDisableBreakpointsInfo(out int count);
		Func<object, object> OnRemoveBreakpoints { get; set; }
	}

	[Export, Export(typeof(IBreakpointManager)), Export(typeof(ILoadBeforeDebug))]
	sealed class BreakpointManager : IBreakpointManager, ILoadBeforeDebug {
		public event EventHandler<BreakpointListModifiedEventArgs> OnListModified;

		readonly HashSet<DebugEventBreakpoint> otherBreakpoints = new HashSet<DebugEventBreakpoint>();

		public Breakpoint[] Breakpoints {
			get {
				var bps = new List<Breakpoint>(textLineObjectManager.GetObjectsOfType<ILCodeBreakpoint>());
				bps.AddRange(otherBreakpoints);
				return bps.ToArray();
			}
		}

		public ILCodeBreakpoint[] ILCodeBreakpoints => textLineObjectManager.GetObjectsOfType<ILCodeBreakpoint>();
		public DebugEventBreakpoint[] DebugEventBreakpoints => otherBreakpoints.ToArray();

		readonly ITextLineObjectManager textLineObjectManager;
		readonly IFileTabManager fileTabManager;
		readonly ITheDebugger theDebugger;
		readonly IMessageBoxManager messageBoxManager;
		readonly IModuleIdCreator moduleIdCreator;

		[ImportingConstructor]
		BreakpointManager(ITextLineObjectManager textLineObjectManager, IFileTabManager fileTabManager, ITheDebugger theDebugger, IMessageBoxManager messageBoxManager, IModuleIdCreator moduleIdCreator) {
			this.textLineObjectManager = textLineObjectManager;
			this.fileTabManager = fileTabManager;
			this.theDebugger = theDebugger;
			this.messageBoxManager = messageBoxManager;
			this.moduleIdCreator = moduleIdCreator;
			textLineObjectManager.OnListModified += MarkedTextLinesManager_OnListModified;
			foreach (var bp in Breakpoints)
				InitializeDebuggerBreakpoint(bp);

			fileTabManager.FileCollectionChanged += FileTabManager_FileCollectionChanged;
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
			if (theDebugger.IsDebugging)
				AddDebuggerBreakpoints();
		}

		void MarkedTextLinesManager_OnListModified(object sender, TextLineObjectListModifiedEventArgs e) =>
			BreakPointAddedRemoved(e.TextLineObject as Breakpoint, e.Added);

		void BreakPointAddedRemoved(Breakpoint bp, bool added) {
			if (bp == null)
				return;
			if (added) {
				InitializeDebuggerBreakpoint(bp);
				OnListModified?.Invoke(this, new BreakpointListModifiedEventArgs(bp, true));
			}
			else {
				UninitializeDebuggerBreakpoint(bp);
				OnListModified?.Invoke(this, new BreakpointListModifiedEventArgs(bp, false));
			}
		}

		public Func<object, object> OnRemoveBreakpoints { get; set; }
		void FileTabManager_FileCollectionChanged(object sender, NotifyFileCollectionChangedEventArgs e) {
			switch (e.Type) {
			case NotifyFileCollectionType.Clear:
			case NotifyFileCollectionType.Remove:
				var existing = new HashSet<ModuleId>(fileTabManager.FileTreeView.GetAllModuleNodes().Select(a => moduleIdCreator.Create(a.DnSpyFile.ModuleDef)));
				var removed = new HashSet<ModuleId>(e.Files.Select(a => moduleIdCreator.Create(a.ModuleDef)));
				existing.Remove(new ModuleId());
				removed.Remove(new ModuleId());
				object orbArg = null;
				if (OnRemoveBreakpoints != null)
					orbArg = OnRemoveBreakpoints(orbArg);
				foreach (var ilbp in ILCodeBreakpoints) {
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
			foreach (var bp in Breakpoints)
				InitializeDebuggerBreakpoint(bp);
		}

		void RemoveDebuggerBreakpoints() {
			foreach (var bp in Breakpoints)
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
				ilbp.DnBreakpoint = debugger.CreateBreakpoint(ilbp.MethodToken.Module.ToSerializedDnModule(), ilbp.MethodToken.Token, ilbp.ILOffset, cond);
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
			var ilbp = bp as ILCodeBreakpoint;
			if (ilbp != null) {
				textLineObjectManager.Add(ilbp);
				return;
			}

			var debp = bp as DebugEventBreakpoint;
			if (debp != null) {
				otherBreakpoints.Add(debp);
				BreakPointAddedRemoved(debp, true);
				return;
			}
		}

		public void Remove(Breakpoint bp) {
			var ilbp = bp as ILCodeBreakpoint;
			if (ilbp != null) {
				textLineObjectManager.Remove(ilbp);
				return;
			}

			var debp = bp as DebugEventBreakpoint;
			if (debp != null) {
				otherBreakpoints.Remove(debp);
				BreakPointAddedRemoved(debp, false);
				return;
			}
		}

		public bool CanClear => Breakpoints.Length != 0;

		public bool ClearAskUser() {
			var res = messageBoxManager.ShowIgnorableMessage(new Guid("37250D26-E844-49F4-904B-29600B90476C"), dnSpy_Debugger_Resources.AskDeleteAllBreakpoints, MsgBoxButton.Yes | MsgBoxButton.No);
			if (res != null && res != MsgBoxButton.Yes)
				return false;
			Clear();
			return true;
		}

		public void Clear() {
			foreach (var bp in Breakpoints)
				Remove(bp);
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

		public bool CanDisableAllBreakpoints => Breakpoints.Any(b => b.IsEnabled);

		public void DisableAllBreakpoints() {
			foreach (var bp in Breakpoints)
				bp.IsEnabled = false;
		}

		public bool CanEnableAllBreakpoints => Breakpoints.Any(b => !b.IsEnabled);

		public void EnableAllBreakpoints() {
			foreach (var bp in Breakpoints)
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
			GetILCodeBreakpoints(documentViewer, documentViewer.GetMethodDebugService().FindByTextPosition(textPosition));

		List<ILCodeBreakpoint> GetILCodeBreakpoints(IDocumentViewer documentViewer, IList<MethodSourceStatement> methodStatements) {
			var list = new List<ILCodeBreakpoint>();
			if (methodStatements.Count == 0)
				return list;
			var methodStatement = methodStatements[0];
			foreach (var ilbp in ILCodeBreakpoints) {
				TextSpan textSpan;
				if (!ilbp.GetLocation(documentViewer, out textSpan))
					continue;
				if (textSpan != methodStatement.Statement.TextSpan)
					continue;

				list.Add(ilbp);
			}

			return list;
		}

		public void Toggle(IDocumentViewer documentViewer, int textPosition) {
			var statements = documentViewer.GetMethodDebugService().FindByTextPosition(textPosition);
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
					var modId = moduleIdCreator.Create(md.Module);
					var key = new ModuleTokenId(modId, md.MDToken);
					Add(new ILCodeBreakpoint(key, methodStatement.Statement.BinSpan.Start));
				}
				documentViewer.MoveCaretToPosition(statements[0].Statement.TextSpan.Start);
			}
		}
	}
}
