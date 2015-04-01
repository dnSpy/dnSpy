// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;

using Debugger;
using Debugger.Interop.CorDebug;
using Debugger.Interop.CorPublish;
using Debugger.MetaData;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Models.TreeModel;
using ICSharpCode.ILSpy.Debugger.Services.Debugger;
using ICSharpCode.ILSpy.Debugger.Tooltips;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Visitors;
using dnlib.DotNet;
using CorDbg = Debugger;
using Process = Debugger.Process;
using StackFrame = Debugger.StackFrame;

namespace ICSharpCode.ILSpy.Debugger.Services
{
	[Export(typeof(IDebugger))]
	public class WindowsDebugger : IDebugger
	{
		enum StopAttachedProcessDialogResult {
			Detach = 0,
			Terminate = 1,
			Cancel = 2
		}
		
		bool attached;
		NDebugger debugger;
		Process debuggedProcess;
		
		public NDebugger DebuggerCore {
			get {
				return debugger;
			}
		}
		
		public Process DebuggedProcess {
			get {
				return debuggedProcess;
			}
		}
		
		public static Process CurrentProcess {
			get {
				WindowsDebugger dbgr = DebuggerService.CurrentDebugger as WindowsDebugger;
				if (dbgr != null && dbgr.DebuggedProcess != null) {
					return dbgr.DebuggedProcess;
				} else {
					return null;
				}
			}
		}
		
		/// <inheritdoc/>
		public bool BreakAtBeginning {
			get;
			set;
		}
		
		public bool ServiceInitialized {
			get {
				return debugger != null;
			}
		}
		
		public WindowsDebugger()
		{
		}
		
		#region IDebugger Members
		
		const string errorDebugging      = "Error.Debugging";
		const string errorNotDebugging   = "Error.NotDebugging";
		const string errorProcessRunning = "Error.ProcessRunning";
		const string errorProcessPaused  = "Error.ProcessPaused";
		const string errorCannotStepNoActiveFunction = "Threads.CannotStepNoActiveFunction";
		
		public bool IsDebugging {
			get {
				return ServiceInitialized && debuggedProcess != null;
			}
		}
		
		public bool IsAttached {
			get {
				return ServiceInitialized && attached;
			}
		}
		
		public bool IsProcessRunning {
			get {
				return IsDebugging && debuggedProcess.IsRunning;
			}
		}
		
		public void Start(ProcessStartInfo processStartInfo)
		{
			if (IsDebugging) {
				MessageBox.Show(errorDebugging);
				return;
			}
			if (!ServiceInitialized) {
				InitializeService();
			}

			string version = debugger.GetProgramVersion(processStartInfo.FileName);
			
			attached = false;
			if (DebugEvent != null)
				DebugEvent(this, new DebuggerEventArgs(DebuggerEvent.Starting));
			if (version.StartsWith("v1.0")) {
				StartError("Net10NotSupported");
			} else if (version.StartsWith("v1.1")) {
				StartError("Net1.1NotSupported");
			} else if (debugger.IsKernelDebuggerEnabled) {
				StartError("KernelDebuggerEnabled");
			} else {
				try {
					// set the JIT flag for evaluating optimized code
					Process.DebugMode = DebugModeFlag.Debug;
					
					Process process = debugger.Start(processStartInfo.FileName,
					                                 processStartInfo.WorkingDirectory,
					                                 processStartInfo.Arguments);
					SelectProcess(process);
				} catch (System.Exception e) {
					// COMException: The request is not supported. (Exception from HRESULT: 0x80070032)
					// COMException: The application has failed to start because its side-by-side configuration is incorrect. Please see the application event log for more detail. (Exception from HRESULT: 0x800736B1)
					// COMException: The requested operation requires elevation. (Exception from HRESULT: 0x800702E4)
					// COMException: The directory name is invalid. (Exception from HRESULT: 0x8007010B)
					// BadImageFormatException:  is not a valid Win32 application. (Exception from HRESULT: 0x800700C1)
					// UnauthorizedAccessException: Отказано в доступе. (Исключение из HRESULT: 0x80070005 (E_ACCESSDENIED))
					if (e is COMException || e is BadImageFormatException || e is UnauthorizedAccessException) {
						string msg = "CannotStartProcess";
						msg += " " + e.Message;
						if (e is COMException) {
							uint errCode = unchecked((uint)((COMException)e).ErrorCode);
							if (errCode == 0x80070032 || errCode == 0x80131C30) {
								var origMsg = msg;
								if (errCode == 0x80131C30)
									msg = "Use dnSpy-x86.exe to debug 32-bit applications.";
								else
									msg = "Use dnSpy.exe to debug 64-bit applications.";
								msg += Environment.NewLine + Environment.NewLine;
								msg += origMsg;
							}
						}
						StartError(msg);
					} else {
						if (DebugEvent != null)
							DebugEvent(this, new DebuggerEventArgs(DebuggerEvent.Stopped));
						throw;
					}
				}
			}
		}

