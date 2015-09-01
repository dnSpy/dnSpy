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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.AvalonEdit;
using dnSpy.MVVM;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.NRefactory;

namespace dnSpy.Debugger {
	public class BreakpointListModifiedEventArgs : EventArgs {
		/// <summary>
		/// Added/removed breakpoint
		/// </summary>
		public Breakpoint Breakpoint { get; private set; }

		/// <summary>
		/// true if added, false if removed
		/// </summary>
		public bool Added { get; private set; }

		public BreakpointListModifiedEventArgs(Breakpoint bp, bool added) {
			this.Breakpoint = bp;
			this.Added = added;
		}
	}

	public sealed class BreakpointManager {
		public static readonly BreakpointManager Instance = new BreakpointManager();

		public event EventHandler<BreakpointListModifiedEventArgs> OnListModified;

		readonly HashSet<DebugEventBreakpoint> otherBreakpoints = new HashSet<DebugEventBreakpoint>();

		public ICommand ClearCommand {
			get { return new RelayCommand(a => ClearAskUser(), a => CanClear); }
		}

		public ICommand ToggleBreakpointCommand {
			get { return new RelayCommand(a => ToggleBreakpoint(), a => CanToggleBreakpoint); }
		}

		public ICommand DisableBreakpointCommand {
			get { return new RelayCommand(a => DisableBreakpoint(), a => CanDisableBreakpoint); }
		}

		public Breakpoint[] Breakpoints {
			get {
				var bps = new List<Breakpoint>(TextLineObjectManager.Instance.GetObjectsOfType<ILCodeBreakpoint>());
				bps.AddRange(otherBreakpoints);
				return bps.ToArray();
			}
		}

		public ILCodeBreakpoint[] ILCodeBreakpoints {
			get { return TextLineObjectManager.Instance.GetObjectsOfType<ILCodeBreakpoint>(); }
		}

		public DebugEventBreakpoint[] DebugEventBreakpoints {
			get { return otherBreakpoints.ToArray(); }
		}

		BreakpointManager() {
			TextLineObjectManager.Instance.OnListModified += MarkedTextLinesManager_OnListModified;
			foreach (var bp in Breakpoints)
				InitializeDebuggerBreakpoint(bp);
		}

		void MarkedTextLinesManager_OnListModified(object sender, TextLineObjectListModifiedEventArgs e) {
			BreakPointAddedRemoved(e.TextLineObject as Breakpoint, e.Added);
		}

		void BreakPointAddedRemoved(Breakpoint bp, bool added) {
			if (bp == null)
				return;
			if (added) {
				InitializeDebuggerBreakpoint(bp);
				if (OnListModified != null)
					OnListModified(this, new BreakpointListModifiedEventArgs(bp, true));
			}
			else {
				UninitializeDebuggerBreakpoint(bp);
				if (OnListModified != null)
					OnListModified(this, new BreakpointListModifiedEventArgs(bp, false));
			}
		}

		internal void OnLoaded() {
			BreakpointSettings.Instance.OnLoaded();
			MainWindow.Instance.CurrentAssemblyListChanged += MainWindow_CurrentAssemblyListChanged;
			DebugManager.Instance.OnProcessStateChanged += DebugManager_OnProcessStateChanged;
			if (DebugManager.Instance.IsDebugging)
				AddDebuggerBreakpoints();
		}

		void MainWindow_CurrentAssemblyListChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (e.OldItems != null) {
				foreach (var ilbp in ILCodeBreakpoints) {
					if (e.OldItems.Cast<LoadedAssembly>().Any(n => GetSerializedDnModule(n.ModuleDefinition) == ilbp.MethodKey.Module))
						Remove(ilbp);
				}
			}
		}

		static SerializedDnModule GetSerializedDnModule(ModuleDef module) {
			if (module == null)
				return new SerializedDnModule();
			return new SerializedDnModule(module.Location);
		}

		void DebugManager_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			switch (DebugManager.Instance.ProcessState) {
			case DebuggerProcessState.Starting:
				AddDebuggerBreakpoints();
				break;

			case DebuggerProcessState.Running:
			case DebuggerProcessState.Stopped:
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
			var debugger = DebugManager.Instance.Debugger;
			if (debugger == null || debugger.ProcessState == DebuggerProcessState.Terminated)
				return;

			IBreakpointCondition cond;
			switch (bp.Type) {
			case BreakpointType.ILCode:
				var ilbp = (ILCodeBreakpoint)bp;
				cond = AlwaysBreakpointCondition.Instance;//TODO: Let user pick what cond to use
				Debug.Assert(ilbp.DnBreakpoint == null);
				ilbp.DnBreakpoint = debugger.CreateBreakpoint(ilbp.MethodKey.Module, ilbp.MethodKey.Token, ilbp.ILOffset, cond);
				break;

			case BreakpointType.DebugEvent:
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
				var dbg = DebugManager.Instance.Debugger;
				if (dbg != null)
					dbg.RemoveBreakpoint(dnbp);
			}
		}

		public void Add(Breakpoint bp) {
			var ilbp = bp as ILCodeBreakpoint;
			if (ilbp != null) {
				TextLineObjectManager.Instance.Add(ilbp);
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
				TextLineObjectManager.Instance.Remove(ilbp);
				return;
			}

			var debp = bp as DebugEventBreakpoint;
			if (debp != null) {
				otherBreakpoints.Remove(debp);
				BreakPointAddedRemoved(debp, false);
				return;
			}
		}

