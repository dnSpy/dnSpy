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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.AsmEditor;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.NRefactory;
using ICSharpCode.TreeView;

namespace dnSpy.Debugger {
	public sealed class DebugManager {
		public static readonly DebugManager Instance = new DebugManager();

		[DllImport("user32")]
		static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		internal void OnLoaded() {
			MainWindow.Instance.Closing += OnClosing;
			new BringDebuggedProgramWindowToFront();
			MainWindow.Instance.CanExecuteEvent += MainWindow_CanExecuteEvent;
		}

		void MainWindow_CanExecuteEvent(object sender, MainWindow.CanExecuteEventArgs e) {
			if (e.Type == MainWindow.CanExecuteType.ReloadList) {
				if (Instance.IsDebugging)
					e.Result = false;
			}
		}

		DebugManager() {
			OnProcessStateChanged += DebugManager_OnProcessStateChanged;
		}

		public ICommand DebugCurrentAssemblyCommand {
			get { return new RelayCommand(a => DebugCurrentAssembly(a), a => CanDebugCurrentAssembly(a)); }
		}

		public ICommand DebugAssemblyCommand {
			get { return new RelayCommand(a => DebugAssembly(), a => CanDebugAssembly); }
		}

		public ICommand AttachCommand {
			get { return new RelayCommand(a => Attach(), a => CanAttach); }
		}

		public ICommand BreakCommand {
			get { return new RelayCommand(a => Break(), a => CanBreak); }
		}

		public ICommand RestartCommand {
			get { return new RelayCommand(a => Restart(), a => CanRestart); }
		}

		public ICommand StopCommand {
			get { return new RelayCommand(a => Stop(), a => CanStop); }
		}

		public ICommand DetachCommand {
			get { return new RelayCommand(a => Detach(), a => CanDetach); }
		}

		public ICommand ContinueCommand {
			get { return new RelayCommand(a => Continue(), a => CanContinue); }
		}

		public ICommand StepIntoCommand {
			get { return new RelayCommand(a => StepInto(), a => CanStepInto); }
		}

		public ICommand StepOverCommand {
			get { return new RelayCommand(a => StepOver(), a => CanStepOver); }
		}

		public ICommand StepOutCommand {
			get { return new RelayCommand(a => StepOut(), a => CanStepOut); }
		}

		public ICommand ShowNextStatementCommand {
			get { return new RelayCommand(a => ShowNextStatement(), a => CanShowNextStatement); }
		}

		public ICommand SetNextStatementCommand {
			get { return new RelayCommand(a => SetNextStatement(a), a => CanSetNextStatement(a)); }
		}

		public DebuggerProcessState ProcessState {
			get { return Debugger == null ? DebuggerProcessState.Terminated : Debugger.ProcessState; }
		}

		/// <summary>
		/// true if we're debugging
		/// </summary>
		public bool IsDebugging {
			get { return ProcessState != DebuggerProcessState.Terminated; }
		}

		/// <summary>
		/// true if debugged process is running
		/// </summary>
		public bool IsProcessRunning {
			get { return ProcessState == DebuggerProcessState.Running; }
		}

		/// <summary>
		/// Gets the current debugger. This is null if we're not debugging anything
		/// </summary>
		public DnDebugger Debugger {
			get { return debugger; }
		}
		DnDebugger debugger;

		public event EventHandler<DebuggerEventArgs> OnProcessStateChanged;

		static void SetRunningStatusMessage() {
			MainWindow.Instance.SetStatus("Running…");
		}

		static void SetReadyStatusMessage() {
			MainWindow.Instance.SetStatus("Ready");
		}