		void StartError(string msg)
		{
			MessageBox.Show(msg);

			if (DebugEvent != null)
				DebugEvent(this, new DebuggerEventArgs(DebuggerEvent.Stopped));
		}
		
		public void Attach(System.Diagnostics.Process existingProcess)
		{
			if (existingProcess == null)
				return;
			
			if (IsDebugging) {
				MessageBox.Show(errorDebugging);
				return;
			}
			if (!ServiceInitialized) {
				InitializeService();
			}
			
			if (DebugEvent != null)
				DebugEvent(this, new DebuggerEventArgs(DebuggerEvent.Starting));
			string version = debugger.GetProgramVersion(existingProcess.MainModule.FileName);
			if (version.StartsWith("v1.0")) {
				StartError("Net10NotSupported");
			} else {
				
				try {
					// set the JIT flag for evaluating optimized code
					Process.DebugMode = DebugModeFlag.Debug;
					
					Process process = debugger.Attach(existingProcess);
					attached = true;
					SelectProcess(process);
				} catch (System.Exception e) {
					// CORDBG_E_DEBUGGER_ALREADY_ATTACHED
					if (e is COMException || e is UnauthorizedAccessException) {
						string msg = "CannotAttachToProcess";
						StartError(msg + " " + e.Message);
						
					} else {
						if (DebugEvent != null)
							DebugEvent(this, new DebuggerEventArgs(DebuggerEvent.Stopped));
						throw;
					}
				}
			}
		}

		public void Detach()
		{
			if (debuggedProcess == null)
				return;

			if (DebugEvent != null)
				DebugEvent(this, new DebuggerEventArgs(DebuggerEvent.Detaching));
			debugger.Detach();
		}
		
		public void Stop()
		{
			if (!IsDebugging) {
				MessageBox.Show(errorNotDebugging, "Stop");
				return;
			}
			if (IsAttached) {
				Detach();
			} else {
				debuggedProcess.Terminate();
			}
		}
		
		// ExecutionControl:
		
		public void Break()
		{
			if (!IsDebugging) {
				MessageBox.Show(errorNotDebugging, "Break");
				return;
			}
			if (!IsProcessRunning) {
				MessageBox.Show(errorProcessPaused, "Break");
				return;
			}
			debuggedProcess.Break();
		}
		
		public void Continue()
		{
			if (!IsDebugging) {
				MessageBox.Show(errorNotDebugging, "Continue");
				return;
			}
			if (IsProcessRunning) {
				MessageBox.Show(errorProcessRunning, "Continue");
				return;
			}
			debuggedProcess.AsyncContinue();
		}
		
		// Stepping:
		
		SourceCodeMapping GetCurrentCodeMapping(Dictionary<MethodKey, MemberMapping> cm, out StackFrame frame, out bool isMatch, out bool methodExists)
		{
			isMatch = false;
			methodExists = false;
			frame = debuggedProcess.SelectedThread.MostRecentStackFrame;
			var key = frame.MethodInfo.ToMethodKey();
			
			// get the mapped instruction from the current line marker or the next one
			if (cm == null || !cm.ContainsKey(key))
				return null;
			methodExists = true;

			var ip = frame.IP;
			if (ip.IsInvalid)
				return null;

			return cm[key].GetInstructionByOffset((uint)ip.Offset, out isMatch);
		}
		