		public bool CanClear {
			get { return Breakpoints.Length != 0; }
		}

		public bool ClearAskUser() {
			var res = MainWindow.Instance.ShowIgnorableMessageBox("debug: delete all bps", "Do you want to delete all breakpoints?", MessageBoxButton.YesNo);
			if (res != null && res != MsgBoxButton.OK)
				return false;
			Clear();
			return true;
		}

		public void Clear() {
			foreach (var bp in Breakpoints)
				Remove(bp);
		}

		public bool CanToggleBreakpoint {
			get { return MainWindow.Instance.ActiveTextView != null; }
		}

		public bool ToggleBreakpoint() {
			if (!CanToggleBreakpoint)
				return false;

			var textView = MainWindow.Instance.ActiveTextView;
			if (textView == null)
				return false;
			var location = textView.TextEditor.TextArea.Caret.Location;
			BreakpointHelper.Toggle(textView, location.Line, location.Column);
			return true;
		}

		internal bool? GetAddRemoveBreakpointsInfo(out int count) {
			count = 0;
			var textView = MainWindow.Instance.ActiveTextView;
			if (textView == null)
				return null;
			var location = textView.TextEditor.TextArea.Caret.Location;
			var ilbps = BreakpointHelper.GetILCodeBreakpoints(textView, location.Line, location.Column);
			count = ilbps.Count;
			if (ilbps.Count == 0)
				return null;
			return BreakpointHelper.IsEnabled(ilbps);
		}

		public bool CanDisableBreakpoint {
			get {
				var textView = MainWindow.Instance.ActiveTextView;
				if (textView == null)
					return false;
				var location = textView.TextEditor.TextArea.Caret.Location;
				return BreakpointHelper.GetILCodeBreakpoints(textView, location.Line, location.Column).Count != 0;
			}
		}

		public bool DisableBreakpoint() {
			if (!CanDisableBreakpoint)
				return false;

			var textView = MainWindow.Instance.ActiveTextView;
			if (textView == null)
				return false;
			var location = textView.TextEditor.TextArea.Caret.Location;
			var ilbps = BreakpointHelper.GetILCodeBreakpoints(textView, location.Line, location.Column);
			bool isEnabled = BreakpointHelper.IsEnabled(ilbps);
			foreach (var ilbp in ilbps)
				ilbp.IsEnabled = !isEnabled;
			return ilbps.Count > 0;
		}

		internal bool GetEnableDisableBreakpointsInfo(out int count) {
			count = 0;
			var textView = MainWindow.Instance.ActiveTextView;
			if (textView == null)
				return false;
			var location = textView.TextEditor.TextArea.Caret.Location;
			var ilbps = BreakpointHelper.GetILCodeBreakpoints(textView, location.Line, location.Column);
			count = ilbps.Count;
			return BreakpointHelper.IsEnabled(ilbps);
		}

		internal void Toggle(DecompilerTextView textView, int line, int column = 0) {
			BreakpointHelper.Toggle(textView, line, column);
		}

		static class BreakpointHelper {
			public static bool IsEnabled(IEnumerable<ILCodeBreakpoint> bps) {
				foreach (var bp in bps) {
					if (bp.IsEnabled)
						return true;
				}
				return false;
			}

			public static List<ILCodeBreakpoint> GetILCodeBreakpoints(DecompilerTextView textView, int line, int column) {
				return GetILCodeBreakpoints(textView, SourceCodeMappingUtils.Find(textView, line, column));
			}

			static List<ILCodeBreakpoint> GetILCodeBreakpoints(DecompilerTextView textView, IList<SourceCodeMapping> mappings) {
				var list = new List<ILCodeBreakpoint>();
				if (mappings.Count == 0)
					return list;
				var mapping = mappings[0];
				foreach (var ilbp in BreakpointManager.Instance.ILCodeBreakpoints) {
					TextLocation location, endLocation;
					if (!ilbp.GetLocation(textView, out location, out endLocation))
						continue;
					if (location != mapping.StartLocation || endLocation != mapping.EndLocation)
						continue;

					list.Add(ilbp);
				}

				return list;
			}

			public static void Toggle(DecompilerTextView textView, int line, int column) {
				var bps = SourceCodeMappingUtils.Find(textView, line, column);
				var ilbps = GetILCodeBreakpoints(textView, bps);
				if (ilbps.Count > 0) {
					if (IsEnabled(ilbps)) {
						foreach (var ilbp in ilbps)
							BreakpointManager.Instance.Remove(ilbp);
					}
					else {
						foreach (var bpm in ilbps)
							bpm.IsEnabled = true;
					}
				}
				else if (bps.Count > 0) {
					foreach (var bp in bps) {
						var md = bp.MemberMapping.MethodDefinition;
						var key = MethodKey.Create(md);
						if (key == null)
							continue;
						var asm = md.Module == null ? null : md.Module.Assembly;
						var asmName = asm == null ? null : asm.ManifestModule.Location;
						BreakpointManager.Instance.Add(new ILCodeBreakpoint(asmName, key.Value, bp.ILInstructionOffset.From));
					}
					textView.ScrollAndMoveCaretTo(bps[0].StartLocation.Line, bps[0].StartLocation.Column);
				}
			}
		}
	}
}