		void DebugManager_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			switch (DebugManager.Instance.ProcessState) {
			case DebuggerProcessState.Starting:
				currentLocation = null;
				currentMethod = null;
				MainWindow.Instance.SessionSettings.FilterSettings.ShowInternalApi = true;
				SetRunningStatusMessage();
				MainWindow.Instance.SetDebugging();
				break;

			case DebuggerProcessState.Running:
				SetRunningStatusMessage();
				break;

			case DebuggerProcessState.Stopped:
				SetWindowPos(new WindowInteropHelper(MainWindow.Instance).Handle, IntPtr.Zero, 0, 0, 0, 0, 3);
				MainWindow.Instance.Activate();

				UpdateCurrentLocation();
				if (currentMethod != null && currentLocation != null)
					JumpToCurrentStatement(MainWindow.Instance.SafeActiveTextView);

				SetReadyStatusMessage();
				break;

			case DebuggerProcessState.Terminated:
				currentLocation = null;
				currentMethod = null;
				MainWindow.Instance.HideStatus();
				MainWindow.Instance.ClearDebugging();
				break;
			}
		}
		CodeLocation? currentLocation = null;

		void OnClosing(object sender, CancelEventArgs e) {
			if (IsDebugging) {
				var result = MainWindow.Instance.ShowIgnorableMessageBox("debug: exit program", "Do you want to stop debugging?", MessageBoxButton.YesNo);
				if (result == MsgBoxButton.None || result == MsgBoxButton.No)
					e.Cancel = true;
			}
		}

		public bool DebugProcess(DebugProcessOptions options) {
			if (IsDebugging)
				return false;
			if (options == null)
				return false;

			RemoveDebugger();

			DnDebugger newDebugger;
			try {
				newDebugger = DnDebugger.DebugProcess(options);
			}
			catch (Exception ex) {
				var cex = ex as COMException;
				const int ERROR_NOT_SUPPORTED = unchecked((int)0x80070032);
				const int CORDBG_E_UNCOMPATIBLE_PLATFORMS = unchecked((int)0x80131C30);
				if (cex != null && cex.ErrorCode == ERROR_NOT_SUPPORTED)
					MainWindow.Instance.ShowMessageBox("Could not start debugger. Use dnSpy.exe to debug 64-bit applications.");
				else if (cex != null && cex.ErrorCode == CORDBG_E_UNCOMPATIBLE_PLATFORMS)
					MainWindow.Instance.ShowMessageBox("Could not start debugger. Use dnSpy-x86.exe to debug 32-bit applications.");
				else
					MainWindow.Instance.ShowMessageBox(string.Format("Could not start debugger. Make sure you have access to the file '{0}'\n\nError: {1}", options.Filename, ex.Message));
				return false;
			}
			AddDebugger(newDebugger);
			Debug.Assert(debugger == newDebugger);
			CallOnProcessStateChanged();

			return true;
		}

		void CallOnProcessStateChanged() {
			CallOnProcessStateChanged(debugger, DebuggerEventArgs.Empty);
		}

		void CallOnProcessStateChanged(object sender, DebuggerEventArgs e) {
			if (OnProcessStateChanged != null)
				OnProcessStateChanged(sender, e ?? DebuggerEventArgs.Empty);
		}

		void DnDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			if (debugger == null || sender != debugger)
				return;

			CallOnProcessStateChanged(sender, e);