		StackFrame GetStackFrame()
		{
			var textView = MainWindow.Instance.ActiveTextView;
			var cm = textView == null ? null : textView.CodeMappings;
			bool isMatch, methodExists;
			StackFrame frame;
			var map = GetCurrentCodeMapping(cm, out frame, out isMatch, out methodExists);
			if (map == null) {
				if (frame.IP.IsInvalid) {
					frame.SourceCodeLine = -1;
					frame.ILRanges = new[] { 0, 1 };
				}
				else if (methodExists) {
					frame.SourceCodeLine = -1;
					frame.ILRanges = cm[frame.MethodInfo.ToMethodKey()].ToArray(null, false);
					if (frame.ILRanges.Length == 0)
						frame.ILRanges = new[] { frame.IP.Offset, frame.IP.Offset + 1 };
				}
				else {
					// The user has probably selected another method and pressed F10/F11 while in
					// that method instead of the current method. Make sure the debugged method is
					// shown before we continue. It could also be that it's a hidden method with
					// no IL ranges (eg. a hidden .cctor).
					var old = DebugInformation.DebugStepInformation;
					StepIntoUnknownFrame(frame);
					var info = DebugInformation.DebugStepInformation;
					// This condition is true if the method we're in is hidden and there are no
					// IL ranges at all.
					if (old != null && info != null && old.Item1 == info.Item1 && old.Item2 == info.Item2 && old.Item3 == info.Item3) {
						frame.SourceCodeLine = -1;
						frame.ILRanges = new[] { 0, int.MaxValue };
					}
					else {
						DebugUtils.JumpToCurrentStatement(MainWindow.Instance.SafeActiveTextView);
						return null;
					}
				}
			} else {
				frame.SourceCodeLine = map.StartLocation.Line;
				frame.ILRanges = map.ToArray(isMatch);
			}
			
			return frame;
		}
		
		public void StepInto()
		{
			if (!IsDebugging) {
				MessageBox.Show(errorNotDebugging, "StepInto");
				return;
			}
			
			// use most recent stack frame because we don't have the symbols
			if (debuggedProcess.SelectedThread == null ||
			    debuggedProcess.SelectedThread.MostRecentStackFrame == null ||
			    debuggedProcess.IsRunning) {
				MessageBox.Show(errorCannotStepNoActiveFunction, "StepInto");
			} else {
				var frame = GetStackFrame();
				if (frame != null)
					frame.AsyncStepInto();
			}
		}
		
		public void StepOver()
		{
			if (!IsDebugging) {
				MessageBox.Show(errorNotDebugging, "StepOver");
				return;
				
			}
			// use most recent stack frame because we don't have the symbols
			if (debuggedProcess.SelectedThread == null ||
			    debuggedProcess.SelectedThread.MostRecentStackFrame == null ||
			    debuggedProcess.IsRunning) {
				MessageBox.Show(errorCannotStepNoActiveFunction, "StepOver");
			} else {
				var frame = GetStackFrame();
				if (frame != null) {
					frame.AsyncStepOver();
					//Utils.DoEvents(frame.Process);
				}
			}
		}
		
		public void StepOut()
		{
			if (!IsDebugging) {
				MessageBox.Show(errorNotDebugging, "StepOut");
				return;
			}
			
			// use most recent stack frame because we don't have the symbols
			if (debuggedProcess.SelectedThread == null ||
			    debuggedProcess.SelectedThread.MostRecentStackFrame == null ||
			    debuggedProcess.IsRunning) {
				MessageBox.Show(errorCannotStepNoActiveFunction, "StepOut");
			} else {
				var frame = GetStackFrame();
				if (frame != null)
					frame.AsyncStepOut();
			}
		}
		
		public event EventHandler<DebuggerEventArgs> DebugEvent;
		
		/// <summary>
		/// Gets variable of given name.
		/// Returns null if unsuccessful. Can throw GetValueException.
		/// <exception cref="GetValueException">Thrown when evaluation fails. Exception message explains reason.</exception>
		/// </summary>
		public Value GetValueFromName(string variableName)
		{
			if (!CanEvaluate) {
				return null;
			}
			return ExpressionEvaluator.Evaluate(variableName, SupportedLanguage.CSharp, debuggedProcess.SelectedThread.MostRecentStackFrame);
		}
		
		/// <summary>
		/// Gets Expression for given variable. Can throw GetValueException.
		/// <exception cref="GetValueException">Thrown when getting expression fails. Exception message explains reason.</exception>
		/// </summary>
		public ICSharpCode.NRefactory.CSharp.Expression GetExpression(string variableName)
		{
			if (!CanEvaluate) {
				throw new GetValueException("Cannot evaluate now - debugged process is either null or running or has no selected stack frame");
			}
			return ExpressionEvaluator.ParseExpression(variableName, SupportedLanguage.CSharp);
		}
		
