// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.ILSpy.Debugger.Services
{
	public class DebugStackFrame
	{
		public MethodKey MethodKey;
		public int? ILOffset;
	}

	public interface IDebugger : IDisposable
	{
		/// <summary>
		/// Gets whether the debugger can evaluate the expression.
		/// </summary>
		bool CanEvaluate { get; }
		
		/// <summary>
		/// Returns true if debuger is attached to a process
		/// </summary>
		bool IsDebugging {
			get;
		}
		
		/// <summary>
		/// Returns true if process is running
		/// Returns false if breakpoint is hit, program is breaked, program is stepped, etc...
		/// </summary>
		bool IsProcessRunning {
			get;
		}
		
		/// <summary>
		/// Gets or sets whether the debugger should break at the first line of execution.
		/// </summary>
		bool BreakAtBeginning {
			get; set; 
		}
		
		/// <summary>
		/// Starts process and attaches debugger
		/// </summary>
		void Start(ProcessStartInfo processStartInfo);
		
		/// <summary>
		/// Stops/terminates attached process
		/// </summary>
		void Stop();
		
		// ExecutionControl:
		
		void Break();
		
		void Continue();
		
		// Stepping:
		
		void StepInto();
		
		void StepOver();
		
		void StepOut();
		
		/// <summary>
		/// Used to attach to an existing process.
		/// </summary>
		void Attach(Process process);
		
		void Detach();
			
		/// <summary>
		/// Gets the current value of the variable as string that can be displayed in tooltips.
		/// </summary>
		string GetValueAsString(string variable);
		
		/// <summary>
		/// Gets the tooltip control that shows the value of given variable.
		/// Return null if no tooltip is available.
		/// </summary>
		object GetTooltipControl(TextLocation logicalPosition, string variable);

		/// <summary>
		/// Queries the debugger whether it is possible to set the instruction pointer to a given position.
		/// </summary>
		/// <returns>True if possible. False otherwise</returns>
		bool CanSetInstructionPointer(int ilOffset);

		/// <summary>
		/// Set the instruction pointer to a given position.
		/// </summary>
		/// <returns>True if successful. False otherwise</returns>
		bool SetInstructionPointer(int ilOffset);

		/// <summary>
		/// Gets stack frames
		/// </summary>
		/// <param name="count">Max number of stack frames to return.</param>
		/// <returns></returns>
		IEnumerable<DebugStackFrame> GetStackFrames(int count);
		
		/// <summary>
		/// Ocurrs when the debugger is starting.
		/// </summary>
		event EventHandler DebugStarting;
		
		/// <summary>
		/// Ocurrs after the debugger has started.
		/// </summary>
		event EventHandler DebugStarted;
		
		/// <summary>
		/// Ocurrs when the value of IsProcessRunning changes.
		/// </summary>
		event EventHandler ProcessRunningChanged;
		
		/// <summary>
		/// Ocurrs after the debugging of program is finished.
		/// </summary>
		event EventHandler DebugStopped;
	}
}
