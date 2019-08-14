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
using System.Threading.Tasks;
using dnSpy.Contracts.Debugger.DotNet.Code;

namespace dnSpy.Contracts.Debugger.DotNet.Steppers.Engine {
	/// <summary>
	/// .NET stepper
	/// </summary>
	public abstract class DbgDotNetEngineStepper {
		/// <summary>
		/// Max number of return values to save
		/// </summary>
		protected static readonly int maxReturnValues = 100;

		/// <summary>
		/// Session
		/// </summary>
		public abstract class SessionBase {
			/// <summary>
			/// Gets the tag
			/// </summary>
			public object? Tag { get; }

			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="tag">Tag</param>
			protected SessionBase(object? tag) => Tag = tag;
		}

		/// <summary>
		/// Gets/sets the session. It's null if there's no step operation in progress.
		/// </summary>
		public abstract SessionBase? Session { get; set; }

		/// <summary>
		/// Creates a new <see cref="SessionBase"/>
		/// </summary>
		/// <param name="tag">Tag</param>
		/// <returns></returns>
		public abstract SessionBase CreateSession(object? tag);

		/// <summary>
		/// true if the runtime is paused
		/// </summary>
		public abstract bool IsRuntimePaused { get; }

		/// <summary>
		/// Gets the countinue counter. It's incremented each time the process is continued.
		/// </summary>
		public abstract uint ContinueCounter { get; }

		/// <summary>
		/// Gets frame info or null if none is available
		/// </summary>
		/// <param name="thread">Thread</param>
		/// <returns></returns>
		public abstract DbgDotNetEngineStepperFrameInfo? TryGetFrameInfo(DbgThread thread);

		/// <summary>
		/// Lets the process run
		/// </summary>
		public abstract void Continue();

		/// <summary>
		/// Steps out
		/// </summary>
		/// <param name="frame">Frame info</param>
		/// <returns></returns>
		public abstract Task<DbgThread> StepOutAsync(DbgDotNetEngineStepperFrameInfo frame);

		/// <summary>
		/// Steps into
		/// </summary>
		/// <param name="frame">Frame info</param>
		/// <param name="ranges">Statement ranges</param>
		/// <returns></returns>
		public abstract Task<DbgThread> StepIntoAsync(DbgDotNetEngineStepperFrameInfo frame, DbgCodeRange[] ranges);

		/// <summary>
		/// Steps over
		/// </summary>
		/// <param name="frame">Frame info</param>
		/// <param name="ranges">Statement ranges</param>
		/// <returns></returns>
		public abstract Task<DbgThread> StepOverAsync(DbgDotNetEngineStepperFrameInfo frame, DbgCodeRange[] ranges);

		/// <summary>
		/// Cancels last step operation
		/// </summary>
		public abstract void CancelLastStep();

		/// <summary>
		/// Prepares collecting return values
		/// </summary>
		/// <param name="frame">Frame info</param>
		/// <param name="statementInstructions">Statement instructions</param>
		public abstract void CollectReturnValues(DbgDotNetEngineStepperFrameInfo frame, DbgILInstruction[][] statementInstructions);

		/// <summary>
		/// Clears all return values
		/// </summary>
		public abstract void ClearReturnValues();

		/// <summary>
		/// Creates a breakpoint
		/// </summary>
		/// <param name="thread">Thread or null to match any thread</param>
		/// <param name="module">Module</param>
		/// <param name="token">Method token</param>
		/// <param name="offset">IL offset</param>
		/// <returns></returns>
		public abstract DbgDotNetStepperBreakpoint CreateBreakpoint(DbgThread? thread, DbgModule module, uint token, uint offset);

		/// <summary>
		/// Removes breakpoints
		/// </summary>
		/// <param name="breakpoints">Breakpoints to remove</param>
		public abstract void RemoveBreakpoints(DbgDotNetStepperBreakpoint[] breakpoints);

		/// <summary>
		/// Called when the step is complete
		/// </summary>
		public abstract void OnStepComplete();

		/// <summary>
		/// Called when it gets canceled
		/// </summary>
		/// <param name="session">Session</param>
		public abstract void OnCanceled(SessionBase session);

		/// <summary>
		/// Returns true if the exception should be ignored eg. because the process has exited
		/// </summary>
		/// <param name="exception">Thrown exception</param>
		/// <returns></returns>
		public abstract bool IgnoreException(Exception exception);

		/// <summary>
		/// Cleans up
		/// </summary>
		/// <param name="dispatcher">Dispatcher</param>
		public abstract void Close(DbgDispatcher dispatcher);
	}

	/// <summary>
	/// A code breakpoint used by the .NET stepper
	/// </summary>
	public abstract class DbgDotNetStepperBreakpoint {
		/// <summary>
		/// Raised when the breakpoint is hit
		/// </summary>
		public abstract event EventHandler<DbgDotNetStepperBreakpointEventArgs>? Hit;
	}

	/// <summary>
	/// Stepper breakpoint event args
	/// </summary>
	public sealed class DbgDotNetStepperBreakpointEventArgs {
		/// <summary>
		/// Gets the thread
		/// </summary>
		public DbgThread Thread { get; }

		/// <summary>
		/// Set to true by the event handler to pause the process
		/// </summary>
		public bool Pause { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="thread">Current thread</param>
		public DbgDotNetStepperBreakpointEventArgs(DbgThread thread) =>
			Thread = thread ?? throw new ArgumentNullException(nameof(thread));
	}
}