		public static bool IsManaged(int processId)//TODO: Why isn't this method used by AttachToProcessWindow?
		{
			var corPublish = new CorpubPublishClass();
			ICorPublishProcess process = corPublish.GetProcess((uint)processId);
			if (process != null) {
				return process.IsManaged() != 0;
			}
			return false;
		}
		
		/// <summary>
		/// Gets the current value of the variable as string that can be displayed in tooltips.
		/// Returns null if unsuccessful.
		/// </summary>
		public string GetValueAsString(string variableName)
		{
			try {
				Value val = GetValueFromName(variableName);
				if (val == null) return null;
				return val.AsString();
			} catch (GetValueException) {
				return null;
			}
		}

		public int DebuggedProcessId {
			get {
				var p = debuggedProcess;
				if (p == null)
					return -1;
				return p.ProcessId;
			}
		}
		
		/// <inheritdoc/>
		public bool CanEvaluate
		{
			get {
				return debuggedProcess != null &&
					!debuggedProcess.IsRunning &&
					debuggedProcess.SelectedThread != null &&
					debuggedProcess.SelectedThread.MostRecentStackFrame != null;
			}
		}
		
		/// <summary>
		/// Gets the tooltip control that shows the value of given variable.
		/// Return null if no tooltip is available.
		/// </summary>
		public object GetTooltipControl(TextLocation logicalPosition, string variableName)
		{
			try {
				var tooltipExpression = GetExpression(variableName);
				if (tooltipExpression == null) return null;
				
				string imageName;
				var image = ExpressionNode.GetImageForLocalVariable(out imageName);
				ExpressionNode expressionNode = new ExpressionNode(image, variableName, tooltipExpression);
				expressionNode.ImageName = imageName;
				
				return new DebuggerTooltipControl(logicalPosition, expressionNode);
			} catch (GetValueException) {
				return null;
			}
		}
		
		internal ITreeNode GetNode(string variable, string currentImageName = null)
		{
			try {
				var expression = GetExpression(variable);
				string imageName;
				ImageSource image;
				if (string.IsNullOrEmpty(currentImageName)) {
					image = ExpressionNode.GetImageForLocalVariable(out imageName);
				}
				else {
					image = ImageService.GetImage(currentImageName);
					imageName = currentImageName;
				}
				ExpressionNode expressionNode = new ExpressionNode(image, variable, expression);
				expressionNode.ImageName = imageName;
				return expressionNode;
			} catch (GetValueException) {
				return null;
			}
		}

		public bool CanSetInstructionPointer(int ilOffset)
		{
			if (debuggedProcess != null && debuggedProcess.IsPaused &&
			    debuggedProcess.SelectedThread != null && debuggedProcess.SelectedThread.MostRecentStackFrame != null) {
				return debuggedProcess.SelectedThread.MostRecentStackFrame.CanSetIP(ilOffset);
			} else {
				return false;
			}
		}

		public bool SetInstructionPointer(int ilOffset)
		{
			return debuggedProcess.SelectedThread.MostRecentStackFrame.SetIP(ilOffset);
		}
		
		public void Dispose()
		{
			Stop();
		}
		
		#endregion
		
		public event EventHandler Initialize;
		
		public void InitializeService()
		{
			debugger = new NDebugger();
			
			debugger.DebuggerTraceMessage    += debugger_TraceMessage;
			debugger.Processes.Added         += debugger_ProcessStarted;
			debugger.Processes.Removed       += debugger_ProcessExited;
			
			DebuggerService.BreakPointAdded  += delegate (object sender, BreakpointBookmarkEventArgs e) {
				AddBreakpoint(e.BreakpointBookmark);
			};
			
			foreach (BreakpointBookmark b in DebuggerService.Breakpoints) {
				AddBreakpoint(b);
			}
			
			if (Initialize != null) {
				Initialize(this, null);
			}
		}
		