			if (debugger.ProcessState == DebuggerProcessState.Terminated) {
				lastDebugProcessOptions = null;
				RemoveDebugger();
			}
		}

		void RemoveDebugger() {
			if (debugger == null)
				return;

			debugger.OnProcessStateChanged -= DnDebugger_OnProcessStateChanged;
			debugger = null;
		}

		void AddDebugger(DnDebugger newDebugger) {
			RemoveDebugger();

			debugger = newDebugger;
			newDebugger.OnProcessStateChanged += DnDebugger_OnProcessStateChanged;
		}

		public bool CanDebugCurrentAssembly(object parameter) {
			return GetCurrentAssembly(parameter as ContextMenuEntryContext) != null;
		}

		public void DebugCurrentAssembly(object parameter) {
			var asm = GetCurrentAssembly(parameter as ContextMenuEntryContext);
			if (asm == null)
				return;
			DebugAssembly(GetDebugCurrentAssemblyOptions(asm));
		}

		internal LoadedAssembly GetCurrentAssembly(ContextMenuEntryContext context) {
			if (context == null)
				return null;
			if (IsDebugging)
				return null;

			SharpTreeNode node;
			if (context.Element is DecompilerTextView) {
				var tabState = MainWindow.Instance.GetActiveDecompileTabState();
				if (tabState == null)
					return null;
				if (tabState.DecompiledNodes.Length == 0)
					return null;
				node = tabState.DecompiledNodes[0];
			}
			else if (context.SelectedTreeNodes != null) {
				if (context.SelectedTreeNodes.Length == 0)
					return null;
				node = context.SelectedTreeNodes[0];
			}
			else
				return null;

			var asmNode = ILSpyTreeNode.GetNode<AssemblyTreeNode>(node);
			if (asmNode == null)
				return null;

			var loadedAsm = asmNode.LoadedAssembly;
			var mod = loadedAsm.ModuleDefinition;
			if (mod == null)
				return null;
			if (mod.Assembly == null || mod.Assembly.ManifestModule != mod)
				return null;
			if (mod.ManagedEntryPoint == null && mod.NativeEntryPoint == 0)
				return null;

			return loadedAsm;
		}

		DebugProcessOptions GetDebugCurrentAssemblyOptions(LoadedAssembly asm) {
			return null;//TODO:
		}

		public bool CanDebugAssembly {
			get { return !IsDebugging; }
		}

		public void DebugAssembly() {
			if (!CanDebugAssembly)
				return;
			DebugAssembly(GetDebugAssemblyOptions());
		}

		DebugProcessOptions GetDebugAssemblyOptions() {
			return null;//TODO:
		}

		public bool CanRestart {
			get { return IsDebugging && lastDebugProcessOptions != null; }
		}

		public void Restart() {
			if (!CanRestart)
				return;

			Stop();
			if (debugger != null) {
				RemoveDebugger();
				CallOnProcessStateChanged();
			}

			DebugAssembly(lastDebugProcessOptions);
		}

		void DebugAssembly(DebugProcessOptions options) {
			if (options == null)
				return;
			var optionsCopy = options.Clone();
			if (!DebugProcess(options))
				return;
			lastDebugProcessOptions = optionsCopy;
		}
		DebugProcessOptions lastDebugProcessOptions = null;

		public bool CanAttach {
			get { return !IsDebugging; }
		}

		public void Attach() {
			if (!CanAttach)
				return;
			//TODO:
		}

		public bool CanBreak {
			get { return ProcessState == DebuggerProcessState.Starting || ProcessState == DebuggerProcessState.Running; }
		}

		public void Break() {
			if (!CanBreak)
				return;

			int hr = Debugger.TryBreakProcesses();
			if (hr < 0)
				MainWindow.Instance.ShowMessageBox(string.Format("Could not break process. Error: 0x{0:X8}", hr));
		}

		public bool CanStop {
			get { return IsDebugging; }
		}

		public void Stop() {
			if (!CanStop)
				return;
			Debugger.TerminateProcesses();
		}

		public bool CanDetach {
			get { return ProcessState != DebuggerProcessState.Terminated; }
		}

		public void Detach() {
			if (!CanDetach)
				return;
			int hr = Debugger.TryDetach();
			if (hr < 0)
				MainWindow.Instance.ShowMessageBox(string.Format("Could not detach process. Error: 0x{0:X8}", hr));
		}

		public bool CanContinue {
			get { return ProcessState == DebuggerProcessState.Stopped; }
		}

		public void Continue() {
			if (!CanContinue)
				return;
			Debugger.Continue();
		}

		public bool JumpToCurrentStatement(DecompilerTextView textView) {
			if (textView == null)
				return false;
			if (currentMethod == null)
				return false;
			return MainWindow.Instance.JumpToReference(textView, currentMethod, (success, hasMovedCaret) => {
				if (success)
					return MoveCaretToCurrentStatement(textView);
				return false;
			});
		}

		bool MoveCaretToCurrentStatement(DecompilerTextView textView) {
			if (currentLocation == null)
				return false;
			return MoveCaretTo(textView, currentLocation.Value.MethodKey, currentLocation.Value.ILFrameIP.Offset);
		}

		bool MoveCaretTo(DecompilerTextView textView, MethodKey key, uint ilOffset) {
			if (textView == null)
				return false;
			TextLocation location, endLocation;
			var cm = textView.CodeMappings;
			if (cm == null || !cm.ContainsKey(key))
				return false;
			if (!cm[key].GetInstructionByTokenAndOffset(ilOffset, out location, out endLocation)) {
				//TODO: Missing IL ranges
				return false;
			}
			else {
				textView.ScrollAndMoveCaretTo(location.Line, location.Column);
				return true;
			}
		}

		struct CodeLocation {
			public SerializedDnModuleWithAssembly ModuleAssembly;
			public readonly uint Token;
			public ILFrameIP ILFrameIP;

			public MethodKey MethodKey {
				get { return MethodKey.Create(Token, ModuleAssembly.Module); }
			}

			public CodeLocation(SerializedDnModuleWithAssembly moduleAssembly, uint token, ILFrameIP ip) {
				this.ModuleAssembly = moduleAssembly;
				this.Token = token;
				this.ILFrameIP = ip;
			}

			public static bool SameMethod(CodeLocation a, CodeLocation b) {
				return a.ModuleAssembly == b.ModuleAssembly && a.Token == b.Token;
			}
		}

		void UpdateCurrentLocation() {
			var newLoc = GetCurrentCodeLocation();

			if (currentLocation == null || newLoc == null) {
				currentLocation = newLoc;
				UpdateCurrentMethod();
				return;
			}
			if (!CodeLocation.SameMethod(currentLocation.Value, newLoc.Value)) {
				currentLocation = newLoc;
				UpdateCurrentMethod();
				return;
			}

			currentLocation = newLoc;
		}

		void UpdateCurrentMethod() {
			if (currentLocation == null) {
				currentMethod = null;
				return;
			}

			var loadedMod = MainWindow.Instance.LoadAssembly(currentLocation.Value.ModuleAssembly.Assembly, currentLocation.Value.MethodKey.Module).ModuleDefinition as ModuleDefMD;
			if (loadedMod == null) {
				currentMethod = null;
				return;
			}

			currentMethod = loadedMod.ResolveToken(currentLocation.Value.MethodKey.Token) as MethodDef;
		}
		MethodDef currentMethod;

		CodeLocation? GetCurrentCodeLocation() {
			if (ProcessState != DebuggerProcessState.Stopped)
				return null;
			return GetCodeLocation(Debugger.Current.ILFrame);
		}

		CodeLocation? GetCodeLocation(DnFrame frame) {
			if (ProcessState != DebuggerProcessState.Stopped)
				return null;
			if (frame == null)
				return null;
			var sma = frame.GetSerializedDnModuleWithAssembly();
			if (sma == null)
				return null;
			uint token = frame.Token;
			if (token == 0)
				return null;

			return new CodeLocation(sma.Value, token, frame.ILFrameIP);
		}

		StepRange[] GetStepRanges(DnDebugger debugger, DnFrame frame, bool isStepInto) {
			if (frame == null)
				return null;
			if (!frame.IsILFrame)
				return null;
			if (frame.ILFrameIP.IsUnmappedAddress)
				return null;

			var key = CreateMethodKey(debugger, frame);
			if (key == null)
				return null;

			MemberMapping mapping;
			var textView = MainWindow.Instance.SafeActiveTextView;
			var cm = textView.CodeMappings;
			if (cm == null || !cm.TryGetValue(key.Value, out mapping)) {
				// User has decompiled some other code or switched to another tab
				UpdateCurrentMethod();
				JumpToCurrentStatement(textView);

				// It could be cached and immediately available. Check again
				cm = textView.CodeMappings;
				if (cm == null || !cm.TryGetValue(key.Value, out mapping))
					return null;
			}

			bool isMatch;
			var scm = mapping.GetInstructionByOffset(frame.ILFrameIP.Offset, out isMatch);
			uint[] ilRanges;
			if (scm == null)
				ilRanges = mapping.ToArray(null, false);
			else
				ilRanges = scm.ToArray(isMatch);

			if (ilRanges.Length == 0)
				return null;
			return CreateStepRanges(ilRanges);
		}

		static StepRange[] CreateStepRanges(uint[] ilRanges) {
			var stepRanges = new StepRange[ilRanges.Length / 2];
			if (stepRanges.Length == 0)
				return null;
			for (int i = 0; i < stepRanges.Length; i++)
				stepRanges[i] = new StepRange(ilRanges[i * 2], ilRanges[i * 2 + 1]);
			return stepRanges;
		}

		static MethodKey? CreateMethodKey(DnDebugger debugger, DnFrame frame) {
			var sma = frame.GetSerializedDnModuleWithAssembly();
			if (sma == null)
				return null;

			return MethodKey.Create(frame.Token, sma.Value.Module);
		}

		DnFrame GetCurrentILFrame() {
			return debugger.Current.ILFrame;
		}

		DnFrame GetCurrentMethodILFrame() {
			return debugger.Current.ILFrame;
		}

		public bool CanStepInto {
			get { return ProcessState == DebuggerProcessState.Stopped; }
		}

		public void StepInto() {
			if (!CanStepInto)
				return;

			var ranges = GetStepRanges(debugger, GetCurrentILFrame(), true);
			debugger.StepInto(ranges);
		}

		public bool CanStepOver {
			get { return ProcessState == DebuggerProcessState.Stopped; }
		}

		public void StepOver() {
			if (!CanStepOver)
				return;

			var ranges = GetStepRanges(debugger, GetCurrentILFrame(), false);
			debugger.StepOver(ranges);
		}

		public bool CanStepOut {
			get { return ProcessState == DebuggerProcessState.Stopped; }
		}

		public void StepOut() {
			if (!CanStepOut)
				return;

			debugger.StepOut(GetCurrentILFrame());
		}

		public bool CanShowNextStatement {
			get { return ProcessState == DebuggerProcessState.Stopped; }
		}

		public void ShowNextStatement() {
			if (!CanShowNextStatement)
				return;

			var textView = MainWindow.Instance.SafeActiveTextView;
			if (!TryShowNextStatement(textView))
				JumpToCurrentStatement(textView);
		}

		bool TryShowNextStatement(DecompilerTextView textView) {
			// Always reset the selected frame
			StackFrameManager.Instance.SelectedFrame = 0;

			if (textView == null)
				return false;

			Dictionary<MethodKey, MemberMapping> cm;
			if (!VerifyAndGetCurrentDebuggedMethod(textView, out cm))
				return false;
			var currentKey = currentLocation.Value.MethodKey;

			TextLocation location, endLocation;
			if (!cm[currentKey].GetInstructionByTokenAndOffset(currentLocation.Value.ILFrameIP.Offset, out location, out endLocation))
				return false;

			textView.ScrollAndMoveCaretTo(location.Line, location.Column);
			return true;
		}

		bool VerifyAndGetCurrentDebuggedMethod(DecompilerTextView textView, out Dictionary<MethodKey, MemberMapping> codeMappings) {
			codeMappings = textView == null ? null : textView.CodeMappings;
			if (currentLocation == null)
				return false;
			if (codeMappings == null || !codeMappings.ContainsKey(currentLocation.Value.MethodKey))
				return false;

			return true;
		}

		public bool CanSetNextStatement(object parameter) {
			if (!IsDebugging)
				return false;

			var ctx = parameter as ContextMenuEntryContext;

			SourceCodeMapping mapping;
			string errMsg;
			if (!DebugGetSourceCodeMappingForSetNextStatement(ctx == null ? null : ctx.Element as DecompilerTextView, out errMsg, out mapping))
				return false;

			return currentLocation == null ||
				(currentLocation.Value.ILFrameIP.IsExact && currentLocation.Value.ILFrameIP.Offset != mapping.ILInstructionOffset.From);
		}

		public bool SetNextStatement(object parameter) {
			string errMsg;
			if (!DebugSetNextStatement(parameter, out errMsg)) {
				if (string.IsNullOrEmpty(errMsg))
					errMsg = "Could not set next statement (unknown reason)";
				MainWindow.Instance.ShowMessageBox(errMsg);
				return false;
			}

			return true;
		}

		bool DebugSetNextStatement(object parameter, out string errMsg) {
			var ctx = parameter as ContextMenuEntryContext;
			SourceCodeMapping mapping;
			if (!DebugGetSourceCodeMappingForSetNextStatement(ctx == null ? null : ctx.Element as DecompilerTextView, out errMsg, out mapping))
				return false;

			uint ilOffset = mapping.ILInstructionOffset.From;
			var ilFrame = GetCurrentMethodILFrame();
			bool failed = ilFrame == null || !ilFrame.SetILFrameIP(ilOffset);

			// All frames are invalidated
			CallOnProcessStateChanged();

			if (failed) {
				errMsg = "Could not set the next statement.";
				return false;
			}

			return true;
		}

		bool DebugGetSourceCodeMappingForSetNextStatement(DecompilerTextView textView, out string errMsg, out SourceCodeMapping mapping) {
			errMsg = string.Empty;
			mapping = null;

			if (ProcessState == DebuggerProcessState.Terminated) {
				errMsg = "We're not debugging";
				return false;
			}
			if (ProcessState == DebuggerProcessState.Starting || ProcessState == DebuggerProcessState.Running) {
				errMsg = "Can't set next statement when the process is running";
				return false;
			}

			if (textView == null) {
				textView = MainWindow.Instance.ActiveTextView;
				if (textView == null) {
					errMsg = "No tab is available. Decompile the current method!";
					return false;
				}
			}

			Dictionary<MethodKey, MemberMapping> cm;
			if (!VerifyAndGetCurrentDebuggedMethod(textView, out cm)) {
				errMsg = "No debug information found. Make sure that only the debugged method is selected in the treeview (press 'Alt+Num *' to go to current statement)";
				return false;
			}

			var location = textView.TextEditor.TextArea.Caret.Location;
			var bps = SourceCodeMappingUtils.Find(cm, location.Line, location.Column);
			if (bps.Count == 0) {
				errMsg = "It's not possible to set the next statement here";
				return false;
			}

			if (GetCurrentMethodILFrame() == null) {
				errMsg = "There's no IL frame";
				return false;
			}

			if (currentLocation != null) {
				var currentKey = currentLocation.Value.MethodKey;

				foreach (var bp in bps) {
					var md = bp.MemberMapping.MethodDefinition;
					if (currentLocation.Value.Token != md.MDToken.Raw)
						continue;
					var serAsm = GetSerializedDnModuleWithAssembly(md);
					if (serAsm == null)
						continue;
					if (serAsm != currentLocation.Value.ModuleAssembly)
						continue;

					mapping = bp;
					break;
				}
			}
			if (mapping == null) {
				errMsg = "The next statement cannot be set to another method";
				return false;
			}

			return true;
		}

		static SerializedDnModuleWithAssembly? GetSerializedDnModuleWithAssembly(IMemberDef md) {
			if (md == null)
				return null;
			//TODO: Method doesn't work with in-memory modules or assemblies
			var mod = md.Module;
			if (mod == null)
				return null;
			var asm = mod.Assembly;
			if (asm == null)
				return null;
			return new SerializedDnModuleWithAssembly(asm.ManifestModule.Location, new SerializedDnModule(mod.Location));
		}
	}
}