		void AddBreakpoint(BreakpointBookmark bookmark)
		{
			Breakpoint breakpoint = null;
			
			breakpoint = new ILBreakpoint(
				debugger,
				bookmark.Location,
				bookmark.EndLocation,
				bookmark.MethodKey,
				bookmark.ILRange.From,
				bookmark.IsEnabled);
			
			debugger.Breakpoints.Add(breakpoint);
			
			// event handlers on bookmark and breakpoint don't need deregistration
			bookmark.IsEnabledChanged += delegate {
				breakpoint.Enabled = bookmark.IsEnabled;
			};
			
			EventHandler<CollectionItemEventArgs<Process>> bp_debugger_ProcessStarted = (sender, e) => {
				// User can change line number by inserting or deleting lines
				breakpoint.Location = bookmark.Location;
				breakpoint.EndLocation = bookmark.EndLocation;
			};
			EventHandler<CollectionItemEventArgs<Process>> bp_debugger_ProcessExited = (sender, e) => {
			};
			
			BookmarkEventHandler bp_bookmarkManager_Removed = null;
			bp_bookmarkManager_Removed = (sender, e) => {
				if (bookmark == e.Bookmark) {
					debugger.Breakpoints.Remove(breakpoint);
					
					// unregister the events
					debugger.Processes.Added -= bp_debugger_ProcessStarted;
					debugger.Processes.Removed -= bp_debugger_ProcessExited;
					BookmarkManager.Removed -= bp_bookmarkManager_Removed;
				}
			};
			// register the events
			debugger.Processes.Added += bp_debugger_ProcessStarted;
			debugger.Processes.Removed += bp_debugger_ProcessExited;
			BookmarkManager.Removed += bp_bookmarkManager_Removed;
		}
		
		void LogMessage(object sender, MessageEventArgs e)
		{
			//TODO: Log it
		}
		
		void debugger_TraceMessage(object sender, MessageEventArgs e)
		{
			//TODO: Log it
		}
		
		void debugger_ProcessStarted(object sender, CollectionItemEventArgs<Process> e)
		{
			if (debugger.Processes.Count == 1) {
				if (DebugEvent != null)
					DebugEvent(this, new DebuggerEventArgs(DebuggerEvent.Started));
			}
			e.Item.LogMessage += LogMessage;
		}
		
		void debugger_ProcessExited(object sender, CollectionItemEventArgs<Process> e)
		{
			if (debugger.Processes.Count == 0) {
				SelectProcess(null, e);
			} else {
				SelectProcess(debugger.Processes[0]);
			}
		}
		
		public void SelectProcess(Process process, CollectionItemEventArgs<Process> e = null)
		{
			if (debuggedProcess != null) {
				debuggedProcess.Paused          -= debuggedProcess_DebuggingPaused;
				debuggedProcess.ExceptionThrown -= debuggedProcess_ExceptionThrown;
				debuggedProcess.Resumed         -= debuggedProcess_DebuggingResumed;
				debuggedProcess.ModulesAdded 	-= debuggedProcess_ModulesAdded;
			}
			debuggedProcess = process;
			if (debuggedProcess != null) {
				debuggedProcess.Paused          += debuggedProcess_DebuggingPaused;
				debuggedProcess.ExceptionThrown += debuggedProcess_ExceptionThrown;
				debuggedProcess.Resumed         += debuggedProcess_DebuggingResumed;
				debuggedProcess.ModulesAdded 	+= debuggedProcess_ModulesAdded;
				
				debuggedProcess.BreakAtBeginning = BreakAtBeginning;
				if (DebugEvent != null)
					DebugEvent(this, new DebuggerEventArgs(DebuggerEvent.ProcessSelected));
			}
			else {
				if (DebugEvent != null)
					DebugEvent(this, new DebuggerEventArgs(DebuggerEvent.ProcessSelected));
				if (DebugEvent != null)
					DebugEvent(this, new DebuggerEventArgs(DebuggerEvent.Stopped));
			}
			BreakAtBeginning = false;
		}

		void debuggedProcess_ModulesAdded(object sender, ModuleEventArgs e)
		{
			foreach (var bp in debugger.Breakpoints) {
				var breakpoint = bp as ILBreakpoint;
				if (breakpoint != null && breakpoint.MethodKey.IsSameModule(e.Module.FullPath))
					breakpoint.SetBreakpoint(e.Module);
			}
		}
		
		void debuggedProcess_DebuggingPaused(object sender, ProcessEventArgs e)
		{
			JumpToCurrentLine();
			if (DebugEvent != null)
				DebugEvent(this, new DebuggerEventArgs(DebuggerEvent.Paused));
		}
		
		void debuggedProcess_DebuggingResumed(object sender, CorDbg.ProcessEventArgs e)
		{
			if (DebugEvent != null)
				DebugEvent(this, new DebuggerEventArgs(DebuggerEvent.Resumed));
			StackFrameStatementManager.Remove(true);
		}
		
		void debuggedProcess_ExceptionThrown(object sender, CorDbg.ExceptionEventArgs e)
		{
			if (!e.IsUnhandled) {
				// Ignore the exception
				e.Process.AsyncContinue();
				return;
			}
			
			StringBuilder stacktraceBuilder = new StringBuilder();
			
			// Need to intercept now so that we can evaluate properties
			if (e.Process.SelectedThread.InterceptCurrentException()) {
				stacktraceBuilder.AppendLine(e.Exception.ToString());
				string stackTrace;
				try {
					stackTrace = e.Exception.GetStackTrace("--- End of inner exception stack trace ---");
				} catch (GetValueException) {
					stackTrace = e.Process.SelectedThread.GetStackTrace("at {0} in {1}:line {2}", "at {0}");
				}
				stacktraceBuilder.Append(stackTrace);
			} else {
				// For example, happens on stack overflow
				stacktraceBuilder.AppendLine("CannotInterceptException");
				stacktraceBuilder.AppendLine(e.Exception.ToString());
				stacktraceBuilder.Append(e.Process.SelectedThread.GetStackTrace("at {0} in {1}:line {2}", "at {0}"));
			}
			
			string title = e.IsUnhandled ? "Unhandled" : "Handled";
			string message = string.Format("Message {0} {1}", e.Exception.Type, e.Exception.Message);
			
			MessageBox.Show(message + stacktraceBuilder.ToString(), title);
		}
		
		public void JumpToCurrentLine()
		{
			if (debuggedProcess != null &&  debuggedProcess.SelectedThread != null) {

				MainWindow.Instance.Activate();

				// use most recent stack frame because we don't have the symbols
				var frame = debuggedProcess.SelectedThread.MostRecentStackFrame;
				if (frame == null)
					return;
				var ip = frame.IP;
				
				var key = frame.MethodInfo.ToMethodKey();
				TextLocation location, endLocation;

				var textView = MainWindow.Instance.ActiveTextView;
				var cm = textView == null ? null : textView.CodeMappings;
				if (cm != null && cm.ContainsKey(key) &&
					ip.IsValid &&
					cm[key].GetInstructionByTokenAndOffset((uint)ip.Offset, out location, out endLocation)) {
					var info = DebugInformation.DebugStepInformation;
					if (info == null || info.Item1 != key)
						StepIntoUnknownFrame(frame);
					else {
						DebugInformation.DebugStepInformation = Tuple.Create(info.Item1, ip.Offset, info.Item3);
						DebugInformation.MustJumpToReference = false;
					}
					textView.ScrollAndMoveCaretTo(location.Line, location.Column);
				}
				else {
					StepIntoUnknownFrame(frame);
				}

				StackFrameStatementManager.UpdateReturnStatementBookmarks(true);
			}
		}

		void StepIntoUnknownFrame(StackFrame frame)
		{
			var debugType = (DebugType)frame.MethodInfo.DeclaringType;
			var key = frame.MethodInfo.ToMethodKey();
			var ip = frame.IP;

			var debugModule = debugType.DebugModule;
			DebugInformation.MustJumpToReference = false;
			if (!string.IsNullOrEmpty(debugModule.FullPath)) {
				var loadedMod = MainWindow.Instance.LoadAssembly(debugModule.AssemblyFullPath, debugModule.FullPath).ModuleDefinition as ModuleDefMD;
				if (loadedMod != null) {
					DebugInformation.DebugStepInformation = Tuple.Create(key, ip.IsInvalid ? int.MaxValue : ip.Offset, loadedMod.ResolveToken(key.Token) as IMemberRef);
					DebugInformation.MustJumpToReference = true;
				}
			}
			if (!DebugInformation.MustJumpToReference) {
				DebugInformation.DebugStepInformation = null;
				Debug.Fail("No type was found!");
			}
		}

		public IEnumerable<DebugStackFrame> GetStackFrames(int count)
		{
			if (debuggedProcess == null || !debuggedProcess.IsPaused || debuggedProcess.SelectedThread == null)
				yield break;
			foreach (StackFrame frame in debuggedProcess.SelectedThread.GetCallstack(count)) {
				yield return new DebugStackFrame {
					MethodKey = frame.MethodInfo.ToMethodKey(),
					ILOffset = frame.IP.IsValid ? new int?(frame.IP.Offset) : null,
				};
			}
		}
	}
}
